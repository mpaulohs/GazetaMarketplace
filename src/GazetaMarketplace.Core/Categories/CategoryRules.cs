using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GazetaMarketplace.Core.Categories;

/// <summary>Resultado da checagem das regras de uma categoria nova ou renomeada.</summary>
public enum CategoryViolation
{
    None = 0,
    EmptyName = 1,
    NameTooLong = 2,
    ParentNotFound = 3,

    /// <summary>A categoria ficaria no 4º nível (US-013-S11).</summary>
    TooDeep = 4,

    /// <summary>Já existe uma irmã com o mesmo nome (US-013-S06).</summary>
    DuplicateName = 5
}

/// <summary>Regras puras das categorias, sem banco: quem grava (tela de categorias, tarefa 2.6) chama antes de salvar.</summary>
public static class CategoryRules
{
    public const int MaxDepth = 3;

    public const int MaxNameLength = 100;

    /// <summary>
    /// Confere nome, profundidade e unicidade entre irmãs para uma categoria sob <paramref name="parentId"/> (nulo = primeiro nível).
    /// <paramref name="ignoreId"/> é a própria categoria ao renomear, para ela não colidir consigo mesma.
    /// </summary>
    public static CategoryViolation Check(CategoryTreeSnapshot tree, int? parentId, string name, int? ignoreId = null)
    {
        ArgumentNullException.ThrowIfNull(tree);

        string trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return CategoryViolation.EmptyName;
        }

        if (trimmed.Length > MaxNameLength)
        {
            return CategoryViolation.NameTooLong;
        }

        int depth = 1;
        if (parentId is int parent)
        {
            CategoryNode parentNode = tree.Find(parent);
            if (parentNode is null)
            {
                return CategoryViolation.ParentNotFound;
            }

            depth = parentNode.Depth + 1;
        }

        if (depth > MaxDepth)
        {
            return CategoryViolation.TooDeep;
        }

        string key = ComparisonKey(trimmed);
        bool duplicate = tree.ChildrenOf(parentId).Any(sibling => sibling.Id != ignoreId && ComparisonKey(sibling.Name) == key);
        return duplicate ? CategoryViolation.DuplicateName : CategoryViolation.None;
    }

    /// <summary>
    /// A categoria nunca pode ser excluída (A7 b): veio da carga inicial <b>e</b> define o próprio grupo de campos. Categorias que só herdam o grupo
    /// (por exemplo as peças sob Autopeças) e as criadas pelo Administrador seguem as regras comuns.
    /// </summary>
    public static bool IsProtectedFromDeletion(CategoryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.IsSystem && node.FieldGroup is not null;
    }

    /// <summary>Nome comparável: sem acento, sem diferença de maiúscula e com espaços colapsados ("Acessórios" e " acessorios " são o mesmo nome).</summary>
    public static string ComparisonKey(string name)
    {
        StringBuilder builder = new();
        foreach (char c in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return string.Join(' ', builder.ToString().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
    }
}
