using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>Atalhos dos testes da revisão: anúncios Em revisão gravados direto no banco, segundo autor e telefone do site.</summary>
internal static class ReviewSupport
{
    public const string Ana = PanelFixture.WriterEmail;
    public const string Bruno = "bruno.lima@exemplo.com.br";
    public const string SecondAdmin = "carla.admin@exemplo.com.br";

    public static DateTime Day(int day) => new(2026, 9, day, 13, 0, 0, DateTimeKind.Utc);

    public static string PreviewUrl(int adId) => $"/painel/anuncios/{adId}/pre-visualizacao";

    public static async Task EnsureBrunoAsync(DraftSite site)
    {
        if ((await site.Harness.Factory.ListUsersAsync()).All(u => u.Email != Bruno))
        {
            await site.Harness.Factory.CreateUserAsync(Bruno, "Bruno Lima", PanelFixture.Password, RoleNames.Writer);
        }
    }

    /// <summary>Grava um anúncio Em revisão enviado em <paramref name="sentAt"/> (e, se pedido, antes rejeitado e reenviado).</summary>
    public static async Task<int> AddInReviewAsync(
        DraftSite site, string authorEmail, string title, int categoryId, DateTime sentAt, Action<Ad> configure = null)
    {
        await EnsureBrunoAsync(site);
        int author = await site.UserIdAsync(authorEmail);
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(categoryId);
        ad.SetText(title, "Descrição do anúncio");
        ad.SetPrice(6_200_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        configure?.Invoke(ad);
        ad.ApplyTransition(AdStatus.InReview, author, sentAt, null);
        return await site.Harness.WithDbAsync(async db =>
        {
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            return ad.Id;
        });
    }

    /// <summary>Um anúncio Em revisão que passa em todas as pendências (Livros e revistas, preço, CEP, condição) com <paramref name="photos"/> linhas de foto.</summary>
    public static async Task<int> AddPublishableAsync(DraftSite site, string title = "Livro de Direito Civil", int photos = 1, string authorEmail = Ana)
    {
        int adId = await AddInReviewAsync(site, authorEmail, title, 86, Day(29), ad =>
        {
            ad.SetPrice(5_000);
            ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
        });
        if (photos > 0)
        {
            await site.Harness.WithDbAsync(async db =>
            {
                for (int i = 0; i < photos; i++)
                {
                    db.AdPhotos.Add(new AdPhoto { AdId = adId, SortOrder = i, StorageKey = $"{adId}/{Guid.NewGuid():N}", Width = 100, Height = 80, SizeBytes = 3 });
                }

                await db.SaveChangesAsync();
                return 0;
            });
        }

        return adId;
    }

    public static async Task SetPhoneAsync(DraftSite site, string digits)
    {
        await site.Harness.WithDbAsync(async db =>
        {
            db.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = digits });
            await db.SaveChangesAsync();
            return 0;
        });
        site.Harness.Factory.Services.GetRequiredService<ISiteSettings>().Invalidate();
    }
}
