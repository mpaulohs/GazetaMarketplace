using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Core.Photos;

/// <summary>O que é um arquivo guardado, para a limpeza decidir sem conhecer a estrutura de pastas.</summary>
public enum StoredFileKind
{
    /// <summary>Original enviado (<c>_originals/&lt;yyyy-MM&gt;/&lt;guid&gt;.&lt;ext&gt;</c>); a chave é a mesma de <c>AdPhotos.OriginalKey</c>.</summary>
    Original = 1,

    /// <summary>Versão WebP de uma foto (<c>&lt;adId&gt;/&lt;guid&gt;_1600.webp</c> ou <c>_480.webp</c>).</summary>
    Version = 2,

    /// <summary>Arquivo temporário de uma gravação que não terminou (<c>*.tmp</c>).</summary>
    Temporary = 3
}

/// <summary>Um arquivo guardado.</summary>
/// <param name="Path">Caminho relativo à pasta de fotos, com <c>/</c>; é o que <see cref="IPhotoStorageMaintenance.Delete"/> recebe.</param>
/// <param name="Kind">Original, versão ou temporário.</param>
/// <param name="StorageKey">Só para <see cref="StoredFileKind.Version"/>: a base <c>&lt;adId&gt;/&lt;guid&gt;</c> de <c>AdPhotos.StorageKey</c>.</param>
/// <param name="LastWriteUtc">Quando o arquivo foi gravado (data de gravação do próprio arquivo).</param>
public sealed record StoredFile(string Path, StoredFileKind Kind, string StorageKey, DateTime LastWriteUtc);

/// <summary>
/// Manutenção do armazenamento de fotos (limpeza diária, ADR-005). Separada de <see cref="IPhotoStorage"/> para o uso comum continuar enxuto; trocar o disco por
/// armazenamento em nuvem muda só a implementação. Só enxerga arquivos com o formato de nome que o próprio site gera: o resto (a pasta <c>_magick</c>, arquivos
/// alheios) nunca é listado, então nunca é apagado.
/// </summary>
public interface IPhotoStorageMaintenance
{
    /// <summary>Lista, devagar, os arquivos de foto: originais, versões e temporários.</summary>
    IEnumerable<StoredFile> List();

    /// <summary>Apaga um arquivo listado. Arquivo que já não existe não é erro; caminho fora do formato gerado pelo site é recusado.</summary>
    /// <exception cref="System.IO.IOException">O arquivo está preso ou sem permissão; quem chama registra e segue.</exception>
    void Delete(string path);

    /// <summary>Remove as pastas vazias (mês de originais, anúncio) cuja última alteração é anterior a <paramref name="olderThanUtc"/>; devolve quantas.</summary>
    int RemoveEmptyFolders(DateTime olderThanUtc);
}
