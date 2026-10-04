using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Photos;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>
/// O que o formulário do anúncio envia — só texto, como foi digitado. <b>Não existe aqui</b> situação, autor, data de publicação, motivo de
/// rejeição nem qualquer campo de decisão (RC-14): esses valores nunca vêm do navegador, e um campo a mais enviado à mão é simplesmente ignorado.
/// </summary>
public sealed class AdFormSubmission
{
    public string Title { get; set; }

    public string Description { get; set; }

    public int? CategoryId { get; set; }

    /// <summary>Preço em notação brasileira, como digitado.</summary>
    public string Price { get; set; }

    public string Cep { get; set; }

    public string City { get; set; }

    public string Uf { get; set; }

    /// <summary>O CEP para o qual <see cref="City"/> e <see cref="Uf"/> foram obtidos (veja <c>AdDraftInput.LocationCep</c>).</summary>
    public string LocationCep { get; set; }

    public bool LocationManual { get; set; }

    /// <summary>Versão da linha que a tela abriu, em Base64; serve só para detectar edição concorrente.</summary>
    public string RowVersion { get; set; }

    /// <summary>Campos do grupo pela chave do campo (<c>Fields[km]</c>); vários valores só em campos de várias opções.</summary>
    public Dictionary<string, string[]> Fields { get; set; } = [];
}

/// <summary>Uma opção de lista no formulário.</summary>
public sealed record AdOption(string Value, string Label, bool Selected);

/// <summary>Como o campo é desenhado.</summary>
public enum AdFieldKind
{
    Text = 1,
    Number = 2,
    Select = 3,
    MultiSelect = 4
}

/// <summary>Um campo específico do grupo, já pronto para a tela.</summary>
public sealed class AdFieldViewModel
{
    public string Key { get; init; }

    public string Label { get; init; }

    public AdFieldKind Kind { get; init; }

    /// <summary>Obrigatório para <i>enviar à revisão</i> (o rascunho aceita em branco).</summary>
    public bool Required { get; set; }

    public string HelpText { get; init; }

    public string Value { get; init; }

    public int? MaxLength { get; init; }

    /// <summary>"numeric" (inteiro), "decimal" (com vírgula) ou nulo.</summary>
    public string InputMode { get; init; }

    public IReadOnlyList<AdOption> Options { get; init; } = [];

    /// <summary>Texto de leitura (somente leitura e detalhe): o rótulo da opção escolhida ou o valor digitado.</summary>
    public string DisplayValue { get; init; }

    /// <summary>"car" ou "moto" nos campos da cadeia do catálogo; usado pelo JavaScript para consultar as listas.</summary>
    public string CatalogKind { get; init; }

    /// <summary>"brand", "model", "year" ou "version" nos campos da cadeia do catálogo.</summary>
    public string CatalogLevel { get; init; }

    /// <summary>Lista do catálogo sem itens (o campo anterior ainda não foi escolhido): fica desabilitada.</summary>
    public bool Disabled { get; init; }

    /// <summary>Nome do campo no formulário, que é também a chave do erro no <c>ModelState</c>.</summary>
    public string Name => $"Fields[{Key}]";

    public string Id => $"campo-{Key}";

    public string ErrorId => $"error-{Id}";

    public string HelpId => $"ajuda-{Id}";
}

public sealed record AdCategoryOption(int Id, string Label, bool Selected);

public sealed record AdCategoryGroup(string Label, IReadOnlyList<AdCategoryOption> Items);

/// <summary>O formulário do anúncio (novo, edição e somente leitura).</summary>
public sealed class AdFormViewModel
{
    public int? Id { get; init; }

    public bool IsNew => Id is null;

    public string RowVersion { get; init; }

    /// <summary>Situação em texto ("Rascunho", "Rejeitado"…); nulo no anúncio novo.</summary>
    public string StatusLabel { get; init; }

    public byte Status { get; init; }

    public string RejectionReason { get; init; }

    /// <summary>Em revisão, publicado (para o Redator) e arquivado: dados só para leitura.</summary>
    public bool ReadOnly { get; init; }

    public string ReadOnlyMessage { get; init; }

    public string Title { get; init; }

    public string Description { get; init; }

    public int? CategoryId { get; init; }

    public string CategoryName { get; init; }

    public string Price { get; init; }

    public string Cep { get; init; }

    public string City { get; init; }

    public string Uf { get; init; }

    public string LocationCep { get; init; }

    public bool LocationManual { get; init; }

    /// <summary>Cidades da UF escolhida, para a lista do modo manual. Vazia: a UF ainda não tem carga do IBGE (campo de texto).</summary>
    public IReadOnlyList<string> CityOptions { get; init; } = [];

    public IReadOnlyList<State> States => BrazilianStates.All;

    public FieldGroup Group { get; init; }

    public IReadOnlyList<AdFieldViewModel> Fields { get; init; } = [];

    public IReadOnlyList<AdCategoryGroup> Categories { get; init; } = [];

    /// <summary>As fotos gravadas do anúncio, na ordem da galeria (a primeira é a capa). Vazia no anúncio novo.</summary>
    public IReadOnlyList<AdPhotoItem> Photos { get; init; } = [];

    /// <summary>Quantas fotos a categoria <b>gravada</b> do anúncio aceita (20, 6 em Serviços, 0 em Vagas). É o limite que o servidor aplica ao enviar.</summary>
    public int PhotoLimit { get; init; } = FieldGroup.DefaultMaxPhotos;

    public int PhotoCount => Photos.Count;

    /// <summary>O que falta para enviar à revisão (preenchido quando a pessoa pediu o envio e o servidor recusou).</summary>
    public IReadOnlyList<AdPending> Pendings { get; init; } = [];

    /// <summary>Se a tela oferece "Enviar para revisão": só num anúncio já gravado, editável e de uma situação que o Apêndice A deixa passar a Em revisão (Rascunho ou Rejeitado). O Administrador edita um anúncio Em revisão, mas não o reenvia.</summary>
    public bool CanSubmit => !IsNew && !ReadOnly && AdStatusRules.Find(Status, Core.Ads.AdStatus.InReview) is not null;

    /// <summary>Se a tela oferece enviar, trocar a capa e remover: só num anúncio já gravado, editável e de categoria com fotos.</summary>
    public bool CanManagePhotos => !IsNew && !ReadOnly && PhotoLimit > 0;

    public string SubmitLabel => IsNew || Status == Core.Ads.AdStatus.Draft ? "Salvar rascunho" : "Salvar";

    /// <summary>Aviso de sucesso da última gravação (<c>role="status"</c>).</summary>
    public string Message { get; init; }

    /// <summary>Aviso que não impede nada (CEP não encontrado, serviço de CEP fora do ar).</summary>
    public string Warning { get; init; }

    public string Heading => IsNew ? "Novo anúncio" : ReadOnly ? "Anúncio" : "Editar anúncio";

    public bool HasPrice => Group?.HasPrice ?? true;

    public string PriceLabel => Group?.PriceLabel ?? "Preço";

    /// <summary>Se a categoria <b>escolhida no formulário</b> (ainda que não salva) aceita fotos; muda na hora com a troca de categoria.</summary>
    public bool HasPhotos => Group is null || Group.MaxPhotos > 0;
}

/// <summary>A página de confirmação do envio à revisão.</summary>
public sealed record SubmitConfirmationViewModel(int Id, string Title);
