using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>Ignora os testes de navegador quando GAZETA_BASE_URL não aponta para um site no ar (o /build não sobe o site; o /test sim).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class ExigeSiteNoArAttribute : ConditionBaseAttribute
{
    public const string Variavel = "GAZETA_BASE_URL";

    public ExigeSiteNoArAttribute() : base(ConditionMode.Include)
    {
        IgnoreMessage = "Defina " + Variavel + " com o endereço do site no ar para rodar os testes de navegador (roda no /test).";
    }

    public override bool IsConditionMet => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variavel));

    public override string GroupName => nameof(ExigeSiteNoArAttribute);
}
