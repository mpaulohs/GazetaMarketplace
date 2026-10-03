using GazetaMarketplace.Core.Entities;

namespace GazetaMarketplace.Core.Categories;

/// <summary>
/// Categoria do site, em até 3 níveis (US-013-S11). Só categoria folha aceita anúncio (<see cref="IsPostable"/>).
/// A carga inicial vem de <c>specs/categories.md</c> com os ids reais; categorias novas continuam a sequência.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>Id da categoria-pai; nulo nas categorias de primeiro nível.</summary>
    public int? ParentId { get; set; }

    public string Name { get; set; }

    /// <summary>Endereço legível da categoria; único no site inteiro.</summary>
    public string Slug { get; set; }

    /// <summary>Ordem entre irmãs (mesmo pai).</summary>
    public int DisplayOrder { get; set; }

    public bool IsPostable { get; set; }

    /// <summary>Grupo de campos da própria categoria (ADR-002); nulo = herda do ancestral mais próximo.</summary>
    public string FieldGroup { get; set; }

    /// <summary>Veio da carga inicial. Com <see cref="FieldGroup"/> próprio, a categoria não pode ser excluída (A7 b).</summary>
    public bool IsSystem { get; set; }
}
