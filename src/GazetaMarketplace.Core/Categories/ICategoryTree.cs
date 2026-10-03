using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Categories;

/// <summary>
/// A árvore de categorias em cache de memória (10 minutos). O cache vale para o processo inteiro e é esvaziado por
/// <see cref="Invalidate"/>, que o <c>AppDbContext</c> chama sozinho depois de gravar qualquer categoria.
/// </summary>
public interface ICategoryTree
{
    /// <summary>A árvore atual: do cache, ou carregada do banco numa única consulta se o cache estiver vazio ou vencido.</summary>
    Task<CategoryTreeSnapshot> GetAsync(CancellationToken cancellationToken);

    /// <summary>Descarta o cache; a próxima leitura vai ao banco. Chame depois de gravar fora do <c>SaveChanges</c> (por exemplo, <c>ExecuteUpdate</c>).</summary>
    void Invalidate();
}
