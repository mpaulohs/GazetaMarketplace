using System.Security.Claims;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GazetaMarketplace.Web.Security;

/// <summary>Usuário e correlação da requisição atual, para a auditoria (RC-16). O id vem do claim NameIdentifier (Identity com chave int).</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int? UserId =>
        int.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

    public string CorrelationId => accessor.HttpContext?.TraceIdentifier;
}
