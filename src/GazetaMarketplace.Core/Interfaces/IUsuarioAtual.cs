namespace GazetaMarketplace.Core.Interfaces;

/// <summary>Contexto da requisição para auditoria: quem age e o código de correlação do log.</summary>
public interface IUsuarioAtual
{
    /// <summary>Id do usuário logado; nulo fora de uma requisição autenticada (ação do sistema).</summary>
    int? UsuarioId { get; }

    string CorrelationId { get; }
}
