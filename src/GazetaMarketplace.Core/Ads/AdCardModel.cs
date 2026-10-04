namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// A capa do card: o id da foto e o tamanho da <b>miniatura de 480 px</b> (as medidas reservam o espaço antes da imagem chegar, NFR-03).
/// <paramref name="Url"/> existe só para os dados de exemplo da página de componentes do painel; o site sempre deixa em branco e a miniatura vem de <c>/fotos/…</c>.
/// </summary>
public sealed record AdCardCover(int PhotoId, int Width, int Height, string Url = null)
{
    /// <summary>A capa a partir da foto como está gravada em <c>AdPhotos</c> (medidas da versão grande): calcula o tamanho da miniatura.</summary>
    public static AdCardCover FromStored(int photoId, int storedWidth, int storedHeight)
    {
        (int width, int height) = AdPresentation.ThumbSize(storedWidth, storedHeight);
        return new AdCardCover(photoId, width, height);
    }
}

/// <summary>
/// Tudo o que o card do anúncio precisa, já lido do banco (modelo de leitura): quem monta a lista (home, busca, categoria, pré-visualização) preenche e o componente só
/// apresenta. <see cref="GroupKey"/> decide a variante (padrão, Serviços ou Vagas de emprego); <see cref="ServiceType"/> e <see cref="JobArea"/> vêm já como texto.
/// </summary>
/// <param name="Id">Id do anúncio.</param>
/// <param name="Title">Título.</param>
/// <param name="GroupKey">Chave do grupo de campos da categoria (<see cref="Fields.FieldGroupKeys"/>).</param>
/// <param name="PriceCents">Preço em centavos; nulo quando não há (Serviços).</param>
/// <param name="ServiceType">Tipo do serviço, por exemplo "Serviços domésticos"; só em Serviços.</param>
/// <param name="JobArea">A primeira área marcada da vaga; só em Vagas de emprego.</param>
/// <param name="Cover">A capa; nula em Vagas e quando o anúncio não tem foto.</param>
/// <param name="City">Cidade.</param>
/// <param name="Uf">Sigla do estado.</param>
/// <param name="Href">Para onde o card leva (a página de detalhe).</param>
public sealed record AdCardModel(
    int Id,
    string Title,
    string GroupKey,
    long? PriceCents,
    string ServiceType,
    string JobArea,
    AdCardCover Cover,
    string City,
    string Uf,
    string Href);
