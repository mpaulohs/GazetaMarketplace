using Microsoft.AspNetCore.Identity;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <summary>
/// Pessoa da equipe (ADR-003). Chave <c>int</c>; o e-mail é também o nome de usuário.
/// Só a equipe tem conta: o visitante do site não entra.
/// </summary>
public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; }

    /// <summary>Conta desativada não entra e perde a sessão na próxima revalidação.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Verdadeiro com senha provisória: o painel exige a troca antes de mostrar qualquer página (S6).</summary>
    public bool MustChangePassword { get; set; }
}
