using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>Administrador inicial; só existe na primeira publicação (ADR-011, ADR-003).</summary>
public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string AdminEmail { get; set; }

    public string AdminPassword { get; set; }
}
