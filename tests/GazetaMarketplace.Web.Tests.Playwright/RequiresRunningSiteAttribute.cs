using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>Ignora os testes de navegador quando GAZETA_BASE_URL não aponta para um site no ar (o /build não sobe o site; o /test sim).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class RequiresRunningSiteAttribute : ConditionBaseAttribute
{
    public const string Variable = "GAZETA_BASE_URL";

    public RequiresRunningSiteAttribute() : base(ConditionMode.Include)
    {
        IgnoreMessage = "Defina " + Variable + " com o endereço do site no ar para rodar os testes de navegador (roda no /test).";
    }

    public override bool IsConditionMet => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable));

    public override string GroupName => nameof(RequiresRunningSiteAttribute);
}
