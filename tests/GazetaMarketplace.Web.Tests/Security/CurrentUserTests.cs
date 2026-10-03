using System.Security.Claims;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CurrentUserTests
#pragma warning restore CA1515
{
    private sealed class Accessor(HttpContext context) : IHttpContextAccessor
    {
        public HttpContext HttpContext { get; set; } = context;
    }

    [TestMethod]
    public void UsuarioLogado_DevolveOId_ECorrelationId()
    {
        DefaultHttpContext context = new() { TraceIdentifier = "corr-9" };
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "12")], "teste"));

        HttpCurrentUser user = new(new Accessor(context));

        Assert.AreEqual(12, user.UserId);
        Assert.AreEqual("corr-9", user.CorrelationId);
    }

    [TestMethod]
    public void SemLoginOuForaDeRequisicao_UsuarioENulo()
    {
        Assert.IsNull(new HttpCurrentUser(new Accessor(new DefaultHttpContext())).UserId);
        HttpCurrentUser withoutContext = new(new Accessor(null));
        Assert.IsNull(withoutContext.UserId);
        Assert.IsNull(withoutContext.CorrelationId);
    }

    [TestMethod]
    public void ClaimQueNaoEInteiro_UsuarioENulo()
    {
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "nao-e-numero")], "teste"));

        Assert.IsNull(new HttpCurrentUser(new Accessor(context)).UserId);
    }
}
