using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// A ação só existe em Development: fora dele a rota nem é encontrada e a resposta é <b>404</b>, igual a qualquer endereço que não existe
/// (a conferência acontece antes do login, então nem a página de entrada revela que a rota existe).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class DevelopmentOnlyAttribute : Attribute, IActionConstraintFactory
{
    public bool IsReusable => true;

    public IActionConstraint CreateInstance(IServiceProvider services) =>
        new Constraint(services.GetRequiredService<IHostEnvironment>().IsDevelopment());

    private sealed class Constraint(bool isDevelopment) : IActionConstraint
    {
        public int Order => 0;

        public bool Accept(ActionConstraintContext context) => isDevelopment;
    }
}
