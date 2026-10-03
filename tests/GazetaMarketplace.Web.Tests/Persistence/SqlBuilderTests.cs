using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SqlBuilderTests
#pragma warning restore CA1515
{
    private static readonly Dictionary<string, string> Allowed = new()
    {
        ["preco"] = "a.PriceCents",
        ["recentes"] = "a.PublishedAt"
    };

    private static SqlBuilder Base() => new SqlBuilder().Select("a.Id, a.Title").From("Ads a");

    [TestMethod]
    public void Valores_SempreViramParametros()
    {
        const string malicious = "x'; DROP TABLE Ads; --";

        SqlQuery query = Base()
            .Where("a.Title LIKE @Title")
            .Parameter("Title", malicious)
            .OrderBy(null, null, Allowed, "a.PublishedAt DESC")
            .Build();

        Assert.DoesNotContain("DROP", query.Sql);
        Assert.DoesNotContain("x'", query.Sql);
        Assert.AreEqual(malicious, query.Parameters.Get<string>("Title"));
        StringAssert.Contains(query.Sql, "WHERE a.Title LIKE @Title");
    }

    [TestMethod]
    [DataRow("a.Title = 'x'")]
    [DataRow("a.Id = 5 OR 1 = 1")]
    [DataRow("a.Title LIKE @Title; DROP TABLE Ads")]
    [DataRow("a.Title = @Title -- comentário")]
    [DataRow("a.Title = @Title /* x */")]
    [DataRow("")]
    public void FragmentoComLiteralOuTextoPerigoso_E_Recusado(string fragment)
    {
        Assert.ThrowsExactly<ArgumentException>(() => Base().Where(fragment));
    }

    [TestMethod]
    public void ParametroSemValor_FalhaNoBuild()
    {
        SqlBuilder builder = Base().Where("a.Title LIKE @Title").OrderBy(null, null, Allowed, "a.Id");

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    [TestMethod]
    public void NomeDeParametroInvalido_E_Recusado()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Base().Parameter("a; DROP", 1));
    }

    [TestMethod]
    public void OrdenacaoForaDaLista_EIgnorada()
    {
        SqlQuery outsideList = Base().OrderBy("preco; DROP TABLE Ads", "desc; --", Allowed, "a.PublishedAt DESC").Build();
        SqlQuery invalidDirection = Base().OrderBy("preco", "sideways", Allowed, "a.PublishedAt DESC").Build();
        SqlQuery valid = Base().OrderBy("preco", "desc", Allowed, "a.PublishedAt DESC").Build();

        StringAssert.EndsWith(outsideList.Sql, "ORDER BY a.PublishedAt DESC");
        Assert.DoesNotContain("DROP", outsideList.Sql);
        StringAssert.EndsWith(invalidDirection.Sql, "ORDER BY a.PriceCents ASC");
        StringAssert.EndsWith(valid.Sql, "ORDER BY a.PriceCents DESC");
    }

    [TestMethod]
    public void Paginar_GeraOffsetFetch_ComParametros()
    {
        SqlQuery query = Base().OrderBy(null, null, Allowed, "a.Id").Page(3, 24).Build();

        StringAssert.EndsWith(query.Sql, "OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY");
        Assert.AreEqual(48, query.Parameters.Get<int>("Offset"));
        Assert.AreEqual(24, query.Parameters.Get<int>("Take"));
    }

    [TestMethod]
    public void Paginar_SemOrdenacao_FalhaNoBuild()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Base().Page(1, 20).Build());
    }

    [TestMethod]
    [DataRow(0, 20)]
    [DataRow(1, 0)]
    [DataRow(1, 101)]
    public void Paginar_ComValoresForaDoIntervalo_E_Recusado(int page, int size)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Base().Page(page, size));
    }

    [TestMethod]
    public void FragmentoSomentePublicados_E_UnicoEReutilizado()
    {
        SqlQuery search = Base().OnlyPublished().Where("a.Title LIKE @Title").Parameter("Title", "%a%").OrderBy(null, null, Allowed, "a.Id").Build();
        SqlQuery list = new SqlBuilder().Select("a.Id").From("Ads a").OnlyPublished().OrderBy(null, null, Allowed, "a.Id").Build();

        StringAssert.Contains(search.Sql, SqlFragments.OnlyPublished);
        StringAssert.Contains(list.Sql, SqlFragments.OnlyPublished);
        Assert.AreEqual(3, search.Parameters.Get<byte>("PublishedStatus"));

        // o filtro de situação existe num único arquivo da Infrastructure: nenhuma consulta o reescreve
        string folder = RepositoryHelper.Project("src/GazetaMarketplace.Infrastructure");
        string[] copies = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(f => !f.EndsWith("SqlFragments.cs", StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("Status = @", StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(copies, "Filtro de situação reescrito em: " + string.Join(", ", copies));
    }
}
