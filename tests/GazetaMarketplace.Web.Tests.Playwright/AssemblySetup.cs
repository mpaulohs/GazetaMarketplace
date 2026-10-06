using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>Configuração única da rodada de navegador.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public static class AssemblySetup
#pragma warning restore CA1515
{
    /// <summary>
    /// As esperas de <c>Expect(...)</c> passam de 5 s (o padrão) para 15 s. Com a rodada completa (quatro navegadores em paralelo, mais de 240 casos) o servidor de teste fica mais lento e
    /// uma espera de 5 s por uma página nova (por exemplo, a confirmação "Enviar para revisão?") esgotava em um caso por rodada, sem nenhum defeito no produto: o mesmo caso passa isolado.
    /// Esperar mais não esconde falha: um elemento que nunca aparece continua falhando, só 10 s depois.
    /// </summary>
    [AssemblyInitialize]
    public static void SetExpectTimeout(TestContext context) => Assertions.SetDefaultExpectTimeout(15_000);
}
