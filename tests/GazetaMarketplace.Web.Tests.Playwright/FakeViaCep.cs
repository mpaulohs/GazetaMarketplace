using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>ViaCEP de mentira para as jornadas que só precisam de um CEP válido: 13015100 existe (Campinas/SP) e qualquer outro dá erro. Escuta em GAZETA_E2E_VIACEP_PORT.</summary>
internal sealed class FakeViaCep : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();

    public FakeViaCep()
    {
        int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_VIACEP_PORT"), CultureInfo.InvariantCulture);
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _ = Task.Run(LoopAsync);
    }

    private async Task LoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            bool known = Regex.IsMatch(context.Request.Url!.AbsolutePath, @"^/ws/13015100/json/$");
            context.Response.StatusCode = known ? 200 : 500;
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(known ? """{"cep":"13015-100","localidade":"Campinas","uf":"SP","ibge":"3509502"}""" : "falhou")).ConfigureAwait(false);
            context.Response.Close();
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Close();
    }
}
