using System;
using System.Threading;
using System.Threading.Tasks;

namespace VehicleCatalogExport;

// Classe própria (e não instruções de nível superior) para o tipo não se chamar Program: os testes de integração referenciam o site e a ferramenta juntos
internal static class ExportEntryPoint
{
    private static async Task<int> Main(string[] args)
    {
        using CancellationTokenSource cancel = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };

        return await Cli.RunAsync(args, Environment.GetEnvironmentVariable, Console.Out, Console.Error, cancel.Token);
    }
}
