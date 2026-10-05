using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Search;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// O anúncio (ARCHITECTURE §6.2). Guarda só o que o site publica sobre o bem: <b>não existe campo de nome, telefone ou e-mail de vendedor ou
/// comprador</b> (NFR-19); o contato é o telefone do próprio site. Os campos do grupo da categoria ficam em <see cref="Attributes"/> (JSON, ADR-002).
/// </summary>
/// <remarks>
/// Quem altera o anúncio chama os métodos daqui, que protegem as invariantes (preço nunca zero, colunas de busca só pelo C#, trilha de decisão).
/// As regras de quem pode fazer o quê ficam em <see cref="AdAccess"/> e no <c>IAdService</c>.
/// </remarks>
public class Ad : BaseEntity
{
    public const int TitleMaxLength = 120;

    /// <summary>O maior limite de descrição entre os grupos (Serviços e Vagas); o limite de cada grupo é conferido na tarefa 3.3.</summary>
    public const int DescriptionMaxLength = 6000;

    public const int RejectionReasonMaxLength = 500;

    public const int CityMaxLength = 80;

    /// <summary>Nulo enquanto o rascunho não tem categoria (US-008-S07: rascunho só com o título); o envio para revisão exige uma categoria postável.</summary>
    public int? CategoryId { get; private set; }

    /// <summary>Uma das constantes de <see cref="AdStatus"/>.</summary>
    public byte Status { get; private set; } = AdStatus.Draft;

    public string Title { get; private set; }

    public string Description { get; private set; }

    /// <summary>Preço em centavos; <b>nulo = sem preço</b> (Serviços), nunca zero (S28: teto de R$ 99.999.999,99).</summary>
    public long? PriceCents { get; private set; }

    /// <summary>Só os 8 dígitos.</summary>
    public string Cep { get; private set; }

    public string City { get; private set; }

    public string Uf { get; private set; }

    /// <summary>Cidade e UF escolhidas à mão porque o serviço de CEP falhou (selo de conferência).</summary>
    public bool LocationManual { get; private set; }

    /// <summary>Campos do grupo da categoria, em JSON de objeto (ADR-002). Use <see cref="AdAttributes"/> para ler e escrever.</summary>
    public string Attributes { get; private set; } = AdAttributes.EmptyJson;

    // Colunas calculadas persistidas pelo banco a partir de Attributes (ADR-002); só leitura aqui, o EF nunca as grava
    public int? VehicleBrandId { get; private set; }

    public int? VehicleModelId { get; private set; }

    public int? ModelYear { get; private set; }

    public int? Km { get; private set; }

    public decimal? AreaM2 { get; private set; }

    /// <summary>Título normalizado (<see cref="Normalizer"/>); preenchido só pelo C#.</summary>
    public string TitleSearch { get; private set; }

    /// <summary>Descrição normalizada (<see cref="Normalizer"/>); preenchida só pelo C#.</summary>
    public string DescriptionSearch { get; private set; }

    public int AuthorId { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public int? PublishedById { get; private set; }

    public DateTime? RejectedAt { get; private set; }

    public int? RejectedById { get; private set; }

    /// <summary>Motivo da rejeição, visível ao autor (US-010).</summary>
    public string RejectionReason { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    /// <summary>Quem arquivou (o Administrador), como <see cref="PublishedById"/> e <see cref="RejectedById"/> guardam quem decidiu.</summary>
    public int? ArchivedById { get; private set; }

    /// <summary>Cria um rascunho. Só o título é obrigatório (US-008-S07, S08).</summary>
    /// <exception cref="ValidationException">Título vazio ou acima de <see cref="TitleMaxLength"/>.</exception>
    public static Ad CreateDraft(string title, int authorId)
    {
        Ad ad = new() { AuthorId = authorId, Status = AdStatus.Draft };
        ad.SetText(title, null);
        return ad;
    }

    /// <summary>Define título e descrição e recalcula as colunas de busca. A descrição pode ser vazia (nula).</summary>
    /// <exception cref="ValidationException">Título vazio ou longo demais, ou descrição acima de <see cref="DescriptionMaxLength"/>.</exception>
    public void SetText(string title, string description)
    {
        string trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle))
        {
            throw Invalid("title", AdMessages.TitleRequired);
        }

        if (trimmedTitle.Length > TitleMaxLength)
        {
            throw Invalid("title", $"O título pode ter no máximo {TitleMaxLength} caracteres");
        }

        if (description is { Length: > DescriptionMaxLength })
        {
            throw Invalid("description", $"A descrição pode ter no máximo {DescriptionMaxLength} caracteres");
        }

        Title = trimmedTitle;
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        TitleSearch = Normalizer.Normalize(Title);
        DescriptionSearch = Description is null ? null : Normalizer.Normalize(Description);
    }

    /// <summary>Define o preço em centavos; nulo = sem preço. Zero e negativo não existem (um preço "zero" é o campo vazio).</summary>
    /// <exception cref="ValidationException">Valor zero, negativo ou acima de R$ 99.999.999,99.</exception>
    public void SetPrice(long? priceCents)
    {
        if (priceCents is <= 0 or > FieldLimits.MaxMoneyCents)
        {
            throw Invalid("price", "Informe um preço entre R$ 0,01 e R$ 99.999.999,99");
        }

        PriceCents = priceCents;
    }

    /// <summary>Define a categoria; nulo desfaz. A conferência de que é postável é do serviço do rascunho (3.3) e do envio (3.7).</summary>
    public void SetCategory(int? categoryId) => CategoryId = categoryId;

    /// <summary>Define o endereço. Cep só dígitos; cidade e UF conforme a padronização da US-008 (3.2).</summary>
    public void SetLocation(string cep, string city, string uf, bool manual)
    {
        Cep = string.IsNullOrWhiteSpace(cep) ? null : cep;
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        Uf = string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
        LocationManual = manual;
    }

    public void SetAttributes(AdAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        Attributes = attributes.ToJson();
    }

    /// <summary>
    /// Aplica uma passagem de situação do Apêndice A e a trilha de decisão. Confere só a passagem (<see cref="AdStatusRules"/>); quem pode
    /// fazê-la é conferido pelo <c>IAdService</c>.
    /// </summary>
    /// <exception cref="ConflictException">A passagem não existe no Apêndice A.</exception>
    /// <exception cref="ValidationException">Rejeição sem motivo ou com motivo longo demais.</exception>
    public void ApplyTransition(byte target, int? actorId, DateTime now, string reason)
    {
        if (AdStatusRules.Find(Status, target) is null)
        {
            throw new ConflictException(AdMessages.InvalidTransition(Status, target));
        }

        string trimmedReason = reason?.Trim();
        if (target == AdStatus.Rejected)
        {
            if (string.IsNullOrEmpty(trimmedReason))
            {
                throw Invalid("reason", AdMessages.RejectionReasonRequired);
            }

            if (trimmedReason.Length > RejectionReasonMaxLength)
            {
                throw Invalid("reason", AdMessages.RejectionReasonTooLong(RejectionReasonMaxLength));
            }
        }

        switch (target)
        {
            case AdStatus.InReview:
                SentAt = now;
                ClearRejection(); // o histórico fica na auditoria
                break;
            case AdStatus.Published:
                PublishedAt = now;
                PublishedById = actorId;
                break;
            case AdStatus.Rejected:
                RejectedAt = now;
                RejectedById = actorId;
                RejectionReason = trimmedReason;
                break;
            case AdStatus.Draft: // despublicar
                PublishedAt = null;
                PublishedById = null;
                break;
            case AdStatus.Archived:
                ArchivedAt = now;
                ArchivedById = actorId;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }

        Status = target;
    }

    private void ClearRejection()
    {
        RejectedAt = null;
        RejectedById = null;
        RejectionReason = null;
    }

    private static ValidationException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
