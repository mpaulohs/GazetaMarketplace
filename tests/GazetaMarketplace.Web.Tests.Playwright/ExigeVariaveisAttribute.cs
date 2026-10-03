using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>
/// Ignora o teste quando alguma variável de ambiente não está definida. Os E2E que entram no painel precisam de uma
/// conta real no site de teste (o /test cria o banco e a conta) e, para a sessão expirada, de um site com a sessão curta.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class ExigeVariaveisAttribute : ConditionBaseAttribute
{
    private readonly string[] _variaveis;

    public ExigeVariaveisAttribute(params string[] variaveis)
        : base(ConditionMode.Include)
    {
        _variaveis = variaveis;
        IgnoreMessage = "Defina " + string.Join(", ", variaveis) + " para rodar este teste de navegador (roda no /test).";
    }

    public override bool IsConditionMet => _variaveis.All(v => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(v)));

    public override string GroupName => nameof(ExigeVariaveisAttribute) + string.Join("+", _variaveis);

    public static string Valor(string nome) => Environment.GetEnvironmentVariable(nome);
}
