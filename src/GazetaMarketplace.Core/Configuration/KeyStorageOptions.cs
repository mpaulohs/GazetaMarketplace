using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>Pasta das chaves do Data Protection, fora da raiz do site (ADR-011).</summary>
public sealed class KeyStorageOptions
{
    public const string SectionName = "DataProtection";

    /// <summary>Chave de configuração que liga a cifra das chaves com o DPAPI do Windows (variável <c>DataProtection__ProtectWithDpapi</c>).</summary>
    public const string ProtectWithDpapiKey = "DataProtection:ProtectWithDpapi";

    [Required]
    public string KeysDirectory { get; set; }
}
