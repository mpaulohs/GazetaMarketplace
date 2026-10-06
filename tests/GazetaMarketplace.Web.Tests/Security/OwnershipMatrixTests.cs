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
using GazetaMarketplace.Web.Tests.Photos;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// NFR-13, o outro lado da matriz de acesso: um Redator só mexe nos anúncios dele. O Redator B chama toda rota do anúncio da Redatora A (edição, fotos, envio) e a resposta tem de ser
/// negada, sem revelar o conteúdo do anúncio e sem mudar nada no banco. O Administrador, ao contrário, alcança o anúncio de qualquer pessoa.
/// </summary>
[TestClass]
public sealed class OwnershipMatrixTests
{
    private const string SecretTitle = "Rascunho secreto da Ana 4821";

    private sealed record Route(string Method, string Url, bool Multipart = false);

    private static Route[] RoutesOf(int adId, int photoId) =>
    [
        new("GET", $"/painel/anuncios/{adId}/editar"),
        new("POST", $"/painel/anuncios/{adId}/editar"),
        new("POST", $"/painel/anuncios/{adId}/atualizar"),
        new("POST", $"/painel/anuncios/{adId}/enviar"),
        new("GET", $"/painel/anuncios/{adId}/enviar/confirmar"),
        new("POST", $"/painel/anuncios/{adId}/enviar/confirmar"),
        new("POST", $"/painel/anuncios/{adId}/fotos", true),
        new("POST", $"/painel/anuncios/{adId}/fotos/{photoId}/capa"),
        new("GET", $"/painel/anuncios/{adId}/fotos/{photoId}/remover"),
        new("POST", $"/painel/anuncios/{adId}/fotos/{photoId}/remover"),
        new("POST", $"/api/v1/ads/{adId}/photos", true),
        new("DELETE", $"/api/v1/ads/{adId}/photos/{photoId}"),
        new("POST", $"/api/v1/ads/{adId}/photos/{photoId}/cover")
    ];

    private static async Task<string> TokenAsync(HttpClient client)
    {
        string page = await client.GetStringAsync("/painel/anuncios");
        return Regex.Match(page, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;
    }

    private static HttpRequestMessage Build(Route route, string token)
    {
        HttpRequestMessage request = new(new HttpMethod(route.Method), route.Url);
        if (route.Method != "GET")
        {
            request.Headers.Add("RequestVerificationToken", token);
        }

        if (route.Method == "POST")
        {
            // O envio leva uma foto de verdade: sem arquivo a validação responde antes de olhar quem é o autor, e o teste não provaria a posse
            request.Content = route.Multipart
                ? new MultipartFormDataContent { { new ByteArrayContent(PhotoApi.Jpeg()), "file", "foto.jpg" } }
                : new FormUrlEncodedContent(new Dictionary<string, string> { ["title"] = "Trocado pelo Bruno", ["description"] = "invadido" });
        }

        return request;
    }

    private static async Task<(int AdId, int PhotoId)> AnasDraftAsync(DraftSite site)
    {
        int author = await site.UserIdAsync(Ana);
        Ad ad = Ad.CreateDraft(SecretTitle, author);
        ad.SetCategory(86);
        ad.SetText(SecretTitle, "Descrição que só a Ana pode ver");
        ad.SetPrice(5_000);
        return await site.Harness.WithDbAsync(async db =>
        {
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            AdPhoto photo = new() { AdId = ad.Id, SortOrder = 0, StorageKey = $"{ad.Id}/{Guid.NewGuid():N}", Width = 100, Height = 80, SizeBytes = 3 };
            db.AdPhotos.Add(photo);
            await db.SaveChangesAsync();
            return (ad.Id, photo.Id);
        });
    }

    [TestMethod]
    public async Task AnotherWriter_IsDeniedEveryRouteOfAnAdThatIsNotTheirs_WithoutSeeingItOrChangingIt()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await EnsureBrunoAsync(site);
        (int adId, int photoId) = await AnasDraftAsync(site);
        using HttpClient bruno = await PanelFixture.SignedInAsync(site.Harness.Factory, Bruno);
        string token = await TokenAsync(bruno);
        List<string> wrong = [];

        foreach (Route route in RoutesOf(adId, photoId))
        {
            using HttpRequestMessage request = Build(route, token);
            using HttpResponseMessage response = await bruno.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.NotFound))
            {
                wrong.Add($"{route.Method} {route.Url}: o Redator B recebeu {(int)response.StatusCode} {response.Headers.Location}");
            }

            if (body.Contains(SecretTitle, StringComparison.Ordinal) || body.Contains("Descrição que só a Ana", StringComparison.Ordinal))
            {
                wrong.Add($"{route.Method} {route.Url}: a resposta negada revelou o conteúdo do anúncio");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));

        // Nada mudou no banco: o anúncio continua Rascunho, com o mesmo título e a mesma foto
        await site.Harness.WithDbAsync(async db =>
        {
            Ad ad = await db.Ads.AsNoTracking().SingleAsync(a => a.Id == adId);
            Assert.AreEqual(AdStatus.Draft, ad.Status);
            Assert.AreEqual(SecretTitle, ad.Title);
            Assert.AreEqual("Descrição que só a Ana pode ver", ad.Description);
            Assert.AreEqual(1, await db.AdPhotos.CountAsync(p => p.AdId == adId));
            Assert.IsTrue(await db.AdPhotos.AnyAsync(p => p.Id == photoId && p.SortOrder == 0));
            return 0;
        });
    }

    [TestMethod]
    public async Task TheAuthor_AndTheAdministrator_ReachTheAdThatIsOwnedByTheAuthor()
    {
        using DraftSite site = await DraftSite.StartAsync();
        (int adId, _) = await AnasDraftAsync(site);

        using HttpResponseMessage author = await site.Writer.GetAsync($"/painel/anuncios/{adId}/editar");
        using HttpResponseMessage admin = await site.Admin.GetAsync($"/painel/anuncios/{adId}/editar");

        Assert.AreEqual(HttpStatusCode.OK, author.StatusCode, "a autora edita o próprio anúncio");
        Assert.AreEqual(HttpStatusCode.OK, admin.StatusCode, "o Administrador alcança o anúncio de qualquer pessoa");
        StringAssert.Contains(await author.Content.ReadAsStringAsync(), SecretTitle);
    }
}
