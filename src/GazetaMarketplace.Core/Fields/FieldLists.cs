using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Listas de opções dos campos. As do GazetaOnline mantêm os <b>ids originais</b> de <c>specs/discovery/gazetaonline-lookups.md</c> (alguns ids
/// têm buracos de propósito); as de Serviços e Vagas de emprego são novas e têm ids 1 a N na ordem do SPEC (Apêndice B). Para estender uma lista
/// depois, acrescente opções com ids novos: nunca mude nem reaproveite o id de uma opção já usada por um anúncio.
/// Os testes de <c>ListsTests</c> relêem as duas fontes e conferem cada opção.
/// </summary>
public static class FieldLists
{
    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList ProductCondition = new(nameof(ProductCondition),
    [
        new(1, "Novo"),
        new(2, "Usado - Excelente"),
        new(3, "Usado - Bom"),
        new(4, "Recondicionado"),
        new(5, "Com defeito ou avarias"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList ApartmentType = new(nameof(ApartmentType),
    [
        new(1, "Padrão"),
        new(2, "Cobertura"),
        new(3, "Duplex/triplex"),
        new(4, "Kitnet"),
        new(5, "Loft"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList HouseType = new(nameof(HouseType),
    [
        new(1, "Padrão"),
        new(2, "Casa de vila"),
        new(3, "Casa de condomínio"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList LandType = new(nameof(LandType),
    [
        new(1, "Terrenos e lotes"),
        new(2, "Sítios e chácaras"),
        new(3, "Fazendas"),
        new(4, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CommercialType = new(nameof(CommercialType),
    [
        new(1, "Escritório"),
        new(2, "Galpão/Depósito"),
        new(3, "Hotel"),
        new(4, "Fábrica"),
        new(5, "Garagem/Vaga"),
        new(6, "Loja"),
        new(7, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PropertyTransactionType = new(nameof(PropertyTransactionType),
    [
        new(1, "Oferta - Vendo"),
        new(2, "Oferta - Alugo"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList ApartmentFeature = new(nameof(ApartmentFeature),
    [
        new(1, "Área de serviço"),
        new(2, "Armários no quarto"),
        new(3, "Armários na cozinha"),
        new(4, "Mobiliado"),
        new(5, "Ar condicionado"),
        new(6, "Churrasqueira"),
        new(7, "Varanda"),
        new(8, "Academia"),
        new(9, "Piscina"),
        new(10, "Quarto de serviço"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList LandFeature = new(nameof(LandFeature),
    [
        new(1, "Área verde"),
        new(2, "Casa sede"),
        new(3, "Pomar"),
        new(4, "Piscina"),
        new(5, "Churrasqueira"),
        new(6, "Poço artesiano"),
        new(7, "Água encanada"),
        new(8, "Energia elétrica"),
        new(9, "Campo de futebol"),
        new(10, "Acesso asfaltado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CommercialFeature = new(nameof(CommercialFeature),
    [
        new(1, "Garagem"),
        new(2, "Segurança 24h"),
        new(3, "Câmeras de segurança"),
        new(4, "Elevador"),
        new(5, "Portaria"),
        new(6, "Acesso para deficientes"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList ApartmentCondoFeature = new(nameof(ApartmentCondoFeature),
    [
        new(1, "Condomínio fechado"),
        new(2, "Elevador"),
        new(3, "Segurança 24h"),
        new(4, "Portaria"),
        new(5, "Permitido animais"),
        new(6, "Academia"),
        new(7, "Piscina"),
        new(8, "Salão de festas"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList HouseCondoFeature = new(nameof(HouseCondoFeature),
    [
        new(1, "Condomínio fechado"),
        new(2, "Segurança 24h"),
        new(3, "Área murada"),
        new(4, "Permitido animais"),
        new(5, "Portão eletrônico"),
        new(6, "Academia"),
        new(7, "Piscina"),
    ]);

    /// <summary>Fonte: SPEC, Apêndice B, Serviços.</summary>
    public static readonly FieldList ServiceType = new(nameof(ServiceType),
    [
        new(1, "Serviços domésticos"),
        new(2, "Outros"),
        new(3, "Babá"),
        new(4, "Eventos / Festas"),
        new(5, "Reparação / Conserto / Reforma"),
        new(6, "Saúde / Beleza"),
        new(7, "Informática"),
        new(8, "Tradução"),
        new(9, "Transporte / Mudanças"),
        new(10, "Profissionais liberais"),
        new(11, "Turismo"),
    ]);

    /// <summary>Fonte: SPEC, Apêndice B, Vagas de emprego.</summary>
    public static readonly FieldList JobArea = new(nameof(JobArea),
    [
        new(1, "Administrativo / Secretariado / Finanças"),
        new(2, "Comercial / Vendas"),
        new(3, "Engenharia / Arquitetura / Design"),
        new(4, "Telecomunicações / Informática / Multimídia"),
        new(5, "Atendimento ao Cliente / Call Center"),
        new(6, "Banco / Seguros / Consultoria / Jurídica"),
        new(7, "Logística / Distribuição"),
        new(8, "Turismo / Hotelaria / Restaurante"),
        new(9, "Educação / Formação"),
        new(10, "Marketing / Comunicação"),
        new(11, "Serviços Domésticos / Limpezas"),
        new(12, "Construção / Industrial"),
        new(13, "Saúde / Medicina / Enfermagem"),
        new(14, "Agricultura / Pecuária / Veterinária"),
    ]);
}
