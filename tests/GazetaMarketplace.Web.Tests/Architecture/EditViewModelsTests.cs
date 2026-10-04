using System;
using System.Linq;
using System.Reflection;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Areas.Panel.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Architecture;

/// <summary>
/// RC-14: o que o navegador envia ao salvar um anúncio não pode carregar decisão. Situação, autor, datas de publicação e motivo de rejeição
/// são do servidor; se um dia aparecerem no tipo que recebe o formulário, qualquer pessoa da equipe poderia publicar o próprio anúncio.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class EditViewModelsTests
#pragma warning restore CA1515
{
    private static readonly string[] DecisionProperties =
    [
        "Status", "AuthorId", "Author", "PublishedAt", "PublishedById", "RejectedAt", "RejectedById", "RejectionReason", "SentAt", "ArchivedAt",
        "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "Id", "TitleSearch", "DescriptionSearch", "Attributes"
    ];

    private static string[] PropertiesOf(Type type) =>
        [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name)];

    [TestMethod]
    public void FormularioDeEdicao_NaoTemCamposDeDecisao()
    {
        string[] found = [.. PropertiesOf(typeof(AdFormSubmission)).Intersect(DecisionProperties, StringComparer.OrdinalIgnoreCase)];

        Assert.IsEmpty(found, "campos de decisão no formulário: " + string.Join(", ", found));
    }

    [TestMethod]
    public void EntradaDoServicoDoRascunho_NaoTemCamposDeDecisao()
    {
        string[] found = [.. PropertiesOf(typeof(AdDraftInput)).Intersect(DecisionProperties, StringComparer.OrdinalIgnoreCase)];

        Assert.IsEmpty(found, "campos de decisão na entrada do serviço: " + string.Join(", ", found));
    }

    [TestMethod]
    public void OFormularioEnviaSoTexto_ComAChaveDoCampoNoDicionario()
    {
        PropertyInfo[] properties = [.. typeof(AdFormSubmission).GetProperties(BindingFlags.Public | BindingFlags.Instance)];

        foreach (PropertyInfo property in properties)
        {
            Assert.IsTrue(
                property.PropertyType == typeof(string) || property.PropertyType == typeof(int?) || property.PropertyType == typeof(bool)
                || property.PropertyType == typeof(System.Collections.Generic.Dictionary<string, string[]>),
                $"{property.Name} é {property.PropertyType}: o formulário só traz texto, o número da categoria e dois sinais");
        }
    }
}
