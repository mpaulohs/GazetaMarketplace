using System;
using Microsoft.AspNetCore.WebUtilities;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>Como o token vai e volta no endereço do link: texto seguro para URL, sem <c>+</c>, <c>/</c> nem <c>=</c>.</summary>
internal static class RecoveryCode
{
    public const string LinkPath = "/painel/redefinir-senha";

    /// <summary>O token do provedor já é Base64; aqui só vira Base64 de URL (mais curto que codificar o texto de novo).</summary>
    public static string Encode(string token) => WebEncoders.Base64UrlEncode(Convert.FromBase64String(token));

    /// <summary>O token original, ou <c>null</c> se o texto não for um código nosso.</summary>
    public static string Decode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        try
        {
            return Convert.ToBase64String(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
