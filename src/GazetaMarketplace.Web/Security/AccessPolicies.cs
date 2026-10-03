namespace GazetaMarketplace.Web.Security;

/// <summary>Nomes das políticas de autorização (ADR-003). A checagem de autoria fica no serviço de aplicação.</summary>
public static class AccessPolicies
{
    /// <summary>Só o Administrador.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Redator ou Administrador: o Administrador faz tudo o que o Redator faz nos anúncios.</summary>
    public const string Writer = "Writer";
}

/// <summary>Claims extras que a equipe leva no cookie.</summary>
public static class TeamClaims
{
    /// <summary>Nome completo mostrado no topo do painel.</summary>
    public const string FullName = "full_name";

    /// <summary>"1" enquanto a senha for provisória; o filtro do painel não precisa ir ao banco para saber.</summary>
    public const string MustChangePassword = "must_change_password";
}
