using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Photos;

/// <summary>
/// Onde as fotos moram (ADR-005): uma pasta persistente fora da raiz do site. Quem usa só conhece <i>chaves</i> geradas pelo site, nunca nomes enviados
/// pelo usuário. A troca por um armazenamento de nuvem fica só na implementação.
/// </summary>
public interface IPhotoStorage
{
    /// <summary>Grava as duas versões de <paramref name="storageKey"/> (<c>&lt;adId&gt;/&lt;guid&gt;</c>), ou nenhuma delas.</summary>
    Task SaveVersionsAsync(string storageKey, byte[] large, byte[] thumb, CancellationToken cancellationToken);

    /// <summary>Guarda o arquivo enviado em <c>_originals/&lt;yyyy-MM&gt;/&lt;guid&gt;.&lt;ext&gt;</c> e devolve essa chave. Nenhuma rota serve essa pasta.</summary>
    Task<string> SaveOriginalAsync(byte[] content, PhotoFormat format, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Abre a versão para leitura, ou devolve <c>null</c> se não existe.</summary>
    Task<Stream> OpenAsync(string storageKey, PhotoSize size, CancellationToken cancellationToken);

    /// <summary>Apaga as versões e o original (chaves ausentes ou arquivos que já não existem não são erro).</summary>
    Task DeleteAsync(string storageKey, string originalKey, CancellationToken cancellationToken);
}
