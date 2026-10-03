using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Navegador mínimo para as telas da equipe: guarda cookies, não segue redirecionamentos e sabe preencher o formulário de entrada.</summary>
internal static class ClienteDaEquipe
{
    public static HttpClient Novo(WebFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    /// <summary>Preenche e envia o formulário de entrada, com o token antiforgery da própria página.</summary>
    public static async Task<HttpResponseMessage> EntrarAsync(
        this HttpClient client, string email, string password, string retorno = null, string ip = null)
    {
        string page = await client.GetStringAsync("/painel/entrar");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        using FormUrlEncodedContent formulario = new(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Senha"] = password,
            ["Retorno"] = retorno ?? string.Empty,
            ["__RequestVerificationToken"] = token
        });
        using HttpRequestMessage request = new(HttpMethod.Post, "/painel/entrar") { Content = formulario };
        if (ip is not null)
        {
            request.Headers.Add(WebFactory.RemoteIpHeader, ip);
        }

        return await client.SendAsync(request);
    }

    /// <summary>Clica em "Sair" (POST com o token que o layout do painel publica na página).</summary>
    public static async Task<HttpResponseMessage> SairAsync(this HttpClient client, string paginaComToken = "/painel/anuncios")
    {
        string page = await client.GetStringAsync(paginaComToken);
        string token = Regex.Match(page, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;

        using HttpRequestMessage request = new(HttpMethod.Post, "/painel/sair");
        request.Headers.Add("RequestVerificationToken", token);
        return await client.SendAsync(request);
    }

    /// <summary>Preenche a tela "Defina sua nova senha" com o token antiforgery da própria página.</summary>
    public static async Task<HttpResponseMessage> DefinirSenhaAsync(this HttpClient client, string nova, string confirmar = null)
    {
        string page = await client.GetStringAsync("/painel/definir-senha");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        using FormUrlEncodedContent formulario = new(new Dictionary<string, string>
        {
            ["NovaSenha"] = nova,
            ["ConfirmarSenha"] = confirmar ?? nova,
            ["__RequestVerificationToken"] = token
        });
        return await client.PostAsync("/painel/definir-senha", formulario);
    }

    /// <summary>Caminho e consulta do redirecionamento; o cookie manda endereço absoluto, o controller manda relativo.</summary>
    public static string Destino(this HttpResponseMessage response) =>
        response.Headers.Location.IsAbsoluteUri ? response.Headers.Location.PathAndQuery : response.Headers.Location.OriginalString;

    public static async Task<string> TextoAsync(this HttpResponseMessage response) =>
        System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
}
