using System.Globalization;
using System.Text;

namespace GazetaMarketplace.Core.Search;

/// <summary>
/// Texto da busca (ADR-006): minúsculas, sem acento e com os espaços repetidos reduzidos a um. A mesma função grava <c>TitleSearch</c> e
/// <c>DescriptionSearch</c> e normaliza o termo digitado, então não existe uma segunda representação da regra em SQL.
/// </summary>
public static class Normalizer
{
    /// <summary>Normaliza o texto; nulo ou só espaços vira texto vazio.</summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder result = new(text.Length);
        bool pendingSpace = false;

        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }

            result.Append(char.ToLowerInvariant(c));
        }

        return result.ToString();
    }
}
