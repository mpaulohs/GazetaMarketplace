using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>
/// Ignora o teste quando alguma variável de ambiente não está definida. Os E2E que entram no painel precisam de uma
/// conta real no site de teste (o /test cria o banco e a conta) e, para a sessão expirada, de um site com a sessão curta.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class RequiresVariablesAttribute : ConditionBaseAttribute
{
    private readonly string[] _variables;

    public RequiresVariablesAttribute(params string[] variables)
        : base(ConditionMode.Include)
    {
        _variables = variables;
        IgnoreMessage = "Defina " + string.Join(", ", variables) + " para rodar este teste de navegador (roda no /test).";
    }

    public override bool IsConditionMet => _variables.All(v => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(v)));

    public override string GroupName => nameof(RequiresVariablesAttribute) + string.Join("+", _variables);

    public static string Value(string name) => Environment.GetEnvironmentVariable(name);
}
