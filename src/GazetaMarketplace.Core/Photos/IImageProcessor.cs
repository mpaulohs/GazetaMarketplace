using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Photos;

/// <summary>As duas versões geradas de uma foto, já em WebP e sem nenhum metadado.</summary>
/// <param name="Large">A versão de até <see cref="PhotoLimits.LargeWidth"/> px de largura.</param>
/// <param name="Thumb">A miniatura de até <see cref="PhotoLimits.ThumbWidth"/> px de largura.</param>
/// <param name="Width">Largura da versão grande.</param>
/// <param name="Height">Altura da versão grande.</param>
public sealed record ProcessedImage(byte[] Large, byte[] Thumb, int Width, int Height);

/// <summary>
/// Decodifica a foto com o formato que a assinatura do arquivo já decidiu, corrige a orientação, converte para sRGB, tira todos os metadados (GPS incluído)
/// e gera as duas versões WebP. É trabalho de CPU: quem chama de uma requisição o roda fora do fluxo principal.
/// </summary>
public interface IImageProcessor
{
    /// <exception cref="ValidationException">Imagem ilegível, grande demais em pixels ou HEIC que a biblioteca não consegue ler; a mensagem é de <see cref="PhotoMessages"/>.</exception>
    ProcessedImage Process(byte[] content, PhotoFormat format);
}
