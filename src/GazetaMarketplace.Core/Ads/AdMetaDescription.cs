using System.Text;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// O texto de <c>&lt;meta name="description"&gt;</c> da página do anúncio (NFR-21): os primeiros 160 caracteres da descrição, em texto puro (quebras de linha e espaços repetidos
/// viram um espaço só). Sem descrição, vale "título — cidade/UF". Passou de 160, termina em reticências sem partir uma palavra nem um par de substituição Unicode.
/// </summary>
public static class AdMetaDescription
{
    public const int MaxLength = 160;

    public static string For(string title, string description, string location)
    {
        string text = Collapse(description);
        if (text.Length == 0)
        {
            text = Collapse(string.IsNullOrWhiteSpace(location) ? title : $"{title} — {location}");
        }

        return Truncate(text);
    }

    /// <summary>Corta o texto (já em uma linha só) em <see cref="MaxLength"/> caracteres: passou disso, termina em reticências sem partir uma palavra nem um par de substituição Unicode.</summary>
    public static string Truncate(string text)
    {
        if (text.Length <= MaxLength)
        {
            return text;
        }

        // 159 caracteres e a reticência: o total fica em 160
        int end = MaxLength - 1;
        if (char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }

        string cut = text[..end];
        int space = cut.LastIndexOf(' ');
        return (space > MaxLength / 2 ? cut[..space] : cut).TrimEnd(' ', ',', ';', ':', '.', '-') + "…";
    }

    private static string Collapse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder result = new(value.Length);
        bool space = false;
        foreach (char c in value.Trim())
        {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
            {
                space = true;
                continue;
            }

            if (space)
            {
                result.Append(' ');
                space = false;
            }

            result.Append(c);
        }

        return result.ToString();
    }
}
