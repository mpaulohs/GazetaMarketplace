using System;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// Foto do anúncio (só o modelo; o processamento é da 3.4 e o envio, da 3.5). Não tem auditoria completa nem <c>RowVersion</c>: o §6.2 lista só
/// <see cref="CreatedAt"/>, que o <c>AppDbContext</c> carimba ao gravar.
/// </summary>
public class AdPhoto
{
    public int Id { get; set; }

    public int AdId { get; set; }

    /// <summary>Posição na galeria; 0 é a capa.</summary>
    public int SortOrder { get; set; }

    /// <summary>Nome gerado do arquivo (nunca o nome enviado), sem caminho.</summary>
    public string StorageKey { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Tamanho em bytes da versão de 1600 px (a que a 3.4 grava).</summary>
    public int SizeBytes { get; set; }

    /// <summary>Caminho do original em <c>_originals/</c>; <b>nulo depois da limpeza de 30 dias</b> (3.6).</summary>
    public string OriginalKey { get; set; }

    public DateTime CreatedAt { get; set; }
}
