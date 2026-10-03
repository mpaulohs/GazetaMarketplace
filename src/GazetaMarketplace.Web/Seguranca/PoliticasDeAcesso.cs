namespace GazetaMarketplace.Web.Seguranca;

/// <summary>Nomes das políticas de autorização (ADR-003). A checagem de autoria fica no serviço de aplicação.</summary>
public static class PoliticasDeAcesso
{
    /// <summary>Só o Administrador.</summary>
    public const string Administrador = "Administrador";

    /// <summary>Redator ou Administrador: o Administrador faz tudo o que o Redator faz nos anúncios.</summary>
    public const string Redator = "Redator";
}

/// <summary>Claims extras que a equipe leva no cookie.</summary>
public static class ClaimsDaEquipe
{
    /// <summary>Nome completo mostrado no topo do painel.</summary>
    public const string NomeCompleto = "nome_completo";
}
