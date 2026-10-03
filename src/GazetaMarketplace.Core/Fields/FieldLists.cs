using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Listas de opções dos campos. As do GazetaOnline mantêm os <b>ids originais</b> de <c>specs/discovery/gazetaonline-lookups.md</c> (alguns ids
/// têm buracos de propósito); as de Serviços e Vagas de emprego são novas e têm ids 1 a N na ordem do SPEC (Apêndice B). Para estender uma lista
/// depois, acrescente opções com ids novos: nunca mude nem reaproveite o id de uma opção já usada por um anúncio.
/// A lista de anos do modelo não está aqui: ela depende do ano atual (<see cref="ModelYears"/>).
/// Os testes de <c>ListsTests</c> relêem as duas fontes e conferem cada opção.
/// </summary>
public static class FieldLists
{
    /// <summary>
    /// Ano do modelo (<c>VehicleModelYear</c> do GazetaOnline): do ano seguinte ao atual (fabricantes lançam o modelo do ano seguinte no fim do
    /// ano anterior) em ordem decrescente até 1951, mais "1950 ou anterior". O <b>id é o próprio ano</b> (1950 para "1950 ou anterior").
    /// </summary>
    public static FieldList ModelYears(int currentYear)
    {
        List<FieldOption> options = [];
        for (int year = ModelYearRules.MaxYear(currentYear); year > ModelYearRules.MinYear; year--)
        {
            options.Add(new FieldOption(year, year.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        options.Add(new FieldOption(ModelYearRules.MinYear, "1950 ou anterior"));
        return new FieldList("VehicleModelYear", options);
    }

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

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CarFuel = new(nameof(CarFuel),
    [
        new(1, "Gasolina"),
        new(2, "Álcool"),
        new(3, "Flex"),
        new(4, "Diesel"),
        new(5, "Híbrido"),
        new(6, "Elétrico"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CarColor = new(nameof(CarColor),
    [
        new(1, "Amarelo"),
        new(2, "Azul"),
        new(3, "Branco"),
        new(4, "Cinza"),
        new(5, "Laranja"),
        new(6, "Prata"),
        new(7, "Preto"),
        new(8, "Verde"),
        new(9, "Vermelho"),
        new(10, "Outro"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CarTransmission = new(nameof(CarTransmission),
    [
        new(1, "Manual"),
        new(2, "Automático"),
        new(3, "Semi-Automático"),
        new(4, "Automatizado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CarSteeringGear = new(nameof(CarSteeringGear),
    [
        new(1, "Hidráulica"),
        new(2, "Elétrica"),
        new(3, "Mecânica"),
        new(4, "Assistida"),
        new(5, "Eletro-hidráulica"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList VehicleSteeringGear = new(nameof(VehicleSteeringGear),
    [
        new(1, "Elétrica"),
        new(2, "Eletro-hidráulica"),
        new(3, "Hidráulica"),
        new(4, "Mecânica"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CarDoor = new(nameof(CarDoor),
    [
        new(1, "2 portas"),
        new(2, "4 portas"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList CarEnginePower = new(nameof(CarEnginePower),
    [
        new(1, "1.0"),
        new(2, "1.2"),
        new(3, "1.3"),
        new(4, "1.4"),
        new(5, "1.5"),
        new(6, "1.6"),
        new(7, "1.7"),
        new(8, "1.8"),
        new(9, "1.9"),
        new(10, "2.0"),
        new(11, "2.0 - 2.9"),
        new(12, "3.0 - 3.9"),
        new(13, "4.0 ou mais"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList VehicleType = new(nameof(VehicleType),
    [
        new(1, "Buggy"),
        new(2, "Caminhão Leve"),
        new(3, "Conversível"),
        new(4, "Coupé"),
        new(5, "Hatch"),
        new(6, "Perua"),
        new(7, "Pick-up"),
        new(8, "Sedã"),
        new(9, "SUV"),
        new(10, "Van/Utilitário"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList TruckType = new(nameof(TruckType),
    [
        new(1, "Baú"),
        new(2, "Boladeiro"),
        new(3, "Caçamba"),
        new(4, "Carga seca"),
        new(5, "Carroceria"),
        new(6, "Graneleiro"),
        new(7, "Guindaste"),
        new(8, "Munck"),
        new(9, "No Chassi"),
        new(10, "Outro"),
        new(11, "Pipa"),
        new(12, "Plataforma"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList BusType = new(nameof(BusType),
    [
        new(1, "Micro ônibus"),
        new(2, "Ônibus"),
        new(3, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList BoatType = new(nameof(BoatType),
    [
        new(1, "Barco"),
        new(2, "Embarcação Inflável"),
        new(3, "Hovercraft"),
        new(4, "Jet Ski"),
        new(5, "Lancha"),
        new(6, "Motor"),
        new(7, "Outros"),
        new(8, "Pedalinho"),
        new(9, "Veleiro Monocasco"),
        new(10, "Veleiro Multicasco"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList MotorcycleDisplacement = new(nameof(MotorcycleDisplacement),
    [
        new(1, "50"),
        new(2, "85"),
        new(3, "100"),
        new(4, "110"),
        new(5, "115"),
        new(6, "120"),
        new(7, "125"),
        new(8, "150"),
        new(9, "155"),
        new(10, "160"),
        new(11, "180"),
        new(12, "190"),
        new(13, "200"),
        new(14, "230"),
        new(15, "250"),
        new(16, "300"),
        new(17, "321"),
        new(18, "350"),
        new(19, "390"),
        new(20, "400"),
        new(21, "411"),
        new(22, "420"),
        new(23, "450"),
        new(24, "500"),
        new(25, "550"),
        new(26, "600"),
        new(27, "636"),
        new(28, "650"),
        new(29, "660"),
        new(30, "689"),
        new(31, "700"),
        new(32, "750"),
        new(33, "765"),
        new(34, "800"),
        new(35, "850"),
        new(36, "900"),
        new(37, "950"),
        new(38, "1000"),
        new(39, "Acima de 1.000"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList OptionalItem = new(nameof(OptionalItem),
    [
        new(1, "Airbag"),
        new(2, "Ar Condicionado"),
        new(3, "Alarme"),
        new(4, "Bancos de Couro"),
        new(5, "Blindado"),
        new(6, "Camera de ré"),
        new(7, "Com Kit GNV"),
        new(8, "Computador de bordo"),
        new(9, "Conexão USB"),
        new(10, "Controle automático de velocidade"),
        new(11, "Interface Bluetooth"),
        new(12, "Navegador GPS"),
        new(13, "Rodas de liga leve"),
        new(14, "Sensor de ré"),
        new(15, "Som"),
        new(16, "Teto Solar"),
        new(17, "Tração 4x4"),
        new(18, "Trava elétrica"),
        new(19, "Vidro elétrico"),
        new(20, "Volante multifuncional"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList TruckOptionalItem = new(nameof(TruckOptionalItem),
    [
        new(1, "ABS"),
        new(2, "Ar Condicionado"),
        new(3, "Blindado"),
        new(4, "Trava elétrica"),
        new(5, "Vidro elétrico"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList BusOptionalItem = new(nameof(BusOptionalItem),
    [
        new(1, "Ar Condicionado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList MotorcycleOptionalItem = new(nameof(MotorcycleOptionalItem),
    [
        new(1, "ABS"),
        new(2, "Alarme"),
        new(3, "Amortecedor de direção"),
        new(4, "Bolsa / Baú / Bauleto"),
        new(5, "Computador de bordo"),
        new(6, "Contra peso no guidon"),
        new(7, "Escapamento esportivo"),
        new(8, "Faróis de neblina"),
        new(9, "GPS"),
        new(10, "Som"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList AdditionalInfo = new(nameof(AdditionalInfo),
    [
        new(1, "Carro de leilão"),
        new(2, "Com chave reserva"),
        new(3, "Com garantia de fábrica"),
        new(4, "Com manual"),
        new(5, "Com multas"),
        new(6, "IPVA pago"),
        new(7, "Revisões feitas em concessionária"),
        new(8, "Único dono"),
        new(9, "Veículo em financiamento"),
        new(10, "Veículo quitado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList VehicleAdditionalInfo = new(nameof(VehicleAdditionalInfo),
    [
        new(1, "Com chave reserva"),
        new(2, "Com garantia de fábrica"),
        new(3, "Com manual"),
        new(4, "Com multas"),
        new(5, "De leilão"),
        new(6, "IPVA pago"),
        new(7, "Revisões feitas em concessionária"),
        new(8, "Único dono"),
        new(9, "Veículo em financiamento"),
        new(10, "Veículo quitado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList MotorcycleAdditionalInfo = new(nameof(MotorcycleAdditionalInfo),
    [
        new(1, "Com multas"),
        new(2, "De leilão"),
        new(3, "IPVA pago"),
        new(4, "Único dono"),
        new(5, "Veículo em financiamento"),
        new(6, "Veículo quitado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList BoatAdditionalInfo = new(nameof(BoatAdditionalInfo),
    [
        new(1, "De leilão"),
        new(2, "Único dono"),
        new(3, "Veículo em financiamento"),
        new(4, "Veículo quitado"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PartCondition = new(nameof(PartCondition),
    [
        new(1, "Novo"),
        new(2, "Usado - Excelente"),
        new(3, "Usado - Bom"),
        new(4, "Recondicionado"),
        new(5, "Com defeito ou avarias"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PartColor = new(nameof(PartColor),
    [
        new(1, "Amarelo"),
        new(2, "Azul"),
        new(3, "Branco"),
        new(4, "Cinza"),
        new(5, "Laranja"),
        new(6, "Prata"),
        new(7, "Preto"),
        new(8, "Verde"),
        new(9, "Vermelho"),
        new(10, "Outra"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList AutoPartType = new(nameof(AutoPartType),
    [
        new(1, "Acessórios para exterior"),
        new(2, "Acessórios para interior"),
        new(3, "Calotas"),
        new(4, "GPS"),
        new(5, "Peças automotivas"),
        new(6, "Pneus"),
        new(7, "Rodas"),
        new(8, "Som e multimídia"),
        new(9, "Tuning e Performance"),
        new(10, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList MotorcyclePartType = new(nameof(MotorcyclePartType),
    [
        new(1, "Acabamento"),
        new(2, "Alarmes"),
        new(3, "Bagageiros, baús e mochilas"),
        new(4, "Calotas"),
        new(5, "Capacetes"),
        new(6, "Peças de motos"),
        new(7, "Pneus"),
        new(8, "Rodas"),
        new(9, "Roupas de moto"),
        new(10, "Suportes"),
        new(11, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList BoatPartType = new(nameof(BoatPartType),
    [
        new(1, "Âncoras"),
        new(2, "Bombas"),
        new(3, "Cabos"),
        new(4, "Hélices"),
        new(5, "Iluminação"),
        new(6, "Inversores"),
        new(7, "Motores"),
        new(8, "Rotores"),
        new(9, "Sonares e GPS"),
        new(10, "Velas"),
        new(11, "Outros"),
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
