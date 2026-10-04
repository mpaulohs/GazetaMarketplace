using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Photos;

/// <summary>O que ficou gravado de uma foto; a tarefa 3.5 guarda isto em <c>AdPhotos</c>.</summary>
/// <param name="StorageKey">Base dos nomes das versões: <c>&lt;adId&gt;/&lt;guid&gt;</c> (as versões são <c>&lt;chave&gt;_1600.webp</c> e <c>&lt;chave&gt;_480.webp</c>).</param>
/// <param name="OriginalKey">Caminho do original em <c>_originals/</c>.</param>
/// <param name="Width">Largura da versão grande.</param>
/// <param name="Height">Altura da versão grande.</param>
/// <param name="SizeBytes">Tamanho da versão grande (a que a galeria baixa).</param>
public sealed record StoredPhoto(string StorageKey, string OriginalKey, int Width, int Height, int SizeBytes);

/// <summary>Recebe uma foto enviada e a deixa pronta no armazenamento: confere tamanho e formato, processa e grava. Se algo falhar no meio, nada fica no disco.</summary>
public interface IPhotoIngestion
{
    /// <exception cref="ValidationException">Vazia, maior que 10 MB, formato não aceito, ilegível ou grande demais em pixels (campo <c>file</c>).</exception>
    Task<StoredPhoto> IngestAsync(int adId, Stream content, CancellationToken cancellationToken);
}
