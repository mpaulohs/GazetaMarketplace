using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Panel;

/// <summary>
/// A matriz de permissões da Fase 4 (NFR-13, autorização no servidor): todo endereço de lista, fila, pré-visualização, publicar, rejeitar, despublicar e arquivar × {sem login, Redator,
/// Administrador}, nos dois métodos. Uma tabela só: o que mudar numa política sem querer aparece aqui, não numa tela esquecida.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PermissionMatrixTests
#pragma warning restore CA1515
{
    private const string TokenPage = "/painel/anuncios/novo";

    private enum Who { Anonymous, Writer, Admin }

    private enum Expect { SignIn, Denied, Ok, Success }

    private sealed record Route(string Name, string Path, bool AdminOnly, bool ListedForWriter = false);

    private static string Token(string html) => Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

    private static HttpClient Client(DraftSite site, Who who, out bool dispose)
    {
        dispose = who == Who.Anonymous;
        return who switch { Who.Anonymous => site.Harness.Anonymous(), Who.Writer => site.Writer, _ => site.Admin };
    }

    private static void Check(HttpResponseMessage response, Expect expected, string label)
    {
        switch (expected)
        {
            case Expect.SignIn:
                Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, label);
                StringAssert.StartsWith(response.Destination(), "/painel/entrar", label);
                break;
            case Expect.Denied:
                Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, label);
                StringAssert.StartsWith(response.Destination(), "/painel/acesso-negado", label);
                break;
            case Expect.Ok:
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, label);
                break;
            default:
                Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, label);
                Assert.IsFalse(response.Destination().StartsWith("/painel/acesso-negado", System.StringComparison.Ordinal) || response.Destination().StartsWith("/painel/entrar", System.StringComparison.Ordinal), label + " → " + response.Destination());
                break;
        }
    }

    [TestMethod]
    public async Task Get_TodosOsEnderecosDaFase4_PorPapel()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678"); // sem o telefone do site a confirmação de publicar volta para a pré-visualização (US-010-S08)
        int inReview = await AddPublishableAsync(site, "Para decidir");
        int published = await site.AddAdAsync(Ana, AdStatus.Published, "Para retirar");
        Route[] routes =
        [
            new("lista", "/painel/anuncios", AdminOnly: false),
            new("fila", "/painel/anuncios/fila", AdminOnly: true),
            new("pré-visualização", $"/painel/anuncios/{inReview}/pre-visualizacao", AdminOnly: true),
            new("confirmar publicar", $"/painel/anuncios/{inReview}/publicar", AdminOnly: true),
            new("motivo da rejeição", $"/painel/anuncios/{inReview}/rejeitar", AdminOnly: true),
            new("confirmar despublicar", $"/painel/anuncios/{published}/despublicar", AdminOnly: true),
            new("confirmar arquivar", $"/painel/anuncios/{published}/arquivar", AdminOnly: true)
        ];

        foreach (Route route in routes)
        {
            foreach (Who who in new[] { Who.Anonymous, Who.Writer, Who.Admin })
            {
                HttpClient client = Client(site, who, out bool dispose);
                try
                {
                    Expect expected = who switch
                    {
                        Who.Anonymous => Expect.SignIn,
                        Who.Writer => route.AdminOnly ? Expect.Denied : Expect.Ok,
                        _ => Expect.Ok
                    };
                    Check(await client.GetAsync(route.Path), expected, $"GET {route.Name} como {who}");
                }
                finally
                {
                    if (dispose)
                    {
                        client.Dispose();
                    }
                }
            }
        }

        // O GET só mostra a pergunta: nada mudou depois de todos esses pedidos
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(inReview)).Status);
        Assert.AreEqual(AdStatus.Published, (await site.LoadAsync(published)).Status);
        Assert.IsEmpty((await site.AuditAsync()).Where(e => e.Action is "ad.publish" or "ad.reject" or "ad.unpublish" or "ad.archive").ToList());
    }

    [TestMethod]
    public async Task Post_PublicarRejeitarDespublicarArquivar_PorPapel_SemTokenE400_ComTokenSoOAdministradorGrava()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        string writerToken = Token(await site.Writer.GetStringAsync(TokenPage));
        string adminToken = Token(await site.Admin.GetStringAsync(TokenPage));
        (string Action, byte Status, string Done)[] actions =
        [
            ("publicar", AdStatus.InReview, "ad.publish"),
            ("rejeitar", AdStatus.InReview, "ad.reject"),
            ("despublicar", AdStatus.Published, "ad.unpublish"),
            ("arquivar", AdStatus.Published, "ad.archive")
        ];

        foreach ((string action, byte status, string done) in actions)
        {
            int id = status == AdStatus.InReview ? await AddPublishableAsync(site, "Matriz " + action) : await site.AddAdAsync(Ana, status, "Matriz " + action);
            string path = $"/painel/anuncios/{id}/{action}";
            Dictionary<string, string> withReason = new() { ["reason"] = "Fotos escuras" };

            // Sem login: vai para a entrada (antes de qualquer conferência de token)
            using (HttpClient anonymous = site.Harness.Anonymous())
            {
                using FormUrlEncodedContent anonymousBody = new(withReason);
                Check(await anonymous.PostAsync(path, anonymousBody), Expect.SignIn, $"POST {action} sem login");
            }

            // Redator com token válido: acesso negado
            using (FormUrlEncodedContent writerBody = new(new Dictionary<string, string>(withReason) { ["__RequestVerificationToken"] = writerToken }))
            {
                Check(await site.Writer.PostAsync(path, writerBody), Expect.Denied, $"POST {action} como Redator");
            }

            // Administrador sem token: 400; nada muda
            using (FormUrlEncodedContent noToken = new(withReason))
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, (await site.Admin.PostAsync(path, noToken)).StatusCode, $"POST {action} sem token");
            }

            Assert.AreEqual(status, (await site.LoadAsync(id)).Status, $"{action}: nada mudou até aqui");
            Assert.IsEmpty((await site.AuditAsync()).Where(e => e.Action == done && e.TargetId == id.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList(), $"{action}: sem auditoria até aqui");

            // Administrador com token: grava
            using (FormUrlEncodedContent adminBody = new(new Dictionary<string, string>(withReason) { ["__RequestVerificationToken"] = adminToken }))
            {
                Check(await site.Admin.PostAsync(path, adminBody), Expect.Success, $"POST {action} como Administrador");
            }

            Assert.AreNotEqual(status, (await site.LoadAsync(id)).Status, $"{action}: o Administrador grava");
            Assert.HasCount(1, (await site.AuditAsync()).Where(e => e.Action == done && e.TargetId == id.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList(), $"{action}: uma auditoria");
        }
    }
}
