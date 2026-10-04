using System;
using System.IO;
using System.Reflection;
using ImageMagick;
using ImageMagick.Configuration;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <summary>
/// Liga o ImageMagick uma única vez por processo, antes do primeiro uso: aplica a política embutida (<c>policy.xml</c>, só os cinco formatos) e os limites de
/// recurso (RC-2). O ImageMagick lê a política de uma pasta, então os arquivos de configuração são gravados numa subpasta <c>_magick</c> da pasta de fotos
/// (ou da pasta temporária, se a pasta de fotos não está configurada, como em desenvolvimento), numa subpasta com o hash da política.
/// </summary>
public static class MagickRuntime
{
    private static readonly object Gate = new();
    private static bool _initialized;

    /// <summary>Memória máxima do ImageMagick por processo: 512 MB (uma foto de 50 milhões de pixels em Q8 usa uns 200 MB).</summary>
    public const ulong MemoryLimitBytes = 512UL * 1024 * 1024;

    /// <summary>Tempo máximo de uma operação do ImageMagick, em segundos.</summary>
    public const ulong TimeLimitSeconds = 30;

    /// <summary>O texto da política embutida no assembly.</summary>
    public static string PolicyXml()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GazetaMarketplace.Infrastructure.Photos.policy.xml")
            ?? throw new InvalidOperationException("A política do ImageMagick não está embutida no assembly.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// A pasta dos arquivos de configuração para esta política. O ImageMagick não sobrescreve arquivos que já existem na pasta: ela leva o hash da política, então uma
    /// política nova (outra versão do site, outra lista de formatos) nunca é ignorada por sobrar uma pasta antiga.
    /// </summary>
    public static string ConfigFolder(string baseFolder, string policy)
    {
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(policy)))[..16].ToLowerInvariant();
        return Path.Combine(string.IsNullOrWhiteSpace(baseFolder) ? Path.GetTempPath() : baseFolder, "_magick", hash);
    }

    /// <param name="baseFolder">Pasta onde ficam os arquivos de configuração (<c>_magick</c>); vazia usa a pasta temporária.</param>
    /// <param name="extraCoders">Só para testes: formatos a mais na lista permitida (por exemplo <c>XC</c>, para criar imagens de cor sólida). Nunca é usado pelo site.</param>
    public static void EnsureInitialized(string baseFolder, string extraCoders = null)
    {
        if (_initialized)
        {
            return;
        }

        lock (Gate)
        {
            if (_initialized)
            {
                return;
            }

            string policy = PolicyXml();
            if (!string.IsNullOrEmpty(extraCoders))
            {
                policy = policy.Replace("HEIF}", "HEIF," + extraCoders + "}", StringComparison.Ordinal);
            }

            string folder = ConfigFolder(baseFolder, policy);
            Directory.CreateDirectory(folder);
            IConfigurationFiles files = ConfigurationFiles.Default;
            files.Policy.Data = policy;
            MagickNET.Initialize(files, folder);

            ResourceLimits.Memory = MemoryLimitBytes;
            ResourceLimits.Time = TimeLimitSeconds;
            ResourceLimits.Width = Core.Photos.PhotoLimits.MaxSide;
            ResourceLimits.Height = Core.Photos.PhotoLimits.MaxSide;
            _initialized = true;
        }
    }
}
