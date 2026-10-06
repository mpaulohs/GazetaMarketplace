using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// RC-10 e NFR-06 (rules/security.md): login, "esqueci minha senha" e "redefinir senha" aceitam 5 pedidos por 15 minutos por IP. A política <c>auth</c> existia e nenhuma ação a usava
/// (o teste antigo a provava numa rota de teste); aqui o limite é provado nas rotas reais do painel.
/// </summary>
[TestClass]
public sealed class AuthRateLimitTests
{
    private const string Email = "ana@exemplo.com.br";
    private const string Wrong = "Senha@Errada9";

    // "0" = valor ausente: vale o padrão de produção (5); o host de teste usa um limite alto para os outros testes
    private static readonly Dictionary<string, string> ProductionDefault = new() { ["RateLimiting:AuthPermits"] = "0" };

    private static async Task<WebFactory> NewFactoryAsync(Dictionary<string, string> configuration = null)
    {
        WebFactory factory = new(configuration: configuration ?? ProductionDefault, withDatabase: true);
        await factory.CreateUserAsync(Email, "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        return factory;
    }

    private static Task<HttpResponseMessage> ForgotAsync(HttpClient client, string ip)
        => client.PostFormWithIpAsync("/painel/esqueci-minha-senha", "/painel/esqueci-minha-senha", new Dictionary<string, string> { ["Email"] = Email }, ip);

    private static Task<HttpResponseMessage> ResetAsync(HttpClient client, string ip)
        => client.PostFormWithIpAsync("/painel/esqueci-minha-senha", "/painel/redefinir-senha", new Dictionary<string, string> { ["UserId"] = "1", ["Token"] = "x", ["NewPassword"] = "Senha@Forte2", ["ConfirmPassword"] = "Senha@Forte2" }, ip);

    [TestMethod]
    public async Task SignIn_SixthPostFromTheSameIp_Is429_AndAnotherIpIsNotAffected()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.1")).StatusCode, "tentativa " + i);
        }

        HttpResponseMessage sixth = await client.SignInAsync(Email, Wrong, ip: "198.51.100.1");
        HttpResponseMessage other = await client.SignInAsync(Email, Wrong, ip: "198.51.100.2");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        Assert.IsTrue(sixth.Headers.Contains("Retry-After"));
        Assert.AreNotEqual(HttpStatusCode.TooManyRequests, other.StatusCode, "o limite é por IP");
    }

    [TestMethod]
    public async Task ForgotPassword_SixthPostFromTheSameIp_Is429()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await ForgotAsync(client, "198.51.100.3")).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await ForgotAsync(client, "198.51.100.3")).StatusCode);
    }

    [TestMethod]
    public async Task ResetPassword_SixthPostFromTheSameIp_Is429()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await ResetAsync(client, "198.51.100.4")).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await ResetAsync(client, "198.51.100.4")).StatusCode);
    }

    [TestMethod]
    public async Task TheThreeActions_ShareOneBudgetPerIp_AndOpeningThePagesIsNeverCounted()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 10; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/entrar")).StatusCode, "abrir a tela não conta");
        }

        await client.SignInAsync(Email, Wrong, ip: "198.51.100.5");
        await client.SignInAsync(Email, Wrong, ip: "198.51.100.5");
        await ForgotAsync(client, "198.51.100.5");
        await ForgotAsync(client, "198.51.100.5");
        await ResetAsync(client, "198.51.100.5");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await ForgotAsync(client, "198.51.100.5")).StatusCode, "o sexto pedido, de qualquer das três ações, é recusado");
    }

    [TestMethod]
    public async Task TheLimit_CanBeRaisedByConfiguration_ForTheE2ESite()
    {
        using WebFactory factory = await NewFactoryAsync(new Dictionary<string, string> { ["RateLimiting:AuthPermits"] = "50" });
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 12; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await ForgotAsync(client, "198.51.100.6")).StatusCode, "pedido " + i);
        }
    }
}
