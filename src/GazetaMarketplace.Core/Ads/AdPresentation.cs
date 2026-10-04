using System;
using System.Collections.Generic;
using System.Globalization;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Photos;

namespace GazetaMarketplace.Core.Ads;

/// <summary>As três formas do valor do anúncio (A6): Preço, Salário (Vagas de emprego) ou Tipo do serviço (Serviços).</summary>
public enum AdValueKind
{
    Price = 1,
    Salary = 2,
    ServiceType = 3
}

/// <summary>O valor pronto para mostrar: <see cref="Label"/> é "Preço", "Salário" ou "Tipo"; <see cref="Text"/> é "R$ 62.000" ou o tipo do serviço.</summary>
public sealed record AdValue(AdValueKind Kind, string Label, string Text);

/// <summary>
/// As regras de apresentação do anúncio, sem HTML (design-system §5.2 e §5.3): a forma do valor, o nome acessível do card, a localização e a miniatura.
/// Ficam no Core para o card do site, a pré-visualização do painel e os testes usarem a mesma regra.
/// </summary>
public static class AdPresentation
{
    public const string JobBlockTitle = "Vaga de emprego";

    public const string PhotoUnavailable = "Foto indisponível";

    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>"R$ 62.000" ou "R$ 2.499,90": os centavos só aparecem quando não são zero (S29).</summary>
    public static string FormatMoney(long cents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cents);
        return cents % 100 == 0
            ? "R$ " + (cents / 100).ToString("N0", PtBr)
            : "R$ " + (cents / 100m).ToString("N2", PtBr);
    }

    /// <summary>
    /// O valor que o anúncio mostra, decidido pelo grupo: Serviços mostra o Tipo e não tem preço; Vagas mostra "Salário"; os demais, o preço.
    /// Nulo quando não há o que mostrar (preço vazio, tipo em branco), para a tela omitir a linha em vez de mostrar "R$ 0".
    /// </summary>
    public static AdValue ValueOf(FieldGroup group, long? priceCents, string serviceType)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (!group.HasPrice)
        {
            return string.IsNullOrWhiteSpace(serviceType) ? null : new AdValue(AdValueKind.ServiceType, "Tipo", serviceType.Trim());
        }

        if (priceCents is not > 0)
        {
            return null;
        }

        return group.Key == FieldGroupKeys.Jobs
            ? new AdValue(AdValueKind.Salary, "Salário", FormatMoney(priceCents.Value))
            : new AdValue(AdValueKind.Price, "Preço", FormatMoney(priceCents.Value));
    }

    /// <summary>Verdadeiro quando o grupo tem foto; falso em Vagas de emprego, que mostra o bloco neutro "Vaga de emprego" no lugar da capa (A4).</summary>
    public static bool HasPhotos(FieldGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return group.MaxPhotos > 0;
    }

    /// <summary>"Campinas/SP"; só a cidade ou só a UF quando falta a outra; nulo quando faltam as duas.</summary>
    public static string Location(string city, string uf)
    {
        string c = city?.Trim();
        string u = uf?.Trim();
        if (string.IsNullOrEmpty(c))
        {
            return string.IsNullOrEmpty(u) ? null : u;
        }

        return string.IsNullOrEmpty(u) ? c : c + "/" + u;
    }

    /// <summary>
    /// O nome acessível do link do card: "título, valor, cidade/UF". O valor leva "Salário" nas vagas e "Tipo:" nos serviços ("Pizzaiolo…, Salário R$ 2.800, Campinas/SP");
    /// a parte que falta some, sem vírgula sobrando.
    /// </summary>
    public static string AccessibleName(string title, AdValue value, string city, string uf)
    {
        List<string> parts = [title?.Trim()];
        if (value is not null)
        {
            parts.Add(value.Kind switch
            {
                AdValueKind.Salary => value.Label + " " + value.Text,
                AdValueKind.ServiceType => value.Label + ": " + value.Text,
                _ => value.Text
            });
        }

        if (Location(city, uf) is { } location)
        {
            parts.Add(location);
        }

        return string.Join(", ", parts.FindAll(p => !string.IsNullOrEmpty(p)));
    }

    /// <summary>O endereço da miniatura de 480 px (<c>getPhotoFile</c>, ADR-005).</summary>
    public static string CoverUrl(int adId, int photoId) => $"/fotos/{adId}/{photoId}-{PhotoLimits.ThumbWidth}.webp";

    /// <summary>
    /// O tamanho da miniatura a partir do tamanho gravado da foto: nunca amplia, 480 px de largura no máximo e o lado maior limitado, como no processador.
    /// Serve só para reservar o espaço (<c>width</c> e <c>height</c> da imagem).
    /// </summary>
    public static (int Width, int Height) ThumbSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return (PhotoLimits.ThumbWidth, PhotoLimits.ThumbWidth * 3 / 4);
        }

        double scale = Math.Min(1d, Math.Min((double)PhotoLimits.ThumbWidth / width, (double)PhotoLimits.MaxLongSide / Math.Max(width, height)));
        return ((int)Math.Max(1, Math.Round(width * scale)), (int)Math.Max(1, Math.Round(height * scale)));
    }
}
