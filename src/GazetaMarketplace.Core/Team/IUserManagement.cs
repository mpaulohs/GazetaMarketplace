using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Team;

/// <summary>
/// Gestão das contas da equipe pelo Administrador (US-014). As regras vivem aqui, não na tela: sempre resta ao menos um
/// Administrador ativo, ninguém desativa a própria conta e toda mudança grava em <c>AuditEntries</c> (RC-16).
/// Não há exclusão de conta, para os anúncios manterem o autor.
/// </summary>
public interface IUserManagement
{
    /// <summary>Todas as pessoas da equipe, ativas e desativadas, por ordem de nome.</summary>
    Task<IReadOnlyList<TeamMember>> ListAsync(CancellationToken cancellationToken);

    /// <summary>A pessoa com esse id, ou nulo se não existir.</summary>
    Task<TeamMember> FindAsync(int id, CancellationToken cancellationToken);

    /// <summary>Cria a conta ativa, com troca de senha obrigatória no primeiro acesso (S01, S05, S06, S07).</summary>
    Task<UserManagementResult> CreateAsync(NewTeamMember request, CancellationToken cancellationToken);

    /// <summary>Muda o papel; vale no próximo acesso da pessoa (S02, S09).</summary>
    Task<UserManagementResult> ChangeRoleAsync(int id, string role, CancellationToken cancellationToken);

    /// <summary>Desativa a conta e encerra as sessões dela em até 5 minutos (S03, S08, S09).</summary>
    Task<UserManagementResult> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>Reativa a conta, sem pedir nova senha (S04).</summary>
    Task<UserManagementResult> ReactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>Define uma senha provisória: a anterior deixa de valer, as sessões acabam e a troca é exigida no próximo acesso (S10).</summary>
    Task<UserManagementResult> ResetPasswordAsync(int id, string provisionalPassword, CancellationToken cancellationToken);
}
