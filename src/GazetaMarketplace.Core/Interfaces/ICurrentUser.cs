namespace GazetaMarketplace.Core.Interfaces;

/// <summary>Contexto da requisição para auditoria: quem age e o código de correlação do log.</summary>
public interface ICurrentUser
{
    /// <summary>Id do usuário logado; nulo fora de uma requisição autenticada (ação do sistema).</summary>
    int? UserId { get; }

    /// <summary>Se o usuário logado tem o papel de Administrador (decide o que pode fazer em anúncios; ver <c>AdAccess</c>).</summary>
    bool IsAdministrator { get; }

    string CorrelationId { get; }
}
