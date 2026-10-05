using System;

namespace GazetaMarketplace.Core.Contact;

/// <summary>O link "Ligar" (US-004): <c>tel:+55{número}</c>, a partir dos dígitos guardados (sem o código do país).</summary>
public static class PhoneLink
{
    public static string Tel(string phoneDigits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneDigits);
        return "tel:+55" + phoneDigits;
    }
}
