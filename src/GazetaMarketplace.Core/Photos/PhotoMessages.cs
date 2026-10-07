namespace GazetaMarketplace.Core.Photos;

/// <summary>Os textos de recusa de foto, como estão na SPEC (US-008-S05) ou, onde ela não diz, os mais simples. Todos saem como erro de validação do campo <c>file</c>.</summary>
public static class PhotoMessages
{
    public const string Field = "file";

    public const string UnsupportedFormat = "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC";

    public const string TooLarge = "A foto excede o limite de 10 MB";

    public const string Empty = "O arquivo está vazio";

    public const string Unreadable = "Não foi possível ler a foto. Tente outro arquivo";

    public const string HeicUnreadable = "Não foi possível converter esta foto HEIC. Envie-a em JPG ou PNG";

    public const string TooManyPixels = "A foto tem dimensões grandes demais";

    /// <summary>US-008-S04: o texto é o da SPEC, com o limite do grupo da categoria (20, 6 em Serviços).</summary>
    public static string TooManyPhotos(int max) => $"Cada anúncio pode ter no máximo {max} fotos";

    /// <summary>Vagas de emprego (limite 0): a frase "no máximo 0 fotos" confundiria.</summary>
    public const string CategoryHasNoPhotos = "Este tipo de anúncio não tem fotos";

    /// <summary>US-008-S16: um anúncio no ar não pode ficar sem a foto que o envio à revisão exigiu.</summary>
    public const string LastPhotoOfPublished = "Não é possível remover a última foto de um anúncio publicado. Despublique antes.";

    public const string NotFound = "A foto não foi encontrada neste anúncio";

    /// <summary>Reprocessar sem original (já apagado pela limpeza de 30 dias, ou arquivo ausente).</summary>
    public const string OriginalUnavailable = "Original indisponível: o arquivo foi apagado depois de 30 dias e a foto não pode ser reprocessada";
}

