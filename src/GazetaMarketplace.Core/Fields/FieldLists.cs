using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Ano de fabricação (Máquinas): do ano atual até 1950, em ordem decrescente. O <b>id é o próprio ano</b>. Diferente do ano do modelo de veículos, não
    /// passa do ano atual (máquina fabricada não é do futuro).
    /// </summary>
    public static FieldList ManufactureYears(int currentYear)
    {
        List<FieldOption> options = [];
        for (int year = ManufactureYearRules.MaxYear(currentYear); year >= ManufactureYearRules.MinYear; year--)
        {
            options.Add(new FieldOption(year, year.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new FieldList("ManufactureYear", options);
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

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList SeasonalType = new(nameof(SeasonalType),
    [
        new(1, "Apartamento"),
        new(2, "Casa"),
        new(3, "Quarto individual"),
        new(4, "Quarto compartilhado"),
        new(5, "Hotel, hostel e pousada"),
        new(6, "Sítio, fazenda e chácara"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList SeasonalFeature = new(nameof(SeasonalFeature),
    [
        new(1, "Geladeira"),
        new(2, "Fogão"),
        new(3, "Estacionamento"),
        new(4, "Ventilador"),
        new(5, "Varanda/Terraço"),
        new(6, "TV a cabo"),
        new(7, "Churrasqueira"),
        new(8, "Piscina"),
        new(9, "Ar condicionado"),
        new(10, "Roupa de cama"),
        new(11, "Internet"),
        new(12, "Permitido animais"),
        new(13, "Máquina de lavar"),
        new(14, "Toalhas"),
        new(15, "Café da manhã"),
        new(16, "Aquecimento"),
        new(17, "Lareira"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList RoomFeature = new(nameof(RoomFeature),
    [
        new(1, "Armário no quarto"),
        new(2, "Banheiro no quarto"),
        new(3, "Mobiliado"),
        new(4, "Ar condicionado"),
        new(5, "Varanda"),
        new(6, "Aquecimento"),
        new(7, "Internet"),
        new(8, "TV a cabo"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList SeasonalPaymentType = new(nameof(SeasonalPaymentType),
    [
        new(1, "Por dia"),
        new(2, "Por semana"),
        new(3, "Por mês"),
        new(4, "Pacote"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PhoneStorage = new(nameof(PhoneStorage),
    [
        new(1, "512MB"),
        new(2, "1GB"),
        new(3, "2GB"),
        new(4, "4GB"),
        new(5, "8GB"),
        new(6, "16GB"),
        new(7, "32GB"),
        new(8, "64GB"),
        new(9, "128GB"),
        new(10, "256GB"),
        new(11, "512GB"),
        new(12, "1TB"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PhoneColor = new(nameof(PhoneColor),
    [
        new(1, "Amarelo"),
        new(2, "Azul"),
        new(3, "Branco"),
        new(4, "Bronze"),
        new(5, "Cinza"),
        new(6, "Dourado"),
        new(7, "Laranja"),
        new(8, "Prata"),
        new(9, "Preto"),
        new(10, "Rosa"),
        new(11, "Roxo"),
        new(12, "Verde"),
        new(13, "Vermelho"),
        new(14, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PhoneBrand = new(nameof(PhoneBrand),
    [
        new(1, "Apple"),
        new(2, "Asus"),
        new(3, "Huawei"),
        new(4, "Infinix"),
        new(5, "Lenovo"),
        new(6, "LG"),
        new(7, "Motorola"),
        new(8, "Samsung"),
        new(9, "Sony"),
        new(10, "Xiaomi"),
        new(11, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList SmartwatchBrand = new(nameof(SmartwatchBrand),
    [
        new(1, "Apple"),
        new(2, "Samsung"),
        new(3, "Garmin"),
        new(4, "Xiaomi"),
        new(5, "Amazfit"),
        new(6, "Fitbit"),
        new(7, "Huawei"),
        new(8, "Fossil"),
        new(9, "Mobvoi"),
        new(10, "Suunto"),
        new(11, "Polar"),
        new(12, "Coros"),
        new(13, "Tag Heuer"),
        new(14, "Withings"),
        new(15, "Michael Kors"),
        new(16, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PhoneBatteryHealth = new(nameof(PhoneBatteryHealth),
    [
        new(1, "Perfeita (95% até 100%)"),
        new(2, "Boa (80% até 94%)"),
        new(3, "OK (60% até 79%)"),
        new(4, "Ruim (40% até 59%)"),
        new(5, "Muito ruim (abaixo de 39%)"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PhoneAccessoryType = new(nameof(PhoneAccessoryType),
    [
        new(1, "Adaptadores para Celular"),
        new(2, "Cabos para Celular"),
        new(3, "Capas para Celular"),
        new(4, "Carregadores de Celular"),
        new(5, "Carregadores Portáteis"),
        new(6, "Controles para Celular"),
        new(7, "Estabilizadores e Gimbals"),
        new(8, "Microfones para Celular"),
        new(9, "Películas para Celular"),
        new(10, "Ring Lights para Celular"),
        new(11, "Suportes para Celular"),
        new(12, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PhonePartType = new(nameof(PhonePartType),
    [
        new(1, "Baterias de Celular"),
        new(2, "Carcaças de Celular"),
        new(3, "Conectores de Celular"),
        new(4, "Câmeras de Celular"),
        new(5, "Displays e Telas de Celular"),
        new(6, "Placas-mãe de Celular"),
        new(7, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList SmartwatchAccessoryType = new(nameof(SmartwatchAccessoryType),
    [
        new(1, "Carregadores para Smartwatch"),
        new(2, "Películas para Smartwatch"),
        new(3, "Pulseiras para Smartwatch"),
        new(4, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList LandlinePhoneType = new(nameof(LandlinePhoneType),
    [
        new(1, "Aparelhos de Telefone Fixo"),
        new(2, "Centrais Telefônicas"),
        new(3, "Interfones"),
        new(4, "Telefones sem Fio"),
        new(5, "Walkie Talkie"),
        new(6, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList AirConditionerCapacity = new(nameof(AirConditionerCapacity),
    [
        new(1, "Até 9000 BTUs"),
        new(2, "10000 até 15000 BTUs"),
        new(3, "16000 até 20000 BTUs"),
        new(4, "Acima de 20000 BTUs"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList AirConditionerType = new(nameof(AirConditionerType),
    [
        new(1, "Ar-condicionados de Janela"),
        new(2, "Ar-condicionados Piso Teto"),
        new(3, "Ar-condicionados Portáteis"),
        new(4, "Ar-condicionados Split"),
        new(5, "Ar-condicionados Split Cassete"),
        new(6, "Ar-condicionados Split Inverter"),
        new(7, "Peças Para Ar-condicionado"),
        new(8, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList FanType = new(nameof(FanType),
    [
        new(1, "Aquecedores"),
        new(2, "Circuladores de Ar"),
        new(3, "Climatizadores de Ar"),
        new(4, "Exaustores"),
        new(5, "Ventiladores de Parede"),
        new(6, "Ventiladores de Torre e Coluna"),
        new(7, "Ventiladores de Mesa"),
        new(8, "Ventiladores de Teto"),
        new(9, "Peças Para Ventiladores e Climatizadores"),
        new(10, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList RefrigeratorType = new(nameof(RefrigeratorType),
    [
        new(1, "Adegas e Cervejeiras Climatizadas"),
        new(2, "Freezers Horizontais"),
        new(3, "Freezers Verticais"),
        new(4, "Frigobares"),
        new(5, "Geladeiras"),
        new(6, "Geladeiras Duplex"),
        new(7, "Geladeiras Inverse"),
        new(8, "Peças Para Geladeiras e Freezers"),
        new(9, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList StoveType = new(nameof(StoveType),
    [
        new(1, "Coifas e Depuradores"),
        new(2, "Cooktop a Gás"),
        new(3, "Cooktop Por Indução"),
        new(4, "Fogões a Gás"),
        new(5, "Fogões Elétricos"),
        new(6, "Fornos a Gás"),
        new(7, "Fornos Elétricos"),
        new(8, "Micro-Ondas"),
        new(9, "Peças Para Fogões e Fornos"),
        new(10, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList WasherType = new(nameof(WasherType),
    [
        new(1, "Centrífugas de Roupa"),
        new(2, "Lava e Seca"),
        new(3, "Lava Louças"),
        new(4, "Máquinas de Lavar Roupa"),
        new(5, "Secadoras de Roupa"),
        new(6, "Tanquinhos"),
        new(7, "Peças Para Máquinas de Lavar e Secar"),
        new(8, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList KitchenApplianceType = new(nameof(KitchenApplianceType),
    [
        new(1, "Aspiradores de Pó"),
        new(2, "Batedeiras Elétricas"),
        new(3, "Bebedouros e Purificadores de Água"),
        new(4, "Cafeteiras Elétricas"),
        new(5, "Churrasqueiras Elétricas"),
        new(6, "Ferros de Passar"),
        new(7, "Fritadeiras Elétricas"),
        new(8, "Grills, Sanduicheiras e Torradeiras"),
        new(9, "Liquidificadores"),
        new(10, "Máquinas de Costurar Elétricas"),
        new(11, "Panelas Elétricas"),
        new(12, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList PersonalCareApplianceType = new(nameof(PersonalCareApplianceType),
    [
        new(1, "Aparadores e Barbeadores Elétricos"),
        new(2, "Chapinhas"),
        new(3, "Escovas Elétricas"),
        new(4, "Modeladores de Cacho"),
        new(5, "Pranchas de Cabelo"),
        new(6, "Secadores de Cabelo"),
        new(7, "Outros"),
    ]);

    /// <summary>Fonte: gazetaonline-lookups.md.</summary>
    public static readonly FieldList ApplianceVoltage = new(nameof(ApplianceVoltage),
    [
        new(1, "127v"),
        new(2, "220v"),
        new(3, "Bivolt"),
    ]);

    /// <summary>Lista nova (PL-01, 2026-10-03). Ids 1 a 7 na ordem do Product Owner; para estender, acrescente ids novos.</summary>
    public static readonly FieldList ClothingSize = new(nameof(ClothingSize),
    [
        new(1, "PP"),
        new(2, "P"),
        new(3, "M"),
        new(4, "G"),
        new(5, "GG"),
        new(6, "XG"),
        new(7, "XGG"),
    ]);

    /// <summary>Lista nova (PL-01): numeração de calçado adulto, 34 a 45. O id é a própria numeração.</summary>
    public static readonly FieldList AdultShoeSize = new(nameof(AdultShoeSize), [.. Enumerable.Range(34, 12).Select(n => new FieldOption(n, n.ToString(System.Globalization.CultureInfo.InvariantCulture)))]);

    /// <summary>Lista nova (PL-01): numeração de calçado infantil e de bebê, 16 a 33. O id é a própria numeração.</summary>
    public static readonly FieldList ChildShoeSize = new(nameof(ChildShoeSize), [.. Enumerable.Range(16, 18).Select(n => new FieldOption(n, n.ToString(System.Globalization.CultureInfo.InvariantCulture)))]);

    /// <summary>Lista nova (PL-01). O texto da decisão trazia "Unisseque"; adotado "Unissex" (confirmado em 2026-10-03).</summary>
    public static readonly FieldList Gender = new(nameof(Gender),
    [
        new(1, "Masculino"),
        new(2, "Feminino"),
        new(3, "Unissex"),
        new(4, "Infantil"),
    ]);
}
