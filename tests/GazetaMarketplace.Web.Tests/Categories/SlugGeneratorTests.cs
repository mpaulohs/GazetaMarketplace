using System;
using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Categories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>Regra de slug das categorias (decisão do Product Owner, 2026-10-03).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SlugGeneratorTests
#pragma warning restore CA1515
{
    private static HashSet<string> Taken(params string[] slugs) => [.. slugs];

    [TestMethod]
    [DataRow("Automóveis, Peças e Acessórios", "automoveis-pecas-e-acessorios")]
    [DataRow("Terrenos, sítios e fazendas", "terrenos-sitios-e-fazendas")]
    [DataRow("CDs, DVDs etc", "cds-dvds-etc")]
    [DataRow("  Móveis   Infantis  ", "moveis-infantis")]
    [DataRow("Ar-condicionados", "ar-condicionados")]
    [DataRow("Eletro & Casa!", "eletro-casa")]
    [DataRow("Caminhões e Ônibus", "caminhoes-e-onibus")]
    [DataRow("Maternidade e Cuidados com o Bebê", "maternidade-e-cuidados-com-o-bebe")]
    [DataRow("TVs e video", "tvs-e-video")]
    [DataRow("Ação/Reação", "acaoreacao")]
    [DataRow("A_B  -  C", "a-b-c")]
    [DataRow("4x4 Ç", "4x4-c")]
    [DataRow("---", "categoria")]
    [DataRow("", "categoria")]
    [DataRow(null, "categoria")]
    [DataRow("¿?!", "categoria")]
    public void Slugify_SemAcento_Minusculas_HifenNoLugarDeEspaco_SemEspeciais(string name, string expected)
    {
        Assert.AreEqual(expected, SlugGenerator.Slugify(name));
    }

    [TestMethod]
    public void Slugify_NomeLongo_CabeNoLimite_ENaoTerminaEmHifen()
    {
        string name = string.Join(' ', Enumerable.Repeat("palavra", 40));

        string slug = SlugGenerator.Slugify(name);

        Assert.IsLessThanOrEqualTo(SlugGenerator.MaxLength, slug.Length);
        Assert.IsFalse(slug.EndsWith('-'));
        StringAssert.StartsWith(slug, "palavra-palavra");
    }

    [TestMethod]
    public void Unique_SlugLivre_DevolveOLimpo()
    {
        Assert.AreEqual("servicos", SlugGenerator.Unique("Serviços", isPostable: true, new HashSet<string>()));
    }

    [TestMethod]
    public void Unique_PostavelQueColide_GanhaNumero()
    {
        HashSet<string> taken = ["servicos", "servicos-2"];

        Assert.AreEqual("servicos-3", SlugGenerator.Unique("Serviços", isPostable: true, taken));
    }

    [TestMethod]
    public void Unique_NaoPostavelQueColide_GanhaGrupo_ESeColidirDeNovo_GrupoComNumero()
    {
        Assert.AreEqual("servicos-grupo", SlugGenerator.Unique("Serviços", isPostable: false, Taken("servicos")));
        Assert.AreEqual("servicos-grupo-2", SlugGenerator.Unique("Serviços", isPostable: false, Taken("servicos", "servicos-grupo")));
        Assert.AreEqual("servicos-grupo-3", SlugGenerator.Unique("Serviços", isPostable: false, Taken("servicos", "servicos-grupo", "servicos-grupo-2")));
    }

    [TestMethod]
    public void Unique_ComSufixo_NuncaPassaDoLimite()
    {
        string longName = new('a', 200);
        string clean = SlugGenerator.Slugify(longName);

        string postable = SlugGenerator.Unique(longName, isPostable: true, Taken(clean));
        string group = SlugGenerator.Unique(longName, isPostable: false, Taken(clean));

        Assert.IsLessThanOrEqualTo(SlugGenerator.MaxLength, postable.Length);
        Assert.IsLessThanOrEqualTo(SlugGenerator.MaxLength, group.Length);
        Assert.IsTrue(postable.EndsWith("-2", StringComparison.Ordinal));
        Assert.IsTrue(group.EndsWith("-grupo", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AssignInitial_OsQuatroCasosDoProductOwner_PostavelGanhaOLimpo()
    {
        // Serviços (mãe, 7) e Serviços (filha, 66); Vagas de emprego (mãe, 13) e Vagas de emprego (filha, 96)
        IReadOnlyDictionary<int, string> slugs = SlugGenerator.AssignInitial(
        [
            new SlugCandidate(7, "Serviços", false, null),
            new SlugCandidate(13, "Vagas de emprego", false, null),
            new SlugCandidate(66, "Serviços", true, null),
            new SlugCandidate(96, "Vagas de emprego", true, null)
        ]);

        Assert.AreEqual("servicos-grupo", slugs[7]);
        Assert.AreEqual("servicos", slugs[66]);
        Assert.AreEqual("vagas-de-emprego-grupo", slugs[13]);
        Assert.AreEqual("vagas-de-emprego", slugs[96]);
    }

    [TestMethod]
    public void AssignInitial_DuasPostaveis_MenorIdGanhaOLimpo()
    {
        IReadOnlyDictionary<int, string> slugs = SlugGenerator.AssignInitial(
        [
            new SlugCandidate(20, "Mesas", true, null),
            new SlugCandidate(10, "Mesas", true, null)
        ]);

        Assert.AreEqual("mesas", slugs[10]);
        Assert.AreEqual("mesas-2", slugs[20]);
    }

    [TestMethod]
    public void AssignInitial_DuasNaoPostaveis_MenorIdGanhaOLimpo_AOutraGanhaGrupo()
    {
        IReadOnlyDictionary<int, string> slugs = SlugGenerator.AssignInitial(
        [
            new SlugCandidate(20, "Casa", false, null),
            new SlugCandidate(10, "Casa", false, null)
        ]);

        Assert.AreEqual("casa", slugs[10]);
        Assert.AreEqual("casa-grupo", slugs[20]);
    }

    [TestMethod]
    public void AssignInitial_SlugExplicito_ValeComoEsta_EGeradoQueColidirRecebeSufixo()
    {
        IReadOnlyDictionary<int, string> slugs = SlugGenerator.AssignInitial(
        [
            new SlugCandidate(33, "Carros, vans e utilitários", true, "cars"),
            new SlugCandidate(90, "Cars", true, null),
            new SlugCandidate(91, "Cars", false, null)
        ]);

        Assert.AreEqual("cars", slugs[33]);
        Assert.AreEqual("cars-2", slugs[90], "o explícito tem o limpo; o postável gerado ganha número");
        Assert.AreEqual("cars-grupo", slugs[91]);
    }

    [TestMethod]
    public void AssignInitial_NaoDependeDaOrdemDasLinhas()
    {
        SlugCandidate[] rows =
        [
            new(7, "Serviços", false, null), new(66, "Serviços", true, null), new(13, "Vagas", false, null),
            new(96, "Vagas", true, null), new(40, "Mesas", true, null), new(41, "Mesas", true, null), new(5, "Casa", false, "casa")
        ];

        IReadOnlyDictionary<int, string> forward = SlugGenerator.AssignInitial(rows);
        IReadOnlyDictionary<int, string> backward = SlugGenerator.AssignInitial(rows.Reverse());

        CollectionAssert.AreEquivalent(forward.ToList(), backward.ToList());
    }

    [TestMethod]
    public void AssignInitial_ResultadoNuncaRepeteSlug()
    {
        SlugCandidate[] rows = [.. Enumerable.Range(1, 30).Select(i => new SlugCandidate(i, "Mesa", i % 3 != 0, null))];

        IReadOnlyDictionary<int, string> slugs = SlugGenerator.AssignInitial(rows);

        Assert.AreEqual(30, slugs.Values.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void AssignInitial_DoisSlugsExplicitosIguais_Recusa()
    {
        Assert.ThrowsExactly<ArgumentException>(() => SlugGenerator.AssignInitial(
        [
            new SlugCandidate(1, "A", true, "x"),
            new SlugCandidate(2, "B", true, "x")
        ]));
    }
}
