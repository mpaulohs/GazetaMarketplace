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
}
