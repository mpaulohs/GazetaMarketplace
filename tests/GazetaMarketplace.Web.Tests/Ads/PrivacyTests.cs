using System.Linq;
using System.Reflection;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>NFR-19 e a regra da US-008: o anúncio não guarda nome, telefone nem e-mail de vendedor ou comprador, e não tem campo para eles.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PrivacyTests
#pragma warning restore CA1515
{
    private static readonly string[] Forbidden =
    [
        "seller", "buyer", "vendedor", "comprador", "phone", "telefone", "email", "mail", "whatsapp", "contact", "contato", "fullname", "cpf", "cnpj", "owner"
    ];

    private static bool IsPersonal(string name) => Forbidden.Any(token => name.Contains(token, System.StringComparison.OrdinalIgnoreCase));

    [TestMethod]
    public void Modelo_NaoTemCamposDePessoaDoVendedor()
    {
        foreach (System.Type type in new[] { typeof(Ad), typeof(AdPhoto) })
        {
            string[] offenders = [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).Where(IsPersonal)];
            Assert.IsEmpty(offenders, $"{type.Name} não pode ter campo de pessoa: {string.Join(", ", offenders)}");
        }
    }

    [TestMethod]
    public void CamposDosDezoitoGrupos_NaoPedemDadoDePessoa()
    {
        Assert.HasCount(18, FieldGroupRegistry.All);
        foreach (FieldGroup group in FieldGroupRegistry.All)
        {
            foreach (FieldDefinition field in group.Fields)
            {
                Assert.IsFalse(IsPersonal(field.Key), $"{group.Key}.{field.Key}");
                Assert.IsFalse(IsPersonal(field.Label), $"{group.Key}.{field.Label}");
            }
        }
    }

    [TestMethod]
    public void OUnicoCampoDePessoaDoAnuncio_EhOIdDoAutorDaEquipe()
    {
        // A autoria é a conta da equipe (AspNetUsers), nunca o vendedor do bem
        string[] ids = [.. typeof(Ad).GetProperties().Select(p => p.Name).Where(n => n.EndsWith("ById", System.StringComparison.Ordinal) || n == nameof(Ad.AuthorId))];
        CollectionAssert.AreEquivalent(new[] { "AuthorId", "PublishedById", "RejectedById" }, ids);
    }
}
