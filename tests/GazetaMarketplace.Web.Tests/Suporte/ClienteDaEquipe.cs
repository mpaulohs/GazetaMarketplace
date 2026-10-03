using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Navegador mínimo para as telas da equipe: guarda cookies, não segue redirecionamentos e sabe preencher o formulário de entrada.</summary>
internal static class ClienteDaEquipe
{
    public static HttpClient Novo(FabricaWeb fabrica) => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    /// <summary>Preenche e envia o formulário de entrada, com o token antiforgery da própria página.</summary>
    public static async Task<HttpResponseMessage> EntrarAsync(
        this HttpClient cliente, string email, string senha, string retorno = null, string ip = null)
    {
        string pagina = await cliente.GetStringAsync("/painel/entrar");
        string token = Regex.Match(pagina, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        using FormUrlEncodedContent formulario = new(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Senha"] = senha,
            ["Retorno"] = retorno ?? string.Empty,
            ["__RequestVerificationToken"] = token
        });
        using HttpRequestMessage requisicao = new(HttpMethod.Post, "/painel/entrar") { Content = formulario };
        if (ip is not null)
        {
            requisicao.Headers.Add(FabricaWeb.CabecalhoIpRemoto, ip);
        }

        return await cliente.SendAsync(requisicao);
    }

    /// <summary>Clica em "Sair" (POST com o token que o layout do painel publica na página).</summary>
    public static async Task<HttpResponseMessage> SairAsync(this HttpClient cliente)
    {
        string pagina = await cliente.GetStringAsync("/painel/anuncios");
        string token = Regex.Match(pagina, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;

        using HttpRequestMessage requisicao = new(HttpMethod.Post, "/painel/sair");
        requisicao.Headers.Add("RequestVerificationToken", token);
        return await cliente.SendAsync(requisicao);
    }

    /// <summary>Caminho e consulta do redirecionamento; o cookie manda endereço absoluto, o controller manda relativo.</summary>
    public static string Destino(this HttpResponseMessage resposta) =>
        resposta.Headers.Location.IsAbsoluteUri ? resposta.Headers.Location.PathAndQuery : resposta.Headers.Location.OriginalString;

    public static async Task<string> TextoAsync(this HttpResponseMessage resposta) =>
        System.Net.WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
}
