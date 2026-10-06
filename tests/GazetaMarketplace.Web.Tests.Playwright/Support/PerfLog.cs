using System;
using System.IO;

namespace GazetaMarketplace.Web.Tests.Playwright.Support;

/// <summary>Guarda os números medidos (peso, métricas) num arquivo ao lado do teste, para o relatório citar o valor e não só "passou". Fica em <c>performance-numbers.txt</c> na pasta de saída do projeto.</summary>
internal static class PerfLog
{
    private static readonly object Gate = new();

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "performance-numbers.txt");

    public static void Write(string line)
    {
        Console.WriteLine(line);
        lock (Gate)
        {
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
    }
}
