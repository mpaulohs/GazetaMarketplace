namespace GazetaMarketplace.Core.Fields;

/// <summary>Chaves dos campos que o sistema trata de um jeito próprio (a pré-visualização separa as áreas da vaga, o valor do serviço é o tipo). Os grupos usam estas constantes, então a chave não tem duas grafias.</summary>
public static class FieldKeys
{
    /// <summary>Áreas da vaga de emprego (múltipla escolha).</summary>
    public const string JobAreas = "jobAreaIds";

    /// <summary>Tipo do serviço, que ocupa o lugar do preço no anúncio de serviço.</summary>
    public const string ServiceType = "serviceTypeId";
}
