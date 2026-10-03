using System;
using System.Collections.Generic;
using System.Linq;

namespace GazetaMarketplace.Core.Categories;

/// <summary>Uma categoria como a árvore a enxerga: dados da linha e a posição na árvore. Imutável.</summary>
public sealed record CategoryNode(
    int Id,
    int? ParentId,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsPostable,
    string FieldGroup,
    bool IsSystem,
    int Depth);

/// <summary>
/// A árvore de categorias inteira em memória, já com ancestrais e descendentes resolvidos: nenhuma consulta por nível.
/// Imutável e segura para várias requisições ao mesmo tempo. Quem edita categorias vê a mudança depois de
/// <see cref="ICategoryTree.Invalidate"/>.
/// </summary>
public sealed class CategoryTreeSnapshot
{
    private readonly Dictionary<int, CategoryNode> _byId;
    private readonly Dictionary<string, CategoryNode> _bySlug;
    private readonly Dictionary<int, List<CategoryNode>> _children;
    private readonly List<CategoryNode> _roots;
    private readonly Dictionary<int, IReadOnlyList<CategoryNode>> _descendants = [];
    private readonly Dictionary<int, IReadOnlyList<CategoryNode>> _ancestors = [];

    private CategoryTreeSnapshot(Dictionary<int, CategoryNode> byId, Dictionary<int, List<CategoryNode>> children, List<CategoryNode> roots)
    {
        _byId = byId;
        _children = children;
        _roots = roots;
        _bySlug = byId.Values.ToDictionary(n => n.Slug, StringComparer.Ordinal);
    }

    public static CategoryTreeSnapshot Empty { get; } = Build([]);

    /// <summary>Todas as categorias, em ordem de exibição dentro de cada pai (profundidade primeiro).</summary>
    public IReadOnlyList<CategoryNode> All => [.. Roots.SelectMany(r => new[] { r }.Concat(DescendantsOf(r.Id)))];

    public IReadOnlyList<CategoryNode> Roots => ChildrenOf(null);

    public int Count => _byId.Count;

    /// <summary>Monta a árvore a partir das linhas do banco. Linha com pai que não existe é ignorada com todo o seu ramo.</summary>
    public static CategoryTreeSnapshot Build(IEnumerable<CategoryRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        List<CategoryRow> list = [.. rows];
        Dictionary<int, CategoryRow> rowById = list.ToDictionary(r => r.Id);
        Dictionary<int, CategoryNode> byId = [];

        int DepthOf(CategoryRow row)
        {
            int depth = 1;
            for (CategoryRow current = row; current.ParentId is int parentId; depth++)
            {
                if (!rowById.TryGetValue(parentId, out current) || depth > list.Count)
                {
                    return -1; // pai inexistente ou ciclo: fora da árvore
                }
            }

            return depth;
        }

        foreach (CategoryRow row in list)
        {
            int depth = DepthOf(row);
            if (depth > 0)
            {
                byId[row.Id] = new CategoryNode(row.Id, row.ParentId, row.Name, row.Slug, row.DisplayOrder, row.IsPostable, row.FieldGroup, row.IsSystem, depth);
            }
        }

        // ATENÇÃO (mantenedores): Dictionary<int?, ...> NÃO aceita chave nula, e o primeiro nível tem ParentId nulo — um GroupBy(ParentId).ToDictionary
        // lança ArgumentNullException só quando há raízes (bug real da tarefa 2.1). Por isso as raízes ficam numa lista própria e o dicionário
        // guarda apenas pais não nulos.
        List<CategoryNode> roots = [.. byId.Values.Where(n => n.ParentId is null).OrderBy(n => n.DisplayOrder).ThenBy(n => n.Id)];
        Dictionary<int, List<CategoryNode>> children = byId.Values
            .Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.DisplayOrder).ThenBy(n => n.Id).ToList());

        CategoryTreeSnapshot snapshot = new(byId, children, roots);
        foreach (CategoryNode node in byId.Values)
        {
            snapshot._ancestors[node.Id] = snapshot.BuildAncestors(node);
        }

        foreach (CategoryNode root in snapshot.Roots)
        {
            snapshot.FillDescendants(root);
        }

        return snapshot;
    }

    public CategoryNode Find(int id) => _byId.GetValueOrDefault(id);

    public CategoryNode FindBySlug(string slug) => slug is null ? null : _bySlug.GetValueOrDefault(slug);

    /// <summary>Filhas diretas, na ordem de exibição; lista vazia se não houver (ou se o id não existir).</summary>
    public IReadOnlyList<CategoryNode> ChildrenOf(int? parentId) =>
        parentId is null ? _roots : _children.TryGetValue(parentId.Value, out List<CategoryNode> list) ? list : [];

    /// <summary>Todas as categorias abaixo desta, em todos os níveis (filhas, netas…), em ordem de exibição, sem a própria.</summary>
    public IReadOnlyList<CategoryNode> DescendantsOf(int id) => _descendants.GetValueOrDefault(id) ?? [];

    /// <summary>Do primeiro nível até o pai direto (sem a própria categoria); vazia na categoria de primeiro nível.</summary>
    public IReadOnlyList<CategoryNode> AncestorsOf(int id) => _ancestors.GetValueOrDefault(id) ?? [];

    /// <summary>O que o usuário vê como caminho: ancestrais e a própria categoria, do primeiro nível para baixo.</summary>
    public IReadOnlyList<CategoryNode> PathTo(int id) =>
        _byId.TryGetValue(id, out CategoryNode node) ? [.. AncestorsOf(id), node] : [];

    /// <summary>
    /// Grupo de campos que vale para a categoria: o próprio ou o do ancestral mais próximo que tenha um (A7 a).
    /// Nulo se nenhuma da cadeia define grupo.
    /// </summary>
    public string ResolveFieldGroup(int id)
    {
        foreach (CategoryNode node in PathTo(id).Reverse())
        {
            if (node.FieldGroup is not null)
            {
                return node.FieldGroup;
            }
        }

        return null;
    }

    private IReadOnlyList<CategoryNode> BuildAncestors(CategoryNode node)
    {
        List<CategoryNode> chain = [];
        for (int? parentId = node.ParentId; parentId is int id; parentId = _byId[id].ParentId)
        {
            chain.Add(_byId[id]);
        }

        chain.Reverse();
        return chain;
    }

    private List<CategoryNode> FillDescendants(CategoryNode node)
    {
        List<CategoryNode> result = [];
        foreach (CategoryNode child in ChildrenOf(node.Id))
        {
            result.Add(child);
            result.AddRange(FillDescendants(child));
        }

        _descendants[node.Id] = result;
        return result;
    }
}

/// <summary>Uma linha da tabela <c>Categories</c>, sem navegação, como o carregador entrega para montar a árvore.</summary>
public sealed record CategoryRow(
    int Id,
    int? ParentId,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsPostable,
    string FieldGroup,
    bool IsSystem);
