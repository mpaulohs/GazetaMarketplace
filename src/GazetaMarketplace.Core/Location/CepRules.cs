namespace GazetaMarketplace.Core.Location;

/// <summary>Regras do CEP: oito dígitos, sem hífen. O hífen e o resto da formatação são coisa da tela.</summary>
public static class CepRules
{
    public const int Length = 8;

    public const string IncompleteMessage = "Informe um CEP com 8 dígitos";

    public const string NotFoundMessage = "CEP não encontrado. Confira os números.";

    public const string UnavailableMessage = "Não foi possível buscar o CEP agora";

    /// <summary>Verdadeiro só para exatamente 8 dígitos ASCII; qualquer outra coisa (menos dígitos, letras, hífen, espaço) não é consultada.</summary>
    public static bool IsValid(string cep)
    {
        if (cep is null || cep.Length != Length)
        {
            return false;
        }

        foreach (char c in cep)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>"13015-100" ← "13015100", para mostrar na tela.</summary>
    public static string Format(string cep) => IsValid(cep) ? cep[..5] + "-" + cep[5..] : cep;
}
