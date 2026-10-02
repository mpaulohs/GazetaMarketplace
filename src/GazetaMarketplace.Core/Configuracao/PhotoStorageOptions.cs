using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuracao;

/// <summary>Pasta persistente de fotos, fora da raiz do site (ADR-005).</summary>
public sealed class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    [Required]
    public string BasePath { get; set; }
}
