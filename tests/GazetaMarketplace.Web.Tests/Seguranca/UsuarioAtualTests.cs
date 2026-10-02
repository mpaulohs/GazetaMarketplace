using System.Security.Claims;
using GazetaMarketplace.Web.Seguranca;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class UsuarioAtualTests
#pragma warning restore CA1515
{
    private sealed class Acessor(HttpContext contexto) : IHttpContextAccessor
    {
        public HttpContext HttpContext { get; set; } = contexto;
    }

    [TestMethod]
    public void UsuarioLogado_DevolveOId_ECorrelationId()
    {
        DefaultHttpContext contexto = new() { TraceIdentifier = "corr-9" };
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "12")], "teste"));

        UsuarioAtualHttp usuario = new(new Acessor(contexto));

        Assert.AreEqual(12, usuario.UsuarioId);
        Assert.AreEqual("corr-9", usuario.CorrelationId);
    }

    [TestMethod]
    public void SemLoginOuForaDeRequisicao_UsuarioENulo()
    {
        Assert.IsNull(new UsuarioAtualHttp(new Acessor(new DefaultHttpContext())).UsuarioId);
        UsuarioAtualHttp semContexto = new(new Acessor(null));
        Assert.IsNull(semContexto.UsuarioId);
        Assert.IsNull(semContexto.CorrelationId);
    }

    [TestMethod]
    public void ClaimQueNaoEInteiro_UsuarioENulo()
    {
        DefaultHttpContext contexto = new();
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "nao-e-numero")], "teste"));

        Assert.IsNull(new UsuarioAtualHttp(new Acessor(contexto)).UsuarioId);
    }
}
