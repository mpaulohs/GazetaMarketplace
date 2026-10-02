using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SqlBuilderTests
#pragma warning restore CA1515
{
    private static readonly Dictionary<string, string> Permitidas = new()
    {
        ["preco"] = "a.PriceCents",
        ["recentes"] = "a.PublishedAt"
    };

    private static SqlBuilder Base() => new SqlBuilder().Select("a.Id, a.Title").From("Ads a");

    [TestMethod]
    public void Valores_SempreViramParametros()
    {
        const string malicioso = "x'; DROP TABLE Ads; --";

        ConsultaSql consulta = Base()
            .Where("a.Title LIKE @Titulo")
            .Parametro("Titulo", malicioso)
            .OrderBy(null, null, Permitidas, "a.PublishedAt DESC")
            .Build();

        Assert.DoesNotContain("DROP", consulta.Sql);
        Assert.DoesNotContain("x'", consulta.Sql);
        Assert.AreEqual(malicioso, consulta.Parametros.Get<string>("Titulo"));
        StringAssert.Contains(consulta.Sql, "WHERE a.Title LIKE @Titulo");
    }

    [TestMethod]
    [DataRow("a.Title = 'x'")]
    [DataRow("a.Id = 5 OR 1 = 1")]
    [DataRow("a.Title LIKE @Titulo; DROP TABLE Ads")]
    [DataRow("a.Title = @Titulo -- comentário")]
    [DataRow("a.Title = @Titulo /* x */")]
    [DataRow("")]
    public void FragmentoComLiteralOuTextoPerigoso_E_Recusado(string fragmento)
    {
        Assert.ThrowsExactly<ArgumentException>(() => Base().Where(fragmento));
    }

    [TestMethod]
    public void ParametroSemValor_FalhaNoBuild()
    {
        SqlBuilder builder = Base().Where("a.Title LIKE @Titulo").OrderBy(null, null, Permitidas, "a.Id");

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    [TestMethod]
    public void NomeDeParametroInvalido_E_Recusado()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Base().Parametro("a; DROP", 1));
    }

    [TestMethod]
    public void OrdenacaoForaDaLista_EIgnorada()
    {
        ConsultaSql foraDaLista = Base().OrderBy("preco; DROP TABLE Ads", "desc; --", Permitidas, "a.PublishedAt DESC").Build();
        ConsultaSql direcaoInvalida = Base().OrderBy("preco", "sideways", Permitidas, "a.PublishedAt DESC").Build();
        ConsultaSql valida = Base().OrderBy("preco", "desc", Permitidas, "a.PublishedAt DESC").Build();

        StringAssert.EndsWith(foraDaLista.Sql, "ORDER BY a.PublishedAt DESC");
        Assert.DoesNotContain("DROP", foraDaLista.Sql);
        StringAssert.EndsWith(direcaoInvalida.Sql, "ORDER BY a.PriceCents ASC");
        StringAssert.EndsWith(valida.Sql, "ORDER BY a.PriceCents DESC");
    }

    [TestMethod]
    public void Paginar_GeraOffsetFetch_ComParametros()
    {
        ConsultaSql consulta = Base().OrderBy(null, null, Permitidas, "a.Id").Paginar(3, 24).Build();

        StringAssert.EndsWith(consulta.Sql, "OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY");
        Assert.AreEqual(48, consulta.Parametros.Get<int>("Offset"));
        Assert.AreEqual(24, consulta.Parametros.Get<int>("Take"));
    }

    [TestMethod]
    public void Paginar_SemOrdenacao_FalhaNoBuild()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Base().Paginar(1, 20).Build());
    }

    [TestMethod]
    [DataRow(0, 20)]
    [DataRow(1, 0)]
    [DataRow(1, 101)]
    public void Paginar_ComValoresForaDoIntervalo_E_Recusado(int pagina, int tamanho)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Base().Paginar(pagina, tamanho));
    }

    [TestMethod]
    public void FragmentoSomentePublicados_E_UnicoEReutilizado()
    {
        ConsultaSql busca = Base().SomentePublicados().Where("a.Title LIKE @Titulo").Parametro("Titulo", "%a%").OrderBy(null, null, Permitidas, "a.Id").Build();
        ConsultaSql lista = new SqlBuilder().Select("a.Id").From("Ads a").SomentePublicados().OrderBy(null, null, Permitidas, "a.Id").Build();

        StringAssert.Contains(busca.Sql, SqlFragments.SomentePublicados);
        StringAssert.Contains(lista.Sql, SqlFragments.SomentePublicados);
        Assert.AreEqual(3, busca.Parametros.Get<byte>("StatusPublicado"));

        // o filtro de situação existe num único arquivo da Infrastructure: nenhuma consulta o reescreve
        string pasta = RepositorioHelper.Projeto("src/GazetaMarketplace.Infrastructure");
        string[] copias = Directory.EnumerateFiles(pasta, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(f => !f.EndsWith("SqlFragments.cs", StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("Status = @", StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(copias, "Filtro de situação reescrito em: " + string.Join(", ", copias));
    }
}
