using System;
using System.Linq;
using System.Web;
using GazetaMarketplace.Core.Contact;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Contact;

/// <summary>O link do WhatsApp e o de ligar (US-004, tarefa 5.3): a mensagem vai codificada como URL e o título chega ao WhatsApp exatamente como está.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class WhatsAppLinkTests
#pragma warning restore CA1515
{
    private const string Page = "https://www.gazeta.example/anuncio/7/titulo";

    private const string Digits = "11912345678";

    // O texto que o WhatsApp vai mostrar: a query lida do jeito que o WhatsApp lê
    private static string Shown(string link)
    {
        Uri uri = new(link);
        var query = HttpUtility.ParseQueryString(uri.Query);
        CollectionAssert.AreEqual(new[] { "text" }, query.AllKeys, "só o parâmetro text: nada do título pode virar outro parâmetro");
        return query["text"];
    }

    [TestMethod]
    public void Link_TemONumeroComOCodigoDoPaisEAMensagemFixa()
    {
        string link = WhatsAppLink.Build(Digits, "Honda Civic 2018", Page);

        StringAssert.StartsWith(link, "https://wa.me/5511912345678?text=");
        Assert.AreEqual("Olá! Tenho interesse no anúncio “Honda Civic 2018”: " + Page, Shown(link));
    }

    [TestMethod]
    [DataRow("Sítio \"Boa Vista\" & Cia")]
    [DataRow("Casa 100% reformada #1")]
    [DataRow("Pergunta? Sim = sim + não")]
    [DataRow("Ação & reação — çãõ ÀÉÎ")]
    [DataRow("Moto 🏍️ nova")]
    [DataRow("Linha um\nlinha dois")]
    [DataRow("Tab\taqui e <b>tag</b> e 'aspas simples'")]
    public void TituloComAcentosAspasESimbolos_VaiCodificado_EChegaIgual(string title)
    {
        string link = WhatsAppLink.Build(Digits, title, Page);

        Assert.AreEqual("Olá! Tenho interesse no anúncio “" + title + "”: " + Page, Shown(link));
        string encoded = link[(link.IndexOf("?text=", StringComparison.Ordinal) + "?text=".Length)..];
        Assert.IsFalse(encoded.Any(c => c is '&' or '#' or '?' or ' ' or '"' or '=' or '+' or '<' or '>' or '\n' or '\t' or '“' or 'á' or 'í' or 'ç'), "só caracteres seguros: " + encoded);
    }

    [TestMethod]
    public void EComercial_NoTitulo_VaiComo26_ENaoPartoOLink()
    {
        string link = WhatsAppLink.Build(Digits, "Sítio \"Boa Vista\" & Cia", Page);

        StringAssert.Contains(link, "%26");
        Assert.AreEqual(1, link.Count(c => c == '?'));
        Assert.IsFalse(link.Contains('&', StringComparison.Ordinal));
    }

    [TestMethod]
    public void Emoji_VaiEmUtf8_ParaOWhatsAppLerOParInteiro()
    {
        string link = WhatsAppLink.Build(Digits, "Moto 🏍 nova", Page);

        StringAssert.Contains(link, "%F0%9F%8F%8D");
        StringAssert.Contains(Shown(link), "🏍");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void TituloVazio_MensagemSemAspasVazias(string title)
    {
        Assert.AreEqual("Olá! Tenho interesse neste anúncio: " + Page, WhatsAppLink.Message(title, Page));
        Assert.AreEqual("Olá! Tenho interesse neste anúncio: " + Page, Shown(WhatsAppLink.Build(Digits, title, Page)));
    }

    [TestMethod]
    public void Titulo500Caracteres_CortaEm120_SemReticencias()
    {
        string title = string.Concat(Enumerable.Range(0, 500).Select(i => (char)('a' + (i % 26))));

        string shown = Shown(WhatsAppLink.Build(Digits, title, Page));

        Assert.AreEqual("Olá! Tenho interesse no anúncio “" + title[..120] + "”: " + Page, shown);
        Assert.IsFalse(shown.Contains('…', StringComparison.Ordinal) || shown.Contains("...", StringComparison.Ordinal));
        Assert.IsTrue(WhatsAppLink.Build(Digits, title, Page).Length < 700, "o endereço completo cabe com folga");
    }

    [TestMethod]
    [DataRow(119)]
    [DataRow(120)]
    public void TituloAte120_NaoSofreCorte(int length)
    {
        string title = new('x', length);

        Assert.AreEqual("Olá! Tenho interesse no anúncio “" + title + "”: " + Page, WhatsAppLink.Message(title, Page));
    }

    [TestMethod]
    public void Titulo121_PerdeSoOUltimoCaractere()
    {
        string title = new string('x', 120) + "y";

        Assert.AreEqual("Olá! Tenho interesse no anúncio “" + new string('x', 120) + "”: " + Page, WhatsAppLink.Message(title, Page));
    }

    [TestMethod]
    public void CorteNoMeioDeUmEmoji_DeixaOParInteiroDeFora()
    {
        string title = new string('x', 119) + "🏍" + "resto"; // o emoji ocupa as posições 119 e 120

        string message = WhatsAppLink.Message(title, Page);

        Assert.AreEqual("Olá! Tenho interesse no anúncio “" + new string('x', 119) + "”: " + Page, message);
        Assert.IsFalse(message.Any(char.IsSurrogate), "nenhum par partido ao meio");
        Assert.IsFalse(Shown(WhatsAppLink.Build(Digits, title, Page)).Contains('�', StringComparison.Ordinal));
    }

    [TestMethod]
    public void Corte_NaoDeixaEspacoAntesDasAspas()
    {
        string title = new string('x', 119) + " fim";

        Assert.AreEqual("Olá! Tenho interesse no anúncio “" + new string('x', 119) + "”: " + Page, WhatsAppLink.Message(title, Page));
    }

    [TestMethod]
    public void Telefone_FixoComDezDigitos_VaiComoVeio()
    {
        StringAssert.StartsWith(WhatsAppLink.Build("1134567890", "x", Page), "https://wa.me/551134567890?text=");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public void SemTelefoneOuSemEndereco_RecusaMontarOLink(string value)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => WhatsAppLink.Build(null, "x", Page));
        Assert.ThrowsExactly<ArgumentException>(() => WhatsAppLink.Build("", "x", Page));
        Assert.ThrowsExactly<ArgumentException>(() => WhatsAppLink.Build(Digits, "x", "  "));
        Assert.ThrowsExactly<ArgumentException>(() => PhoneLink.Tel(value is null ? string.Empty : value));
    }

    [TestMethod]
    [DataRow("11912345678", "tel:+5511912345678")]
    [DataRow("1134567890", "tel:+551134567890")]
    public void Ligar_UsaTelComOCodigoDoPais(string digits, string expected)
    {
        Assert.AreEqual(expected, PhoneLink.Tel(digits));
    }
}
