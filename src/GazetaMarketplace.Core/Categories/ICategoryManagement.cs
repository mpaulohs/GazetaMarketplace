using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Categories;

/// <summary>Textos das recusas da tela de categorias (US-013). Os de S06 a S10 são os da SPEC; os demais seguem o mesmo tom.</summary>
public static class CategoryMessages
{
    public const string EmptyName = "Informe o nome da categoria";

    public const string NameTooLong = "O nome da categoria pode ter no máximo 100 caracteres";

    public const string DuplicateName = "Já existe uma categoria com esse nome neste grupo";

    public const string ParentNotFound = "Escolha uma categoria pai válida";

    public const string TooDeep = "As categorias vão até o terceiro nível: escolha outra categoria pai";

    public const string HasSubcategories = "Exclua ou mova antes as subcategorias desta categoria";

    public const string Protected = "Esta categoria é usada pelos campos específicos e não pode ser excluída. Você pode renomeá-la.";

    /// <summary>Não está na SPEC (suposição registrada no BACKLOG): criar subcategoria numa folha que já tem anúncios.</summary>
    public const string ParentHasAds = "Mova antes os anúncios desta categoria";

    public const string NotFound = "A categoria não existe mais";

    public const string Conflict = "As categorias foram alteradas por outra pessoa. Recarregue a página e tente de novo.";

    /// <summary>"Não é possível excluir: 3 anúncios usam esta categoria" (singular: "1 anúncio usa").</summary>
    public static string HasAds(int count) => count == 1
        ? "Não é possível excluir: 1 anúncio usa esta categoria"
        : $"Não é possível excluir: {count} anúncios usam esta categoria";
}

/// <summary>Para onde mover uma categoria entre as irmãs.</summary>
public enum MoveDirection
{
    Up = 1,
    Down = 2
}

/// <summary>
/// Resultado de uma operação de categoria. Em falha, <see cref="Field"/> diz a que campo a mensagem pertence (vazio = a página toda);
/// em sucesso, <see cref="Message"/> é o aviso para mostrar (nulo = nada a avisar) e <see cref="CategoryId"/> a categoria afetada.
/// </summary>
public sealed record CategoryResult(bool Succeeded, string Field, string Message, int? CategoryId)
{
    public static CategoryResult Ok(int categoryId, string message = null) => new(true, string.Empty, message, categoryId);

    public static CategoryResult Failure(string field, string message) => new(false, field, message, null);
}

/// <summary>
/// Criar, renomear, mover e excluir categorias (US-013). As regras (nome, três níveis, unicidade entre irmãs, exclusão) e a auditoria (RC-16)
/// ficam aqui; a tela só mostra o resultado. Cada operação lê o estado atual do banco, nunca o cache, e esvazia o cache da árvore ao terminar.
/// </summary>
public interface ICategoryManagement
{
    Task<CategoryResult> CreateAsync(int? parentId, string name, CancellationToken cancellationToken);

    Task<CategoryResult> RenameAsync(int id, string name, CancellationToken cancellationToken);

    Task<CategoryResult> MoveAsync(int id, MoveDirection direction, CancellationToken cancellationToken);

    Task<CategoryResult> DeleteAsync(int id, CancellationToken cancellationToken);
}
