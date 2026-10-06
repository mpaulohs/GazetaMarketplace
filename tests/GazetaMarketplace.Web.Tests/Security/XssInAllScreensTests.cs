using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Detail;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// NFR-15: o que a pessoa digita nunca roda como código. Texto hostil (título, descrição, nome, motivo, categoria, cidade, busca) é gravado e aberto em <b>todas</b> as telas públicas e do
/// painel: em nenhuma resposta ele pode aparecer como marcação (<c>&lt;script&gt;</c>, atributo <c>on…=</c>, endereço <c>javascript:</c>, um segundo <c>&lt;/title&gt;</c>), e onde o texto é mostrado ele aparece
/// <b>como texto</b>. A API devolve o texto como dado, em JSON. O teste E2E (<c>XssE2ETests</c>) prova o mesmo no navegador de verdade.
/// </summary>
[TestClass]
public sealed class XssInAllScreensTests
{
    internal const string Marker = "<script>alert(1)</script>";

    /// <summary>109 caracteres que juntam os ataques da lista: tag de script, atributo que quebra o valor, fechar o título, aspas simples, e comercial e endereço javascript.</summary>
    internal const string Hostile = "<script>alert(1)</script>\"><img src=x onerror=alert(1)></title><script>alert(2)</script>'&javascript:alert(3)";

    /// <summary>O mesmo para nomes (até 100 caracteres).</summary>
    internal const string HostileName = "<script>alert(1)</script>\"><img src=x onerror=alert(1)>'&";

    private const int Cars = 33;

    private static readonly string[] Payloads =
    [
        "<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "'", "&", "</title><script>alert(1)</script>", "javascript:alert(1)"
    ];

    // Um leitor de marcação simples: acha cada tag e cada atributo de verdade. Texto codificado (&lt;img onerror=...) dentro de um valor de atributo não é atributo, e não pode dar falso alarme
    private static readonly Regex Tag = new(@"<([a-zA-Z][a-zA-Z0-9-]*)((?:\s+[^\s""'<>/=]+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s""'<>=`]+))?)*)\s*/?>", RegexOptions.CultureInvariant);
    private static readonly Regex Attribute = new(@"\s+([^\s""'<>/=]+)(?:\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'<>=`]+)))?", RegexOptions.CultureInvariant);
    private static readonly string[] UrlAttributes = ["href", "src", "action", "formaction", "data", "poster", "srcset", "xlink:href"];

    /// <summary>Confere uma resposta HTML: nada do ataque virou marcação; e, se <paramref name="mustShow"/>, o texto aparece (como texto) na página.</summary>
    internal static void AssertHarmless(List<string> wrong, string where, string html, bool mustShow)
    {
        if (html.Contains("<script>alert", StringComparison.OrdinalIgnoreCase) || html.Contains("<img src=x", StringComparison.OrdinalIgnoreCase))
        {
            wrong.Add($"{where}: o ataque saiu como marcação");
        }

        foreach (Match tag in Tag.Matches(html))
        {
            string name = tag.Groups[1].Value;
            bool hasSrc = false;
            string type = string.Empty;
            foreach (Match attribute in Attribute.Matches(tag.Groups[2].Value))
            {
                string attributeName = attribute.Groups[1].Value.ToLowerInvariant();
                string value = WebUtility.HtmlDecode(attribute.Groups[2].Success ? attribute.Groups[2].Value : attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[4].Value);
                hasSrc |= attributeName == "src";
                if (attributeName == "type")
                {
                    type = value;
                }

                if (attributeName.StartsWith("on", StringComparison.Ordinal))
                {
                    wrong.Add($"{where}: <{name}> tem o atributo {attributeName} (manipulador de evento)");
                }

                if (UrlAttributes.Contains(attributeName) && new string([.. value.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c))]).StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    wrong.Add($"{where}: <{name} {attributeName}=\"{value}\"> é um endereço javascript:");
                }
            }

            if (name.Equals("script", StringComparison.OrdinalIgnoreCase) && !hasSrc && type is not ("application/json" or "application/ld+json"))
            {
                wrong.Add($"{where}: há um <script> com código dentro da página");
            }
        }

        if (Regex.Matches(html, "</title>", RegexOptions.IgnoreCase).Count > 1)
        {
            wrong.Add($"{where}: mais de um </title> (o texto fechou o título)");
        }

        if (mustShow && !WebUtility.HtmlDecode(html).Contains(Marker, StringComparison.Ordinal))
        {
            wrong.Add($"{where}: o texto digitado não apareceu na página (a verificação não provaria nada)");
        }
    }

    private static async Task VisitAsync(List<string> wrong, HttpClient client, string url, bool mustShow, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using HttpResponseMessage response = await client.GetAsync(url);
        string html = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != expected)
        {
            wrong.Add($"{url}: respondeu {(int)response.StatusCode} (esperado {(int)expected})");
            return;
        }

        AssertHarmless(wrong, url, html, mustShow);
    }

    /// <summary>A página do anúncio manda um endereço fora do padrão para o endereço de verdade (301): abre os dois e confere o destino.</summary>
    private static async Task VisitAdAsync(List<string> wrong, HttpClient client, string url)
    {
        using HttpResponseMessage first = await client.GetAsync(url);
        if (first.StatusCode == HttpStatusCode.MovedPermanently)
        {
            string target = first.Headers.Location.IsAbsoluteUri ? first.Headers.Location.PathAndQuery : first.Headers.Location.OriginalString;
            if (target.Contains('<', StringComparison.Ordinal) || target.Contains('"', StringComparison.Ordinal))
            {
                wrong.Add($"{url}: o endereço canônico leva marcação: {target}");
            }

            await VisitAsync(wrong, client, target, mustShow: true);
            return;
        }

        await VisitAsync(wrong, client, url, mustShow: true);
    }

    private static string Slug(DraftSite site, int categoryId) => site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(default).GetAwaiter().GetResult().Find(categoryId).Slug;

    private static ShowcaseRow HostileRow(int id) => new(id, Hostile, Cars, 6_200_000, null, "Campinas<script>alert(9)</script>", "SP", null, null, null);

    // ---------- Telas públicas ----------

    [TestMethod]
    public async Task PublicScreens_ShowHostileTextAsText_AndNeverAsMarkup()
    {
        using DraftSite site = await DraftSite.StartAsync();
        IServiceProvider services = site.Harness.Factory.Services;
        services.GetRequiredService<StubShowcaseRepository>().Rows.AddRange([HostileRow(1), HostileRow(2)]);
        services.GetRequiredService<StubSearchReadRepository>().Rows.AddRange([HostileRow(1), HostileRow(2)]);
        int adId = await AdDetailTests.AddAsync(site, Hostile, Cars, configure: ad =>
        {
            ad.SetText(Hostile, Hostile + "\n" + Hostile);
            ad.SetLocation("13015100", "Campinas<script>alert(9)</script>", "SP", false);
        });

        // Categoria com nome hostil: aparece na vitrine inteira (menu, início, trilha)
        using HttpResponseMessage created = await site.Admin.PostFormAsync("/painel/categorias/nova", "/painel/categorias/nova", new Dictionary<string, string> { ["Name"] = HostileName, ["ParentId"] = string.Empty });
        string slug = Slug(site, Cars);
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        await VisitAsync(wrong, visitor, "/", mustShow: true);
        await VisitAsync(wrong, visitor, $"/categoria/{slug}", mustShow: true);
        await VisitAsync(wrong, visitor, $"/categoria/{slug}?pagina=2", mustShow: false);
        await VisitAsync(wrong, visitor, "/busca?categoria=" + slug, mustShow: true);
        await VisitAdAsync(wrong, visitor, $"/anuncio/{adId}/qualquer");
        await VisitAdAsync(wrong, visitor, $"/anuncio/{adId}/%3Cscript%3Ealert(1)%3C%2Fscript%3E");
        await VisitAsync(wrong, visitor, "/favoritos", mustShow: false);
        await VisitAsync(wrong, visitor, "/favoritos/lista?ids=1,2", mustShow: true);
        await VisitAsync(wrong, visitor, "/busca?cidade=" + Uri.EscapeDataString(Hostile) + "&uf=SP", mustShow: false);
        foreach (string payload in Payloads)
        {
            await VisitAsync(wrong, visitor, "/busca?q=" + Uri.EscapeDataString(payload), mustShow: false);
            await VisitAsync(wrong, visitor, "/busca?q=" + Uri.EscapeDataString(payload) + "&categoria=" + slug, mustShow: false);
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
        Assert.AreEqual(HttpStatusCode.Redirect, created.StatusCode, "a categoria de nome hostil foi aceita (o nome é só texto)");
    }

    [TestMethod]
    public async Task SearchText_IsShownBackAsText_InTheFieldTheTitleAndTheEmptyState()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        foreach (string payload in Payloads)
        {
            using HttpResponseMessage response = await visitor.GetAsync("/busca?q=" + Uri.EscapeDataString(payload));
            string html = await response.Content.ReadAsStringAsync();
            AssertHarmless(wrong, "/busca?q=" + payload, html, mustShow: false);
            if (payload.Contains('<', StringComparison.Ordinal) && !html.Contains("&lt;", StringComparison.Ordinal))
            {
                wrong.Add($"/busca?q={payload}: o texto da busca não voltou codificado");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task Api_ReturnsHostileTextAsJsonData_NotAsHtml()
    {
        using DraftSite site = await DraftSite.StartAsync();
        site.Harness.Factory.Services.GetRequiredService<StubShowcaseRepository>().Rows.Add(HostileRow(7));
        using HttpClient visitor = site.Harness.Anonymous();

        using HttpResponseMessage response = await visitor.GetAsync("/api/v1/ads?ids=7");
        string raw = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument json = JsonDocument.Parse(raw);
        Assert.IsTrue(ContainsString(json.RootElement, Hostile), "o título volta inteiro, como dado");
        Assert.AreEqual("nosniff", response.Headers.TryGetValues("X-Content-Type-Options", out IEnumerable<string> values) ? values.Single() : null, "o navegador não adivinha o tipo da resposta");
    }

    private static bool ContainsString(JsonElement element, string value) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() == value,
        JsonValueKind.Array => element.EnumerateArray().Any(e => ContainsString(e, value)),
        JsonValueKind.Object => element.EnumerateObject().Any(p => ContainsString(p.Value, value)),
        _ => false
    };

    [TestMethod]
    public async Task Sitemap_WithHostileTitles_IsStillWellFormedXml_WithoutMarkupFromTheText()
    {
        using DraftSite site = await DraftSite.StartAsync();
        site.Harness.Factory.Services.GetRequiredService<StubSitemapReadRepository>().Ads.Add(new SitemapAd(5, Hostile, new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)));
        using HttpClient visitor = site.Harness.Anonymous();

        string xml = await visitor.GetStringAsync("/sitemap.xml");

        XDocument document = XDocument.Parse(xml); // XML malformado lançaria aqui
        Assert.IsFalse(xml.Contains("<script", StringComparison.OrdinalIgnoreCase));
        string[] locations = [.. document.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value)];
        Assert.IsTrue(locations.Any(l => l.Contains("/anuncio/5/", StringComparison.Ordinal)), "o anúncio está no mapa");
        Assert.IsTrue(locations.All(l => l.All(c => !char.IsWhiteSpace(c) && c is not ('<' or '>' or '"' or '\'' or '&'))), "o endereço do anúncio é feito só de letras, números e hífens");
    }

    // ---------- Telas do painel ----------

    [TestMethod]
    public async Task PanelScreens_ShowHostileTextAsText_AndNeverAsMarkup()
    {
        using DraftSite site = await DraftSite.StartAsync();
        IServiceProvider services = site.Harness.Factory.Services;
        string hostileWriter = "redatora.hostil@exemplo.com.br";
        string hostileAdmin = "admin.hostil@exemplo.com.br";
        await site.Harness.Factory.CreateUserAsync(hostileWriter, HostileName, PanelFixture.Password, RoleNames.Writer);
        await site.Harness.Factory.CreateUserAsync(hostileAdmin, HostileName, PanelFixture.Password, RoleNames.Administrator);
        using HttpClient writer = await PanelFixture.SignedInAsync(site.Harness.Factory, hostileWriter);
        using HttpClient admin = await PanelFixture.SignedInAsync(site.Harness.Factory, hostileAdmin);

        void Hostilize(Ad ad) => ad.SetText(Hostile, Hostile + "\n" + Hostile);
        // Rascunho completo (passa em todas as pendências), para a tela de confirmar o envio abrir de verdade
        int draft = await AddCompleteDraftAsync(site, hostileWriter);
        int inReview = await AddPublishableAsync(site, Hostile, 1, hostileWriter);
        int toReject = await AddInReviewAsync(site, hostileWriter, Hostile, Cars, Day(21), Hostilize);
        int published = await AdDetailTests.AddAsync(site, Hostile, Cars, configure: Hostilize);
        int photoId = await site.Harness.WithDbAsync(async db =>
        {
            AdPhoto photo = new() { AdId = draft, SortOrder = 0, StorageKey = $"{draft}/{Guid.NewGuid():N}", Width = 100, Height = 80, SizeBytes = 3 };
            db.AdPhotos.Add(photo);
            await db.SaveChangesAsync();
            return photo.Id;
        });
        int hostileUserId = await site.UserIdAsync(hostileWriter);
        await SetPhoneAsync(site, "19987654321"); // sem o telefone do site a confirmação de publicar manda de volta para a pré-visualização

        // A recusa, com o motivo digitado pelo Administrador, mostrada depois para a autora
        using HttpResponseMessage rejected = await admin.PostFormAsync($"/painel/anuncios/{toReject}/rejeitar", $"/painel/anuncios/{toReject}/rejeitar", new Dictionary<string, string> { ["Reason"] = Hostile });

        // O que a lista do painel mostra: título, autor, categoria e motivo
        services.GetRequiredService<StubPanelAdListRepository>().Rows.Add(new PanelAdListRow(toReject, Hostile, hostileUserId, HostileName, Cars, AdStatus.Rejected, Day(22), Hostile));
        services.GetRequiredService<StubPanelAdListRepository>().InReviewCount = 1;
        List<string> wrong = [];

        // Redator (a conta tem o nome hostil: a barra de cima mostra o nome em toda tela)
        await VisitAsync(wrong, writer, "/painel/anuncios", mustShow: true);
        await VisitAsync(wrong, writer, $"/painel/anuncios/{draft}/editar", mustShow: true);
        await VisitAsync(wrong, writer, $"/painel/anuncios/{draft}/enviar/confirmar", mustShow: true);
        await VisitAsync(wrong, writer, $"/painel/anuncios/{draft}/fotos/{photoId}/remover", mustShow: true);
        await VisitAsync(wrong, writer, $"/painel/anuncios/{toReject}/editar", mustShow: true);
        await VisitAsync(wrong, writer, "/painel/anuncios/novo", mustShow: true);

        // Administrador
        await VisitAsync(wrong, admin, "/painel/anuncios", mustShow: true);
        await VisitAsync(wrong, admin, "/painel/anuncios/fila", mustShow: true);
        await VisitAsync(wrong, admin, PreviewUrl(inReview), mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/anuncios/{inReview}/publicar", mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/anuncios/{inReview}/rejeitar", mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/anuncios/{published}/despublicar", mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/anuncios/{published}/arquivar", mustShow: true);
        await VisitAsync(wrong, admin, "/painel/usuarios", mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/usuarios/{hostileUserId}/editar", mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/usuarios/{hostileUserId}/desativar", mustShow: true);
        await VisitAsync(wrong, admin, $"/painel/usuarios/{hostileUserId}/redefinir-senha", mustShow: true);
        await VisitAsync(wrong, admin, "/painel/configuracoes", mustShow: true);
        await VisitAsync(wrong, admin, "/painel/categorias", mustShow: true);

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
        Assert.AreEqual(HttpStatusCode.Redirect, rejected.StatusCode, "o motivo hostil foi aceito (é só texto)");
    }

    private static async Task<int> AddCompleteDraftAsync(DraftSite site, string authorEmail)
    {
        int author = await site.UserIdAsync(authorEmail);
        Ad ad = Ad.CreateDraft(Hostile, author);
        ad.SetCategory(86);
        ad.SetText(Hostile, Hostile + "\n" + Hostile);
        ad.SetPrice(5_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
        return await site.Harness.WithDbAsync(async db =>
        {
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            return ad.Id;
        });
    }

    [TestMethod]
    public async Task PanelForms_EchoHostileInputBackAsText_OnValidationErrors()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        // Entrada com e-mail hostil: a página devolve o que foi digitado, codificado
        using HttpResponseMessage signIn = await visitor.SignInAsync(Hostile, "Senha-qualquer-1!");
        AssertHarmless(wrong, "POST /painel/entrar", await signIn.Content.ReadAsStringAsync(), mustShow: false);

        // Nova pessoa da equipe com nome e e-mail hostis
        using HttpResponseMessage user = await site.Admin.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", new Dictionary<string, string>
        {
            ["FullName"] = Hostile,
            ["Email"] = Hostile,
            ["Role"] = RoleNames.Writer,
            ["ProvisionalPassword"] = "x"
        });
        string userHtml = await user.Content.ReadAsStringAsync();
        AssertHarmless(wrong, "POST /painel/usuarios/novo", userHtml, mustShow: false);

        // Nova categoria com nome hostil, repetida: o erro de "já existe" cita o nome
        using HttpResponseMessage first = await site.Admin.PostFormAsync("/painel/categorias/nova", "/painel/categorias/nova", new Dictionary<string, string> { ["Name"] = HostileName, ["ParentId"] = string.Empty });
        using HttpResponseMessage second = await site.Admin.PostFormAsync("/painel/categorias/nova", "/painel/categorias/nova", new Dictionary<string, string> { ["Name"] = HostileName, ["ParentId"] = string.Empty });
        AssertHarmless(wrong, "POST /painel/categorias/nova (repetida)", await second.Content.ReadAsStringAsync(), mustShow: false);

        // Anúncio novo com título hostil: a página volta com o texto digitado
        using HttpResponseMessage ad = await site.Writer.PostFormAsync("/painel/anuncios/novo", "/painel/anuncios/novo", new Dictionary<string, string> { ["Title"] = Hostile, ["Description"] = Hostile, ["CategoryId"] = "999999" });
        AssertHarmless(wrong, "POST /painel/anuncios/novo", await ad.Content.ReadAsStringAsync(), mustShow: false);

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
        Assert.AreEqual(HttpStatusCode.Redirect, first.StatusCode);
    }
}
