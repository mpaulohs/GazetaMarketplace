using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Navegador mínimo para as telas da equipe: guarda cookies, não segue redirecionamentos e sabe preencher o formulário de entrada.</summary>
internal static class TeamClient
{
    public static HttpClient Create(WebFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    /// <summary>Preenche e envia o formulário de entrada, com o token antiforgery da própria página.</summary>
    public static async Task<HttpResponseMessage> SignInAsync(
        this HttpClient client, string email, string password, string returnUrl = null, string ip = null)
    {
        string page = await client.GetStringAsync("/painel/entrar");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = returnUrl ?? string.Empty,
            ["__RequestVerificationToken"] = token
        });
        using HttpRequestMessage request = new(HttpMethod.Post, "/painel/entrar") { Content = form };
        if (ip is not null)
        {
            request.Headers.Add(WebFactory.RemoteIpHeader, ip);
        }

        return await client.SendAsync(request);
    }

    /// <summary>Clica em "Sair" (POST com o token que o layout do painel publica na página).</summary>
    public static async Task<HttpResponseMessage> SignOutAsync(this HttpClient client, string pageWithToken = "/painel/anuncios")
    {
        string page = await client.GetStringAsync(pageWithToken);
        string token = Regex.Match(page, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;

        using HttpRequestMessage request = new(HttpMethod.Post, "/painel/sair");
        request.Headers.Add("RequestVerificationToken", token);
        return await client.SendAsync(request);
    }

    /// <summary>Preenche a tela "Defina sua nova senha" com o token antiforgery da própria página.</summary>
    public static async Task<HttpResponseMessage> SetPasswordAsync(this HttpClient client, string newPassword, string confirm = null)
    {
        string page = await client.GetStringAsync("/painel/definir-senha");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["NewPassword"] = newPassword,
            ["ConfirmPassword"] = confirm ?? newPassword,
            ["__RequestVerificationToken"] = token
        });
        return await client.PostAsync("/painel/definir-senha", form);
    }

    /// <summary>Caminho e consulta do redirecionamento; o cookie manda endereço absoluto, o controller manda relativo.</summary>
    public static string Destination(this HttpResponseMessage response) =>
        response.Headers.Location.IsAbsoluteUri ? response.Headers.Location.PathAndQuery : response.Headers.Location.OriginalString;

    public static async Task<string> TextAsync(this HttpResponseMessage response) =>
        System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
}
