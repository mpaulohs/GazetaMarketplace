namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// A cadeia Marca → Modelo → Ano → Versão do catálogo de veículos (A3), obrigatória em Carros e Motos. Guarda <c>brandId</c>, <c>modelId</c>,
/// <c>modelYear</c> (o mesmo nome do Ano do modelo de Caminhões, para a coluna calculada <c>ModelYear</c> servir a todos) e <c>versionId</c>.
/// As consultas encadeadas chegam na tarefa 2.5.
/// </summary>
internal static class CatalogFields
{
    public static FieldDefinition[] Chain(CatalogKind kind) =>
    [
        Field("brandId", "Marca", "Informe a marca", kind, CatalogLevel.Brand),
        Field("modelId", "Modelo", "Informe o modelo", kind, CatalogLevel.Model),
        Field("modelYear", "Ano", "Informe o ano", kind, CatalogLevel.Year),
        Field("versionId", "Versão", "Informe a versão", kind, CatalogLevel.Version)
    ];

    private static FieldDefinition Field(string key, string label, string requiredMessage, CatalogKind kind, CatalogLevel level) => new()
    {
        Key = key,
        Label = label,
        Type = FieldType.CatalogItem,
        Required = true,
        RequiredMessage = requiredMessage,
        Catalog = new CatalogReference(kind, level),
        // Marca e Modelo filtram por igualdade e o ano por faixa (A7 c); a versão não filtra
        Filter = level switch
        {
            CatalogLevel.Brand or CatalogLevel.Model => FieldFilter.Exact,
            CatalogLevel.Year => FieldFilter.Range,
            _ => FieldFilter.None
        }
    };
}
