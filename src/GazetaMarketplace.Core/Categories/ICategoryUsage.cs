using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Categories;

/// <summary>
/// Quantos anúncios usam cada categoria, em qualquer situação (rascunho, em revisão, publicado, rejeitado ou arquivado). Alimenta a coluna
/// "Anúncios" da tela de categorias e as regras de exclusão (US-013-S08) e de criar subcategoria dentro de uma folha.
/// </summary>
/// <remarks>Provisório até a tarefa 3.1: a tabela de anúncios ainda não existe, então a implementação devolve sempre zero.</remarks>
public interface ICategoryUsage
{
    /// <summary>Contagem de anúncios por id de categoria; todo id pedido aparece no resultado (com zero se não houver anúncios).</summary>
    Task<IReadOnlyDictionary<int, int>> CountAdsAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken);
}
