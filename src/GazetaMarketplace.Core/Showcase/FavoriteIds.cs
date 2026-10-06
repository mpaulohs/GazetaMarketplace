using System;
using System.Collections.Generic;
using System.Globalization;

namespace GazetaMarketplace.Core.Showcase;

/// <summary>
/// Os ids que a página "Meus favoritos" e a API <c>GET /api/v1/ads?ids=</c> recebem (US-005): de 1 a 100 números inteiros de 1 em diante, separados por vírgula, sem espaço, sinal nem zero à esquerda (o padrão do contrato é
/// <c>^[0-9]+(,[0-9]+){0,99}$</c>). Um id repetido conta uma vez; a ordem pedida é mantida. Nunca lança.
/// </summary>
public static class FavoriteIds
{
    /// <summary>Quantos ids cada chamada aceita; a página busca em lotes desse tamanho e não limita o total de favoritos (decisão do Product Owner, 2026-10-05).</summary>
    public const int MaxPerRequest = 100;

    public const string InvalidMessage = "Informe de 1 a 100 ids numéricos separados por vírgula, como 12,57,104.";

    public static bool TryParse(string text, out int[] ids)
    {
        ids = [];
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string[] parts = text.Split(',');
        if (parts.Length > MaxPerRequest)
        {
            return false;
        }

        List<int> result = [];
        HashSet<int> seen = [];
        foreach (string part in parts)
        {
            // Id de anúncio é um inteiro de 1 em diante, escrito sem zero à esquerda: a mesma regra do favorites.js (limpar), provada na tabela de paridade
            if (!IsDigits(part) || part[0] == '0' || !int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int id))
            {
                return false;
            }

            if (seen.Add(id))
            {
                result.Add(id);
            }
        }

        ids = [.. result];
        return true;
    }

    // Só 0-9: o NumberStyles.None já recusa sinal e espaço, mas dígitos de outros alfabetos precisam ser barrados aqui
    private static bool IsDigits(string part)
    {
        if (part.Length == 0)
        {
            return false;
        }

        foreach (char c in part)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
