using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Location;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// As pendências para enviar à revisão (SPEC, US-009): título, categoria, descrição, preço (menos Serviços), CEP com cidade, ao menos 1 foto (menos Vagas) e os campos
/// obrigatórios do grupo. Regra pura sobre o anúncio gravado: o serviço só traz o que vem do banco. A ordem é a da tela.
/// </summary>
public static class AdSubmissionRules
{
    /// <summary>Os ids dos campos comuns da tela, para os links das pendências.</summary>
    public static class Targets
    {
        public const string Title = "titulo";
        public const string Category = "categoria";
        public const string Description = "descricao";
        public const string Price = "preco";
        public const string Cep = "cep";
        public const string Photos = "fotos";

        /// <summary>O id do campo de um grupo (<c>campo-km</c>), o mesmo que a tela gera.</summary>
        public static string Field(string key) => $"campo-{key}";
    }

    /// <param name="ad">O anúncio gravado.</param>
    /// <param name="group">O grupo da categoria gravada (o padrão quando o anúncio ainda não tem categoria).</param>
    /// <param name="photoCount">Quantas fotos o anúncio tem gravadas.</param>
    public static IReadOnlyList<AdPending> Pending(Ad ad, FieldGroup group, int photoCount)
    {
        ArgumentNullException.ThrowIfNull(ad);
        ArgumentNullException.ThrowIfNull(group);
        List<AdPending> pending = [];

        if (string.IsNullOrWhiteSpace(ad.Title))
        {
            pending.Add(new AdPending(AdMessages.TitleRequired, Targets.Title));
        }

        if (ad.CategoryId is null)
        {
            pending.Add(new AdPending(AdMessages.CategoryRequired, Targets.Category));
        }

        if (string.IsNullOrWhiteSpace(ad.Description))
        {
            pending.Add(new AdPending(AdMessages.DescriptionRequired(group.DescriptionLabel), Targets.Description));
        }

        if (group.HasPrice && ad.PriceCents is not > 0)
        {
            pending.Add(new AdPending(AdMessages.PriceRequired(group.PriceLabel), Targets.Price));
        }

        if (!CepRules.IsValid(ad.Cep))
        {
            pending.Add(new AdPending(AdMessages.CepRequired, Targets.Cep));
        }
        else if (string.IsNullOrWhiteSpace(ad.City) || string.IsNullOrWhiteSpace(ad.Uf))
        {
            pending.Add(new AdPending(AdMessages.CityRequiredWithUf, Targets.Cep));
        }

        if (photoCount < group.MinPhotosToSubmit)
        {
            pending.Add(new AdPending(AdMessages.PhotosRequired(group.MinPhotosToSubmit), Targets.Photos));
        }

        if (ad.CategoryId is { } categoryId)
        {
            AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes);
            foreach (FieldDefinition field in group.RequiredFieldsFor(categoryId))
            {
                if (!IsFilled(attributes, field))
                {
                    pending.Add(new AdPending(field.RequiredMessage ?? $"Informe: {field.Label}", Targets.Field(field.Key)));
                }
            }
        }

        return pending;
    }

    /// <summary>Se o campo tem valor guardado. Zero conta (Quartos 0 é kitnet); texto só com espaços e lista vazia não.</summary>
    public static bool IsFilled(AdAttributes attributes, FieldDefinition field)
    {
        if (attributes is null)
        {
            return false;
        }

        return field.Type switch
        {
            FieldType.Text => !string.IsNullOrWhiteSpace(attributes.GetString(field.Key)),
            FieldType.Decimal => attributes.TryGetDecimal(field.Key, out _),
            FieldType.Money => attributes.TryGetLong(field.Key, out _),
            FieldType.MultiSelect => attributes.GetInts(field.Key).Count > 0,
            _ => attributes.TryGetInt(field.Key, out _)
        };
    }
}
