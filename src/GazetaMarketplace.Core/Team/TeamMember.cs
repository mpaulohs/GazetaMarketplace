namespace GazetaMarketplace.Core.Team;

/// <summary>Uma pessoa da equipe como a tela de usuários a mostra. <c>Role</c> é um dos valores de <see cref="RoleNames"/>.</summary>
public sealed record TeamMember(int Id, string FullName, string Email, string Role, bool IsActive);

/// <summary>Dados para criar uma conta da equipe (US-014-S01). A senha é provisória: a pessoa a troca no primeiro acesso.</summary>
public sealed record NewTeamMember(string FullName, string Email, string Role, string ProvisionalPassword);
