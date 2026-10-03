using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GazetaMarketplace.Core.Categories;

/// <summary>Uma categoria que precisa de slug na carga inicial. <c>ExplicitSlug</c> nulo = gerar do nome.</summary>
public sealed record SlugCandidate(int Id, string Name, bool IsPostable, string ExplicitSlug);

/// <summary>
/// Slugs das categorias (endereços como <c>/categoria/servicos</c>). Sem acento, em minúsculas, hífen no lugar de espaço,
/// sem caracteres especiais e únicos no site inteiro.
/// <list type="number">
/// <item>Slug explícito do arquivo é usado como está.</item>
/// <item>Colisão entre nomes iguais: a categoria <b>postável</b> fica com o slug limpo (a URL limpa leva à página que tem anúncios);
/// a não postável ganha <c>-grupo</c> (e <c>-grupo-2</c>… se esse também existir). Duas postáveis: a de menor id fica com o limpo e a outra
/// ganha <c>-2</c>, <c>-3</c>… Duas não postáveis: a de menor id fica com o limpo.</item>
/// </list>
/// </summary>
public static class SlugGenerator
{
    public const int MaxLength = 120;

    public const string FallbackSlug = "categoria";

    public const string GroupSuffix = "-grupo";

    /// <summary>O slug "limpo" do nome, sem pensar em colisão.</summary>
    public static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FallbackSlug;
        }

        StringBuilder result = new(name.Length);
        bool pendingSeparator = false;
        foreach (char c in name.Normalize(NormalizationForm.FormD))
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue; // o acento solto depois de decompor a letra
            }

            char lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingSeparator && result.Length > 0)
                {
                    result.Append('-');
                }

                pendingSeparator = false;
                result.Append(lower);
            }
            else if (char.IsWhiteSpace(c) || c is '-' or '_')
            {
                pendingSeparator = true; // espaço, hífen e sublinhado separam palavras
            }

            // qualquer outro caractere (vírgula, &, aspas, símbolo, letra sem equivalente ASCII) é descartado
        }

        string slug = Truncate(result.ToString(), MaxLength);
        return slug.Length == 0 ? FallbackSlug : slug;
    }

    /// <summary>
    /// Slug livre para <i>uma</i> categoria nova ou renomeada, dado o conjunto de slugs já ocupados. A categoria que já existe
    /// nunca perde o seu: quem chega depois é que recebe o sufixo.
    /// </summary>
    public static string Unique(string name, bool isPostable, ISet<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);
        string baseSlug = Slugify(name);
        if (!taken.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (int attempt = 1; ; attempt++)
        {
            string suffix = isPostable
                ? "-" + (attempt + 1).ToString(CultureInfo.InvariantCulture)
                : GroupSuffix + (attempt == 1 ? string.Empty : "-" + attempt.ToString(CultureInfo.InvariantCulture));
            string candidate = Truncate(baseSlug, MaxLength - suffix.Length) + suffix;
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Slugs de uma carga inteira. Os explícitos valem como estão (e não podem se repetir); os demais são gerados, postáveis antes de não
    /// postáveis e, dentro de cada grupo, por id crescente, de modo que o resultado não depende da ordem em que as linhas chegam.
    /// </summary>
    public static IReadOnlyDictionary<int, string> AssignInitial(IEnumerable<SlugCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        List<SlugCandidate> all = [.. candidates];
        Dictionary<int, string> result = [];
        HashSet<string> taken = new(StringComparer.Ordinal);

        foreach (SlugCandidate explicitOne in all.Where(c => c.ExplicitSlug is not null))
        {
            if (!taken.Add(explicitOne.ExplicitSlug))
            {
                throw new ArgumentException($"Slug explícito repetido: '{explicitOne.ExplicitSlug}'.", nameof(candidates));
            }

            result[explicitOne.Id] = explicitOne.ExplicitSlug;
        }

        foreach (SlugCandidate generated in all.Where(c => c.ExplicitSlug is null).OrderByDescending(c => c.IsPostable).ThenBy(c => c.Id))
        {
            string slug = Unique(generated.Name, generated.IsPostable, taken);
            taken.Add(slug);
            result[generated.Id] = slug;
        }

        return result;
    }

    private static string Truncate(string slug, int maxLength) =>
        slug.Length <= maxLength ? slug : slug[..maxLength].TrimEnd('-');
}
