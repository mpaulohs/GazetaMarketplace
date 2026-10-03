namespace GazetaMarketplace.Core.Interfaces;

/// <summary>Contexto da requisição para auditoria: quem age e o código de correlação do log.</summary>
public interface ICurrentUser
{
    /// <summary>Id do usuário logado; nulo fora de uma requisição autenticada (ação do sistema).</summary>
    int? UserId { get; }

    string CorrelationId { get; }
}
