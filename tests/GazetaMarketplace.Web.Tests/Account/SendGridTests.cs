using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Email;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>Envio pela API v3 do SendGrid (ADR-009) e o que cada ambiente pode ou não escrever no log.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SendGridTests
#pragma warning restore CA1515
{
    private const string ApiKey = "SG.chave-secreta-de-teste-123";
    private const string Link = "https://gazeta.exemplo.com.br/painel/redefinir-senha?id=7&code=ABCDEFGH-segredo_TOKEN";

    private static readonly EmailMessage Message = new(
        "ana@exemplo.com.br", "Redefinição de senha — GazetaMarketplace", "Texto: " + Link, "<p><a href=\"" + Link + "\">Redefinir</a></p>");

    private static SendGridEmailSender NewSender(StubSendGridHandler handler, ListLogger<SendGridEmailSender> log) =>
        new(new HttpClient(handler),
            Options.Create(new SendGridOptions { ApiKey = ApiKey, FromEmail = "noreply@gazeta.exemplo.com.br" }),
            log);

    [TestMethod]
    public async Task EnviaPost_ParaApiV3_ComBearer_SemVazarChaveNoLog()
    {
        StubSendGridHandler handler = new();
        ListLogger<SendGridEmailSender> log = new();

        await NewSender(handler, log).SendAsync(Message, CancellationToken.None);

        StubSendGridHandler.CapturedRequest request = handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://api.sendgrid.com/v3/mail/send", request.Uri.ToString());
        Assert.AreEqual("Bearer " + ApiKey, request.Authorization);
        Assert.AreEqual("application/json", request.ContentType);

        using JsonDocument json = JsonDocument.Parse(request.Body);
        JsonElement root = json.RootElement;
        Assert.AreEqual("ana@exemplo.com.br", root.GetProperty("personalizations")[0].GetProperty("to")[0].GetProperty("email").GetString());
        Assert.AreEqual("noreply@gazeta.exemplo.com.br", root.GetProperty("from").GetProperty("email").GetString());
        Assert.AreEqual(Message.Subject, root.GetProperty("subject").GetString());
        JsonElement content = root.GetProperty("content");
        Assert.AreEqual("text/plain", content[0].GetProperty("type").GetString());
        Assert.AreEqual("text/html", content[1].GetProperty("type").GetString());
        StringAssert.Contains(content[0].GetProperty("value").GetString(), Link);

        // O log leva só o destinatário: nunca a chave, o link ou o corpo
        StringAssert.Contains(log.All, "ana@exemplo.com.br");
        Assert.DoesNotContain(ApiKey, log.All);
        Assert.DoesNotContain("redefinir-senha", log.All);
        Assert.DoesNotContain("TOKEN", log.All);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task RespostaDeErro_LancaExcecaoSoComOCodigoHttp(HttpStatusCode status)
    {
        StubSendGridHandler handler = new() { Status = status };

        EmailDeliveryException error = await Assert.ThrowsExactlyAsync<EmailDeliveryException>(
            () => NewSender(handler, new ListLogger<SendGridEmailSender>()).SendAsync(Message, CancellationToken.None));

        StringAssert.Contains(error.Message, ((int)status).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.DoesNotContain(ApiKey, error.ToString());
        Assert.DoesNotContain("ana@exemplo.com.br", error.ToString());
        Assert.DoesNotContain("redefinir-senha", error.ToString());
    }

    [TestMethod]
    public async Task RedeForaDoAr_LancaEmailDeliveryException()
    {
        StubSendGridHandler handler = new() { Fail = new HttpRequestException("sem rede") };

        await Assert.ThrowsExactlyAsync<EmailDeliveryException>(
            () => NewSender(handler, new ListLogger<SendGridEmailSender>()).SendAsync(Message, CancellationToken.None));
    }

    [TestMethod]
    public async Task FalhaDoSendGrid_RespostaContinuaNeutra_EOErroVaiParaOLogComOTraceId()
    {
        StubSendGridHandler handler = new() { Status = HttpStatusCode.InternalServerError };
        using RecoveryHarness harness = new(RecoveryProduction(), "Production", handler);
        await harness.Factory.CreateUserAsync("ana@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);

        HttpResponseMessage response = await harness.RequestAsync("ana@exemplo.com.br");
        await harness.WaitForSendingAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.TextAsync(), RecoveryHarness.Neutral);
        Serilog.Events.LogEvent error = harness.Factory.Logs.Events.Single(e => e.Level == Serilog.Events.LogEventLevel.Error && CollectorSub(e).Contains("Falha ao enviar", StringComparison.Ordinal));
        string traceId = response.Headers.GetValues("X-Correlation-ID").Single();
        StringAssert.Contains(CollectorSub(error), traceId, "a linha do log leva o traceId do pedido");
        StringAssert.Contains(CollectorSub(error), "a***@exemplo.com.br");
        string all = string.Join("\n", harness.Factory.Logs.Events.Select(CollectorSink.AllAsText));
        Assert.DoesNotContain("redefinir-senha", all);
        Assert.DoesNotContain("chave-de-teste", all);
    }

    [TestMethod]
    public async Task EmProduction_OLogTemSoODestinatarioMascarado_SemOLink()
    {
        StubSendGridHandler handler = new();
        using RecoveryHarness harness = new(RecoveryProduction(), "Production", handler);
        await harness.Factory.CreateUserAsync("ana@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);

        await harness.RequestAsync("ana@exemplo.com.br");
        await harness.WaitForSendingAsync();

        // O e-mail saiu de verdade para o (falso) SendGrid, com o link
        StubSendGridHandler.CapturedRequest sent = handler.Requests.Single();
        StringAssert.Contains(sent.Body, "https://gazeta.exemplo.com.br/painel/redefinir-senha?id=");

        // ...mas o log de Production não tem o link, nem o caminho dele, nem o token
        string all = string.Join("\n", harness.Factory.Logs.Events.Select(CollectorSink.AllAsText));
        StringAssert.Contains(all, "E-mail enviado para \"a***@exemplo.com.br\"");
        Assert.DoesNotContain("redefinir-senha", all);
        Assert.DoesNotContain("code=", all);
        Assert.DoesNotContain("gazeta.exemplo.com.br", all);
    }

    [TestMethod]
    public async Task EmDevelopment_OEmailInteiroVaiParaOConsole_ComOLink()
    {
        System.IO.StringWriter console = new();
        ListLogger<LogEmailSender> log = new();

        await new LogEmailSender(log, new StubEnvironment("Development"), console).SendAsync(Message, CancellationToken.None);

        StringAssert.Contains(console.ToString(), Link, "em desenvolvimento a pessoa lê o link");
        Assert.DoesNotContain("redefinir-senha", log.All, "o log, mesmo em desenvolvimento, não leva o link");
        StringAssert.Contains(log.All, "ana@exemplo.com.br");
    }

    [TestMethod]
    [DataRow("Production")]
    [DataRow("Staging")]
    [DataRow("Testing")]
    public async Task ForaDeDevelopment_ORemetenteDeConsoleNaoMostraOLink(string environment)
    {
        System.IO.StringWriter console = new();
        ListLogger<LogEmailSender> log = new();

        await new LogEmailSender(log, new StubEnvironment(environment), console).SendAsync(Message, CancellationToken.None);

        Assert.AreEqual(string.Empty, console.ToString());
        Assert.DoesNotContain("redefinir-senha", log.All);
        Assert.DoesNotContain(Link, log.All);
    }

    [TestMethod]
    public void EmProduction_ORemetenteEOSendGrid_NosDemaisAmbientesEOConsole()
    {
        using WebFactory production = new("Production", RecoveryProduction(), withDatabase: true);
        using WebFactory testing = new("Testing", withDatabase: true);

        Assert.IsInstanceOfType<SendGridEmailSender>(production.Services.GetService(typeof(IEmailSender)));
        Assert.IsInstanceOfType<LogEmailSender>(testing.Services.GetService(typeof(IEmailSender)));
    }

    internal static System.Collections.Generic.Dictionary<string, string> RecoveryProduction()
    {
        System.Collections.Generic.Dictionary<string, string> configuration = WebFactory.ProductionConfiguration();
        configuration["Site:BaseUrl"] = "https://gazeta.exemplo.com.br";
        configuration["DataProtection:KeysDirectory"] = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gazeta-chaves-" + Guid.NewGuid().ToString("N"));
        return configuration;
    }

    private static string CollectorSub(Serilog.Events.LogEvent e) => CollectorSink.AllAsText(e);
}
