using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GazetaMarketplace.Core.Location;

/// <summary>
/// Padronização do nome da cidade (SPEC, US-008): sem espaços extras e com iniciais maiúsculas, <b>mantendo os acentos</b>
/// ("sao jose" → "Sao Jose"; "são josé" → "São José"). Os artigos "de, da, do, das, dos, e" ficam minúsculos fora da primeira palavra
/// ("São José do Rio Preto"). Só vale quando o município não está na lista oficial do IBGE; havendo lista, o nome oficial prevalece.
/// </summary>
public static class CityNames
{
    private static readonly HashSet<string> Particles = new(StringComparer.OrdinalIgnoreCase) { "de", "da", "do", "das", "dos", "e" };

    public static string Standardize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        string[] words = name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder result = new(name.Length);
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                result.Append(' ');
            }

            result.Append(Word(words[i], first: i == 0));
        }

        return result.ToString();
    }

    private static string Word(string word, bool first)
    {
        if (first)
        {
            return Capitalize(word);
        }

        if (Particles.Contains(word))
        {
            return word.ToLower(CultureInfo.GetCultureInfo("pt-BR"));
        }

        // "d'Oeste": o "d'" é um artigo e fica minúsculo
        if (word.Length > 2 && char.ToLowerInvariant(word[0]) == 'd' && word[1] is '\'' or '’')
        {
            return char.ToLowerInvariant(word[0]) + word[1].ToString() + Capitalize(word[2..]);
        }

        return Capitalize(word);
    }

    // Maiúscula na inicial de cada trecho separado por hífen ou apóstrofo ("d'Oeste", "Mogi-Mirim"), minúscula no resto
    private static string Capitalize(string word)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");
        StringBuilder result = new(word.Length);
        bool upperNext = true;
        foreach (char c in word)
        {
            result.Append(upperNext ? char.ToUpper(c, culture) : char.ToLower(c, culture));
            upperNext = c is '-' or '\'' or '’';
        }

        return result.ToString();
    }
}
