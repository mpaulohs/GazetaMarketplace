using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// A matriz de acesso do site inteiro (NFR-13; tarefa 6.1). O teste <b>descobre</b> todos os endpoints do host (<c>EndpointDataSource</c>) e os compara com uma <b>tabela escrita à mão</b> que diz,
/// para cada ação, quem pode: <see cref="Access.Public"/> (qualquer visitante), <see cref="Access.PanelAnonymous"/> (a entrada e a recuperação de senha do painel), <see cref="Access.Writer"/> (Redator ou
/// Administrador) ou <see cref="Access.Administrator"/>. A tabela é a fonte da verdade (não é derivada dos atributos, senão o teste só repetiria o código): rota nova sem entrada na tabela, ou entrada
/// de rota que sumiu, <b>falha o teste</b>. Depois cada endpoint é chamado de verdade sem login, como Redator e como Administrador, e a resposta tem de ser a do contrato: páginas redirecionam para
/// <c>/painel/entrar</c> ou <c>/painel/acesso-negado</c>, a API responde 401 ou 403 com "Você não tem permissão para esta operação.", e nenhuma resposta negada revela conteúdo.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AccessMatrixTests
#pragma warning restore CA1515
{
    internal enum Access { Public, PanelAnonymous, Writer, Administrator }

    private const string DeniedMessage = "Você não tem permissão para esta operação.";

    // Cada linha: "MÉTODO padrão-da-rota -> Controller.Ação". Ao acrescentar uma rota, acrescente a linha aqui, pensando em quem pode chamá-la.
    internal static readonly IReadOnlyDictionary<string, Access> Matrix = new Dictionary<string, Access>
    {
        // ---- Site público ----
        ["* {controller=Home}/{action=Index}/{id?} -> Home.Index"] = Access.Public,
        ["* {controller=Home}/{action=Index}/{id?} -> Home.Error"] = Access.Public,
        ["GET anuncio/{id:int}/{slug?} -> Ad.Index"] = Access.Public,
        ["GET busca -> Search.Index"] = Access.Public,
        ["GET categoria/{slug} -> Category.Index"] = Access.Public,
        ["GET favoritos -> Favorites.Index"] = Access.Public,
        ["GET favoritos/lista -> Favorites.List"] = Access.Public,
        ["GET fotos/{adId:int}/{photoId:int}-{size}.webp -> Photos.Get"] = Access.Public,
        ["GET robots.txt -> Seo.Robots"] = Access.Public,
        ["GET sitemap.xml -> Seo.Sitemap"] = Access.Public,
        ["GET api/v1/ads -> PublicAds.List"] = Access.Public,
        ["GET api/v1/public/cities -> PublicCities.List"] = Access.Public,

        // O catálogo de veículos alimenta os filtros de marca e modelo da busca pública; não leva dado de ninguém
        ["GET api/v1/vehicle-catalog/brands -> VehicleCatalog.Brands"] = Access.Public,
        ["GET api/v1/vehicle-catalog/brands/{brandId:int:min(1)}/models -> VehicleCatalog.Models"] = Access.Public,
        ["GET api/v1/vehicle-catalog/models/{modelId:int:min(1)}/years -> VehicleCatalog.Years"] = Access.Public,
        ["GET api/v1/vehicle-catalog/models/{modelId:int:min(1)}/years/{year:int}/versions -> VehicleCatalog.Versions"] = Access.Public,

        // ---- Entrada do painel (sem login, por desenho) ----
        ["GET painel/acesso-negado -> Account.AccessDenied"] = Access.PanelAnonymous,
        ["GET painel/entrar -> Account.SignIn"] = Access.PanelAnonymous,
        ["POST painel/entrar -> Account.SignIn"] = Access.PanelAnonymous,
        ["GET painel/esqueci-minha-senha -> Account.Forgot"] = Access.PanelAnonymous,
        ["POST painel/esqueci-minha-senha -> Account.Forgot"] = Access.PanelAnonymous,
        ["GET painel/redefinir-senha -> Account.Reset"] = Access.PanelAnonymous,
        ["POST painel/redefinir-senha -> Account.Reset"] = Access.PanelAnonymous,
        ["POST painel/sair -> Account.SignOut"] = Access.PanelAnonymous,

        // ---- Redator ou Administrador: os anúncios do próprio autor (a autoria é conferida em OwnershipMatrixTests) ----
        ["GET painel/anuncios -> Ads.Index"] = Access.Writer,
        ["GET painel/anuncios/campos -> Ads.Fields"] = Access.Writer,
        ["GET painel/anuncios/novo -> Ads.New"] = Access.Writer,
        ["POST painel/anuncios/novo -> Ads.Create"] = Access.Writer,
        ["POST painel/anuncios/novo/atualizar -> Ads.RefreshNew"] = Access.Writer,
        ["GET painel/anuncios/{id:int}/editar -> Ads.Edit"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/editar -> Ads.Save"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/atualizar -> Ads.Refresh"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/enviar -> Ads.SubmitForReview"] = Access.Writer,
        ["GET painel/anuncios/{id:int}/enviar/confirmar -> Ads.ConfirmSubmit"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/enviar/confirmar -> Ads.Submit"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/fotos -> AdPhotoPages.Upload"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/fotos/{photoId:int}/capa -> AdPhotoPages.SetCover"] = Access.Writer,
        ["GET painel/anuncios/{id:int}/fotos/{photoId:int}/remover -> AdPhotoPages.ConfirmRemoval"] = Access.Writer,
        ["POST painel/anuncios/{id:int}/fotos/{photoId:int}/remover -> AdPhotoPages.Remove"] = Access.Writer,
        ["POST api/v1/ads/{adId:int}/photos -> AdPhotos.Upload"] = Access.Writer,
        ["DELETE api/v1/ads/{adId:int}/photos/{photoId:int} -> AdPhotos.Delete"] = Access.Writer,
        ["POST api/v1/ads/{adId:int}/photos/{photoId:int}/cover -> AdPhotos.SetCover"] = Access.Writer,
        ["GET api/v1/cep/{cep} -> Cep.Get"] = Access.Writer,
        ["GET api/v1/cities -> Cities.List"] = Access.Writer,
        ["GET painel/definir-senha -> Password.SetPassword"] = Access.Writer,
        ["POST painel/definir-senha -> Password.SetPassword"] = Access.Writer,

        // ---- Só o Administrador ----
        ["GET painel/anuncios/fila -> ReviewQueue.Index"] = Access.Administrator,
        ["GET painel/anuncios/{id:int}/pre-visualizacao -> ReviewQueue.Preview"] = Access.Administrator,
        ["GET painel/anuncios/{id:int}/publicar -> ReviewQueue.ConfirmPublish"] = Access.Administrator,
        ["POST painel/anuncios/{id:int}/publicar -> ReviewQueue.Publish"] = Access.Administrator,
        ["GET painel/anuncios/{id:int}/rejeitar -> ReviewQueue.Reject"] = Access.Administrator,
        ["POST painel/anuncios/{id:int}/rejeitar -> ReviewQueue.Reject"] = Access.Administrator,
        ["GET painel/anuncios/{id:int}/despublicar -> Ads.ConfirmUnpublish"] = Access.Administrator,
        ["POST painel/anuncios/{id:int}/despublicar -> Ads.Unpublish"] = Access.Administrator,
        ["GET painel/anuncios/{id:int}/arquivar -> Ads.ConfirmArchive"] = Access.Administrator,
        ["POST painel/anuncios/{id:int}/arquivar -> Ads.Archive"] = Access.Administrator,
        ["GET painel/categorias -> Categories.Index"] = Access.Administrator,
        ["GET painel/categorias/nova -> Categories.New"] = Access.Administrator,
        ["POST painel/categorias/nova -> Categories.New"] = Access.Administrator,
        ["GET painel/categorias/{id:int}/editar -> Categories.Edit"] = Access.Administrator,
        ["POST painel/categorias/{id:int}/editar -> Categories.Edit"] = Access.Administrator,
        ["GET painel/categorias/{id:int}/excluir -> Categories.ConfirmDelete"] = Access.Administrator,
        ["POST painel/categorias/{id:int}/excluir -> Categories.Delete"] = Access.Administrator,
        ["POST painel/categorias/{id:int}/mover/{direction} -> Categories.Move"] = Access.Administrator,
        ["GET painel/componentes -> Components.Index"] = Access.Administrator,
        ["GET painel/configuracoes -> Settings.Index"] = Access.Administrator,
        ["POST painel/configuracoes -> Settings.Save"] = Access.Administrator,
        ["GET painel/usuarios -> Users.Index"] = Access.Administrator,
        ["GET painel/usuarios/novo -> Users.New"] = Access.Administrator,
        ["POST painel/usuarios/novo -> Users.New"] = Access.Administrator,
        ["GET painel/usuarios/{id:int}/editar -> Users.Edit"] = Access.Administrator,
        ["POST painel/usuarios/{id:int}/editar -> Users.Edit"] = Access.Administrator,
        ["GET painel/usuarios/{id:int}/desativar -> Users.ConfirmDeactivation"] = Access.Administrator,
        ["POST painel/usuarios/{id:int}/desativar -> Users.Deactivate"] = Access.Administrator,
        ["POST painel/usuarios/{id:int}/reativar -> Users.Reactivate"] = Access.Administrator,
        ["GET painel/usuarios/{id:int}/redefinir-senha -> Users.ResetPassword"] = Access.Administrator,
        ["POST painel/usuarios/{id:int}/redefinir-senha -> Users.ResetPassword"] = Access.Administrator
    };

    // Valor de exemplo de cada parâmetro de rota (os que não aparecem aqui valem 1)
    private static readonly Dictionary<string, string> Samples = new()
    {
        ["slug"] = "x", ["size"] = "480", ["cep"] = "13015100", ["direction"] = "up", ["year"] = "2020"
    };

    internal sealed record Endpoint(string Key, string Pattern, string Method, string Controller, string Action)
    {
        public bool IsApi => Pattern.StartsWith("api/", StringComparison.Ordinal);

        public string SampleUrl()
        {
            if (Pattern.StartsWith("{controller=", StringComparison.Ordinal))
            {
                return "/" + Controller + "/" + Action;
            }

            return "/" + Regex.Replace(Pattern, @"\{(?:\*{0,2})(\w+)[^}]*\}", m => Samples.GetValueOrDefault(m.Groups[1].Value, "1"));
        }

        public string[] Methods => Method == "*" ? ["GET"] : [Method];
    }

    // ---------- Descoberta ----------

    internal static (Endpoint[] Controllers, string[] Others) Discover(IServiceProvider services)
    {
        List<Endpoint> controllers = [];
        List<string> others = [];
        foreach (RouteEndpoint endpoint in services.GetServices<EndpointDataSource>().SelectMany(d => d.Endpoints).OfType<RouteEndpoint>())
        {
            ControllerActionDescriptor action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action is null)
            {
                others.Add(endpoint.DisplayName ?? string.Empty);
                continue;
            }

            // Os controllers de apoio dos testes (/teste/*, /api/v1/teste/*) vivem no projeto de teste e não existem no site de verdade
            if (action.ControllerTypeInfo.Assembly != typeof(Program).Assembly)
            {
                continue;
            }

            string methods = string.Join("/", (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"]).OrderBy(m => m, StringComparer.Ordinal));
            string pattern = endpoint.RoutePattern.RawText!.TrimStart('/');
            controllers.Add(new Endpoint($"{methods} {pattern} -> {action.ControllerName}.{action.ActionName}", pattern, methods, action.ControllerName, action.ActionName));
        }

        return ([.. controllers.OrderBy(e => e.Key, StringComparer.Ordinal)], [.. others]);
    }

    private static string Describe(Endpoint e, Access access) => $"{e.Key} [{access}]";

    // ---------- A tabela e o site dizem a mesma coisa ----------

    [TestMethod]
    public async Task EveryEndpoint_IsInTheMatrix_AndEveryMatrixEntryExists()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (Endpoint[] controllers, _) = Discover(site.Harness.Factory.Services);

        string[] discovered = [.. controllers.Select(e => e.Key)];
        string[] missingFromMatrix = [.. discovered.Except(Matrix.Keys, StringComparer.Ordinal)];
        string[] missingFromSite = [.. Matrix.Keys.Except(discovered, StringComparer.Ordinal)];
        Assert.IsEmpty(missingFromMatrix, "rota nova sem entrada na matriz de acesso (AccessMatrixTests.Matrix): decida quem pode chamá-la e acrescente a linha:\n" + string.Join("\n", missingFromMatrix));
        Assert.IsEmpty(missingFromSite, "entrada da matriz sem rota no site (a rota sumiu ou mudou de padrão):\n" + string.Join("\n", missingFromSite));
        Assert.AreEqual(discovered.Length, discovered.Distinct().Count(), "duas ações com a mesma chave");
        Assert.IsGreaterThanOrEqualTo(70, discovered.Length, "a descoberta achou poucas rotas: " + discovered.Length);
    }

    [TestMethod]
    public async Task EndpointsOutsideControllers_AreOnlyStaticFiles_HealthChecksAndTheFallback()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (_, string[] others) = Discover(site.Harness.Factory.Services);

        string[] unexpected = [.. others.Where(o => o.Length > 0 && o != "Health checks" && !o.StartsWith("Fallback ", StringComparison.Ordinal) && !o.StartsWith("Route: ", StringComparison.Ordinal))];
        Assert.IsEmpty(unexpected, "endpoint fora de controller, sem classificação (um MapGet novo precisa de política e de teste):\n" + string.Join("\n", unexpected));
        Assert.AreEqual(2, others.Count(o => o == "Health checks"), "/health/live e /health/ready");
    }

    [TestMethod]
    public async Task ThePolicyDeclaredInTheCode_MatchesTheMatrix_ForEveryAction()
    {
        // Segunda leitura da mesma verdade: o que o atributo [Authorize] do código diz tem de bater com a tabela (a tabela manda; o código tem de concordar)
        using DraftSite site = await DraftSite.StartAsync();
        List<string> wrong = [];
        foreach (RouteEndpoint endpoint in site.Harness.Factory.Services.GetServices<EndpointDataSource>().SelectMany(d => d.Endpoints).OfType<RouteEndpoint>())
        {
            ControllerActionDescriptor action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action is null || action.ControllerTypeInfo.Assembly != typeof(Program).Assembly)
            {
                continue;
            }

            string methods = string.Join("/", (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"]).OrderBy(m => m, StringComparer.Ordinal));
            string key = $"{methods} {endpoint.RoutePattern.RawText!.TrimStart('/')} -> {action.ControllerName}.{action.ActionName}";
            string[] policies = [.. endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy ?? "authenticated")];
            bool anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            if (!Matrix.TryGetValue(key, out Access expected))
            {
                continue;
            }

            bool ok = expected switch
            {
                Access.PanelAnonymous => anonymous,
                Access.Public => policies.Length == 0 && !key.Contains("painel/", StringComparison.Ordinal),
                Access.Writer => !anonymous && policies.Contains("Writer") && !policies.Contains("Administrator"),
                _ => !anonymous && policies.Contains("Administrator")
            };
            if (!ok)
            {
                wrong.Add($"{key}: a matriz diz {expected}, o código declara [{string.Join(",", policies)}]{(anonymous ? " + AllowAnonymous" : string.Empty)}");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    // ---------- Chamadas de verdade ----------

    private static async Task<string> TokenAsync(HttpClient client)
    {
        string page = await client.GetStringAsync("/painel/anuncios");
        return Regex.Match(page, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> CallAsync(HttpClient client, Endpoint endpoint, string method, string token = null)
    {
        using HttpRequestMessage request = new(new HttpMethod(method), endpoint.SampleUrl());
        if (method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            if (token is not null)
            {
                request.Headers.Add("RequestVerificationToken", token);
            }

            if (method != "DELETE")
            {
                // O envio de foto só aceita multipart (outro tipo dá 415 antes de qualquer coisa); as demais ações recebem um formulário vazio
                request.Content = endpoint.Action == "Upload" ? new MultipartFormDataContent { { new StringContent("x"), "x" } } : new FormUrlEncodedContent(new Dictionary<string, string>());
            }
        }

        return await client.SendAsync(request);
    }

    private static bool IsSignInRedirect(HttpResponseMessage r) => r.StatusCode == HttpStatusCode.Redirect && r.Destination().StartsWith("/painel/entrar", StringComparison.Ordinal);

    private static bool IsDeniedRedirect(HttpResponseMessage r) => r.StatusCode == HttpStatusCode.Redirect && r.Destination().StartsWith("/painel/acesso-negado", StringComparison.Ordinal);

    // A própria página "sem permissão" responde 403 para todo mundo; e o catálogo de componentes só existe em Development (404 em qualquer outro ambiente, para todo mundo)
    private static bool IsDenialPage(Endpoint e) => e.Action == "AccessDenied";

    private static bool IsDevelopmentOnly(Endpoint e) => e.Controller == "Components";

    private static bool IsAnyDenial(HttpResponseMessage r) => IsSignInRedirect(r) || IsDeniedRedirect(r) || r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    [TestMethod]
    public async Task Anonymous_PublicAndPanelAnonymousEndpoints_AreNotBlocked_AndTheRestRequireLogin()
    {
        using DraftSite site = await DraftSite.StartAsync();
        (Endpoint[] endpoints, _) = Discover(site.Harness.Factory.Services);
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        foreach (Endpoint endpoint in endpoints)
        {
            Access access = Matrix[endpoint.Key];
            foreach (string method in endpoint.Methods)
            {
                using HttpResponseMessage response = await CallAsync(visitor, endpoint, method);
                string body = await response.Content.ReadAsStringAsync();
                if (IsDevelopmentOnly(endpoint))
                {
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, Describe(endpoint, access) + " fora de Development");
                    continue;
                }

                if (access is Access.Public or Access.PanelAnonymous)
                {
                    if (IsDenialPage(endpoint) ? response.StatusCode != HttpStatusCode.Forbidden : IsAnyDenial(response) || (int)response.StatusCode >= 500)
                    {
                        wrong.Add($"{Describe(endpoint, access)} sem login: {(int)response.StatusCode} {response.Headers.Location}");
                    }

                    continue;
                }

                // Redator e Administrador: sem login a página vai para a entrada e a API responde 401, nunca com conteúdo
                bool right = endpoint.IsApi ? response.StatusCode == HttpStatusCode.Unauthorized : IsSignInRedirect(response);
                if (!right)
                {
                    wrong.Add($"{Describe(endpoint, access)} sem login: {(int)response.StatusCode} {response.Headers.Location}");
                }
                else if (body.Length > 0 && !body.TrimStart().StartsWith('{'))
                {
                    wrong.Add($"{Describe(endpoint, access)} sem login devolveu corpo: {body[..Math.Min(80, body.Length)]}");
                }
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task Writer_ReachesWriterEndpoints_ButIsDeniedEveryAdministratorEndpoint()
    {
        using DraftSite site = await DraftSite.StartAsync();
        (Endpoint[] endpoints, _) = Discover(site.Harness.Factory.Services);
        string token = await TokenAsync(site.Writer);
        List<string> wrong = [];

        foreach (Endpoint endpoint in endpoints)
        {
            Access access = Matrix[endpoint.Key];
            if (endpoint.Key.Contains("painel/sair", StringComparison.Ordinal))
            {
                continue; // sair encerra a sessão deste cliente
            }

            foreach (string method in endpoint.Methods)
            {
                using HttpResponseMessage response = await CallAsync(site.Writer, endpoint, method, token);
                string body = await response.Content.ReadAsStringAsync();
                if (IsDevelopmentOnly(endpoint))
                {
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, Describe(endpoint, access) + " fora de Development");
                    continue;
                }

                if (IsDenialPage(endpoint))
                {
                    Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, Describe(endpoint, access));
                    continue;
                }

                if (access == Access.Administrator)
                {
                    bool right = endpoint.IsApi ? response.StatusCode == HttpStatusCode.Forbidden && body.Contains(DeniedMessage, StringComparison.Ordinal) : IsDeniedRedirect(response);
                    if (!right)
                    {
                        wrong.Add($"{Describe(endpoint, access)} como Redator devia ser negado: {(int)response.StatusCode} {response.Headers.Location}");
                    }
                    else if (!endpoint.IsApi && body.Length > 0)
                    {
                        wrong.Add($"{Describe(endpoint, access)} negado, mas devolveu corpo");
                    }

                    continue;
                }

                if (access == Access.PanelAnonymous || IsSignInRedirect(response) || (access == Access.Writer && IsAnyDenial(response)) || (int)response.StatusCode >= 500)
                {
                    // Entrar de novo logado também é permitido (a página responde, não nega); o que não pode é negar o Redator em rota de Redator, ou quebrar
                    if (access == Access.PanelAnonymous && !IsAnyDenial(response) && (int)response.StatusCode < 500)
                    {
                        continue;
                    }

                    wrong.Add($"{Describe(endpoint, access)} como Redator: {(int)response.StatusCode} {response.Headers.Location}");
                }
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task Administrator_IsNotDeniedAnywhere()
    {
        using DraftSite site = await DraftSite.StartAsync();
        (Endpoint[] endpoints, _) = Discover(site.Harness.Factory.Services);
        string token = await TokenAsync(site.Admin);
        List<string> wrong = [];

        foreach (Endpoint endpoint in endpoints)
        {
            Access access = Matrix[endpoint.Key];
            if (endpoint.Key.Contains("painel/sair", StringComparison.Ordinal))
            {
                continue; // sair encerra a sessão deste cliente
            }

            foreach (string method in endpoint.Methods)
            {
                using HttpResponseMessage response = await CallAsync(site.Admin, endpoint, method, token);
                if (IsDenialPage(endpoint) || IsDevelopmentOnly(endpoint))
                {
                    continue; // a página de "sem permissão" (403) e o catálogo de componentes (404 fora de Development) já têm o teste próprio acima
                }

                if (IsAnyDenial(response) || (int)response.StatusCode >= 500)
                {
                    wrong.Add($"{Describe(endpoint, access)} como Administrador: {(int)response.StatusCode} {response.Headers.Location}");
                }
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task EveryUnsafeEndpoint_RejectsARequestWithoutTheAntiforgeryToken()
    {
        using DraftSite site = await DraftSite.StartAsync();
        (Endpoint[] endpoints, _) = Discover(site.Harness.Factory.Services);
        await TokenAsync(site.Admin); // o cookie antiforgery do cliente
        List<string> wrong = [];

        foreach (Endpoint endpoint in endpoints.Where(e => e.Method is "POST" or "DELETE" or "PUT" or "PATCH"))
        {
            // O cliente do Administrador faz todas as chamadas sem o token: tem de dar 400 em toda ação que escreve (a entrada do painel, anônima, também)
            HttpClient client = Matrix[endpoint.Key] is Access.PanelAnonymous ? site.Harness.Anonymous() : site.Admin;
            if (endpoint.Key.Contains("painel/sair", StringComparison.Ordinal))
            {
                client = site.Harness.Anonymous();
            }

            using HttpResponseMessage response = await CallAsync(client, endpoint, endpoint.Method);
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                wrong.Add($"{endpoint.Key}: sem token respondeu {(int)response.StatusCode} {response.Headers.Location}");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }
}
