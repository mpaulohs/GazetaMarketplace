using System.Linq;
using GazetaMarketplace.Core.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Settings;

/// <summary>US-015: o telefone/WhatsApp do site, com rigor total (DDD oficial, celular com 9, +55 opcional) e guardado só com dígitos.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhoneNumberTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("(11) 91234-5678", "11912345678")]
    [DataRow("11 91234-5678", "11912345678")]
    [DataRow("11912345678", "11912345678")]
    [DataRow("+55 (11) 91234-5678", "11912345678")]
    [DataRow("5511912345678", "11912345678")]
    [DataRow("+5511912345678", "11912345678")]
    [DataRow("  (11) 91234-5678  ", "11912345678")]
    [DataRow("11.91234.5678", "11912345678")]
    [DataRow("(11) 3456-7890", "1134567890")]
    [DataRow("551134567890", "1134567890")]
    [DataRow("(55) 91234-5678", "55912345678")]
    [DataRow("+55 55 91234-5678", "55912345678")]
    public void FormatosAceitos_GuardamSoOsDigitosSemOPrefixoDoPais(string input, string expected)
    {
        Assert.AreEqual(PhoneParseResult.Valid, PhoneNumber.TryNormalize(input, out string digits), input);
        Assert.AreEqual(expected, digits, input);
    }

    [TestMethod]
    [DataRow("123")]
    [DataRow("(11) 1234-567")]
    [DataRow("(11) 81234-5678", DisplayName = "celular de 11 dígitos precisa começar com 9")]
    [DataRow("(11) 1234-5678", DisplayName = "fixo de 10 dígitos não começa com 0 nem 1")]
    [DataRow("(00) 91234-5678", DisplayName = "DDD inexistente")]
    [DataRow("(10) 91234-5678", DisplayName = "DDD inexistente")]
    [DataRow("(20) 91234-5678", DisplayName = "DDD inexistente")]
    [DataRow("(23) 91234-5678", DisplayName = "DDD inexistente")]
    [DataRow("(11) 91234-5678 ramal 2")]
    [DataRow("telefone")]
    [DataRow("+1 (11) 91234-5678")]
    [DataRow("+54 11 91234-5678")]
    [DataRow("11+912345678")]
    [DataRow("5611912345678", DisplayName = "13 dígitos que não começam com 55")]
    [DataRow("119123456789", DisplayName = "12 dígitos que não começam com 55")]
    [DataRow("(11) 91234-56789")]
    public void FormatosRecusados_SaoInvalidos(string input)
    {
        Assert.AreEqual(PhoneParseResult.Invalid, PhoneNumber.TryNormalize(input, out string digits), input);
        Assert.IsNull(digits);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void EmBranco_EVazio(string input)
    {
        Assert.AreEqual(PhoneParseResult.Empty, PhoneNumber.TryNormalize(input, out string digits));
        Assert.IsNull(digits);
    }

    [TestMethod]
    public void ListaDeDdds_Tem67Codigos_ComDoisDigitosCada()
    {
        Assert.HasCount(67, PhoneNumber.AreaCodes);
        Assert.IsTrue(PhoneNumber.AreaCodes.All(code => code.Length == 2 && code[0] != '0' && code[1] != '0'));
    }

    [TestMethod]
    public void TodoDdd_DaLista_EAceito_ENenhumForaDelaE()
    {
        for (int ddd = 0; ddd <= 99; ddd++)
        {
            string code = ddd.ToString("00");
            PhoneParseResult result = PhoneNumber.TryNormalize($"{code}912345678", out _);
            Assert.AreEqual(PhoneNumber.AreaCodes.Contains(code) ? PhoneParseResult.Valid : PhoneParseResult.Invalid, result, $"DDD {code}");
        }
    }

    [TestMethod]
    public void Format_ExibeCelularEFixo()
    {
        Assert.AreEqual("(11) 91234-5678", PhoneNumber.Format("11912345678"));
        Assert.AreEqual("(11) 3456-7890", PhoneNumber.Format("1134567890"));
        Assert.AreEqual("abc", PhoneNumber.Format("abc"), "o que não é número guardado volta como veio");
    }

    [TestMethod]
    public void NormalizarOQueFoiFormatado_DevolveOMesmoNumero()
    {
        foreach (string stored in new[] { "11912345678", "1134567890", "55912345678", "21987654321" })
        {
            Assert.AreEqual(PhoneParseResult.Valid, PhoneNumber.TryNormalize(PhoneNumber.Format(stored), out string digits), stored);
            Assert.AreEqual(stored, digits);
        }
    }
}
