namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Chaves estáveis dos grupos de campos: é o que fica gravado em <c>Categories.FieldGroup</c>. Nunca renomeie uma chave em uso.
/// Os 18 grupos do Apêndice B entram aqui conforme as tarefas 2.2 a 2.4.
/// </summary>
public static class FieldGroupKeys
{
    public const string Services = "Services";

    public const string Jobs = "Jobs";

    /// <summary>Grupo padrão: vale para toda categoria sem grupo próprio na cadeia de ancestrais.</summary>
    public const string GeneralProducts = "GeneralProducts";

    public const string RealEstate = "RealEstate";
}
