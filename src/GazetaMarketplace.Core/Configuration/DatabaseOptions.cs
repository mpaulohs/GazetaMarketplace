using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>Conexão com o banco (ConnectionStrings:DefaultConnection).</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string DefaultConnection { get; set; }
}
