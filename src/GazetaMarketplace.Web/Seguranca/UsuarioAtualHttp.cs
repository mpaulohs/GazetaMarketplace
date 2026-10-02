using System.Security.Claims;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>Usuário e correlação da requisição atual, para a auditoria (RC-16). O id vem do claim NameIdentifier (Identity com chave int).</summary>
public sealed class UsuarioAtualHttp(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public int? UsuarioId =>
        int.TryParse(acessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

    public string CorrelationId => acessor.HttpContext?.TraceIdentifier;
}
