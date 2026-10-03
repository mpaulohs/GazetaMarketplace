using System;
using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields.Groups;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Todos os grupos de campos, em código. Uma categoria usa o grupo gravado nela ou o do ancestral mais próximo que tenha um (A7 a); se
/// nenhum da cadeia tem, usa <see cref="Default"/> (Produtos em geral). Categoria nova herda do pai sem nenhum trabalho.
/// </summary>
public static class FieldGroupRegistry
{
    private static readonly IReadOnlyDictionary<string, FieldGroup> Groups = new FieldGroup[]
    {
        ServicesGroup.Create(),
        JobsGroup.Create(),
        GeneralProductsGroup.Create(),
        RealEstateGroup.Create()
    }.ToDictionary(g => g.Key, StringComparer.Ordinal);

    public static IReadOnlyCollection<FieldGroup> All => Groups.Values.ToList();

    /// <summary>Produtos em geral: o grupo das categorias que não definem nem herdam outro.</summary>
    public static FieldGroup Default => Groups[FieldGroupKeys.GeneralProducts];

    /// <summary>O grupo pela chave; nulo se a chave não existe.</summary>
    public static FieldGroup Get(string key) => key is null ? null : Groups.GetValueOrDefault(key);

    /// <summary>
    /// O grupo que vale para a categoria, resolvendo a herança pela árvore. Nulo se a categoria não existe. Lança se a categoria (ou um
    /// ancestral) tiver gravada uma chave que não existe aqui: é erro de configuração que não deve passar despercebido.
    /// </summary>
    public static FieldGroup Resolve(CategoryTreeSnapshot tree, int categoryId)
    {
        ArgumentNullException.ThrowIfNull(tree);
        if (tree.Find(categoryId) is null)
        {
            return null;
        }

        string key = tree.ResolveFieldGroup(categoryId);
        if (key is null)
        {
            return Default;
        }

        return Get(key) ?? throw new InvalidOperationException($"A categoria {categoryId} usa o grupo de campos '{key}', que não existe no registro.");
    }
}
