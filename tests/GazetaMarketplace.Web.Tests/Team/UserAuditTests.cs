using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Team;

/// <summary>RC-16: criar, mudar papel, desativar, reativar e redefinir senha gravam ator, ação, alvo e resultado em <c>AuditEntries</c>.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class UserAuditTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "marcos@exemplo.com.br";
    private const string AnaEmail = "ana.souza@exemplo.com.br";
    private const string Password = "Senha@Forte1";
    private const string Provisional = "Provis0ria!";

    private static async Task<(WebFactory Factory, HttpClient Admin, AppUser AdminUser, AppUser Ana)> StartAsync()
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, RoleNames.Administrator);
        await factory.CreateUserAsync(AnaEmail, "Ana Souza", Password, RoleNames.Writer);
        HttpClient admin = TeamClient.Create(factory);
        await admin.SignInAsync(AdminEmail, Password);
        IReadOnlyList<AppUser> users = await factory.ListUsersAsync();
        return (factory, admin, users.Single(u => u.Email == AdminEmail), users.Single(u => u.Email == AnaEmail));
    }

    private static async Task<List<AuditEntry>> EntriesAsync(WebFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditEntries.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
    }

    [TestMethod]
    public async Task CriarMudarPapelDesativarReativarRedefinir_GravamAuditoria()
    {
        (WebFactory factory, HttpClient admin, AppUser adminUser, AppUser ana) = await StartAsync();
        using (factory)
        using (admin)
        {
            await admin.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", new Dictionary<string, string>
            {
                ["FullName"] = "Bruno Lima",
                ["Email"] = "bruno@exemplo.com.br",
                ["Role"] = RoleNames.Writer,
                ["ProvisionalPassword"] = Provisional
            });
            await admin.PostFormAsync($"/painel/usuarios/{ana.Id}/editar", $"/painel/usuarios/{ana.Id}/editar", new Dictionary<string, string> { ["Role"] = RoleNames.Administrator });
            await admin.PostFormAsync($"/painel/usuarios/{ana.Id}/desativar", $"/painel/usuarios/{ana.Id}/desativar");
            await admin.PostFormAsync("/painel/usuarios", $"/painel/usuarios/{ana.Id}/reativar");
            await admin.PostFormAsync($"/painel/usuarios/{ana.Id}/redefinir-senha", $"/painel/usuarios/{ana.Id}/redefinir-senha", new Dictionary<string, string> { ["ProvisionalPassword"] = Provisional });

            AppUser bruno = (await factory.ListUsersAsync()).Single(u => u.Email == "bruno@exemplo.com.br");
            List<AuditEntry> entries = (await EntriesAsync(factory)).Where(e => e.TargetType == "User").ToList();

            CollectionAssert.AreEqual(
                new[] { "user.create", "user.change_role", "user.deactivate", "user.reactivate", "user.reset_password" },
                entries.Select(e => e.Action).ToArray());
            Assert.IsTrue(entries.All(e => e.ActorId == adminUser.Id), "o ator é o Administrador logado");
            Assert.IsTrue(entries.All(e => e.Result == AuditResult.Success));
            Assert.IsTrue(entries.All(e => !string.IsNullOrEmpty(e.CorrelationId)), "liga a entrada à linha do log");
            CollectionAssert.AreEqual(
                new[] { bruno.Id.ToString(), ana.Id.ToString(), ana.Id.ToString(), ana.Id.ToString(), ana.Id.ToString() },
                entries.Select(e => e.TargetId).ToArray());

            Assert.AreEqual("role=" + RoleNames.Writer, entries[0].NewValue);
            Assert.AreEqual("role=" + RoleNames.Writer, entries[1].PreviousValue);
            Assert.AreEqual("role=" + RoleNames.Administrator, entries[1].NewValue);
            Assert.AreEqual("active", entries[2].PreviousValue);
            Assert.AreEqual("inactive", entries[2].NewValue);
            Assert.AreEqual("inactive", entries[3].PreviousValue);
            Assert.AreEqual("active", entries[3].NewValue);
        }
    }

    [TestMethod]
    public async Task RedefinirSenha_RegistraQuemEQuando()
    {
        (WebFactory factory, HttpClient admin, AppUser adminUser, AppUser ana) = await StartAsync();
        using (factory)
        using (admin)
        {
            await admin.PostFormAsync($"/painel/usuarios/{ana.Id}/redefinir-senha", $"/painel/usuarios/{ana.Id}/redefinir-senha", new Dictionary<string, string> { ["ProvisionalPassword"] = Provisional });

            AuditEntry entry = (await EntriesAsync(factory)).Single(e => e.Action == "user.reset_password");

            Assert.AreEqual(adminUser.Id, entry.ActorId);
            Assert.AreEqual(ana.Id.ToString(), entry.TargetId);
            Assert.AreEqual(factory.Clock.Now.UtcDateTime, entry.OccurredAt, "quando: o relógio do site");
            Assert.IsNull(entry.PreviousValue);
            Assert.IsNull(entry.NewValue, "a senha nunca vai para a auditoria");
        }
    }

    [TestMethod]
    public async Task AuditoriaNaoGuardaSenhaNemEmail()
    {
        (WebFactory factory, HttpClient admin, _, AppUser ana) = await StartAsync();
        using (factory)
        using (admin)
        {
            await admin.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", new Dictionary<string, string>
            {
                ["FullName"] = "Bruno Lima",
                ["Email"] = "bruno@exemplo.com.br",
                ["Role"] = RoleNames.Writer,
                ["ProvisionalPassword"] = Provisional
            });
            await admin.PostFormAsync($"/painel/usuarios/{ana.Id}/redefinir-senha", $"/painel/usuarios/{ana.Id}/redefinir-senha", new Dictionary<string, string> { ["ProvisionalPassword"] = Provisional + "x" });

            string everything = string.Join("\n", (await EntriesAsync(factory)).Select(e => $"{e.Action}|{e.TargetType}|{e.TargetId}|{e.PreviousValue}|{e.NewValue}"));

            Assert.IsFalse(everything.Contains(Provisional, System.StringComparison.Ordinal));
            Assert.IsFalse(everything.Contains("@", System.StringComparison.Ordinal), "nem o e-mail (dado pessoal): o id identifica a pessoa");
        }
    }

    [TestMethod]
    public async Task RecusasDeRegra_GravamResultadoNegado_SemMudarNada()
    {
        (WebFactory factory, HttpClient admin, AppUser adminUser, _) = await StartAsync();
        using (factory)
        using (admin)
        {
            await admin.PostFormAsync($"/painel/usuarios/{adminUser.Id}/desativar", $"/painel/usuarios/{adminUser.Id}/desativar");
            await admin.PostFormAsync($"/painel/usuarios/{adminUser.Id}/editar", $"/painel/usuarios/{adminUser.Id}/editar", new Dictionary<string, string> { ["Role"] = RoleNames.Writer });

            List<AuditEntry> entries = (await EntriesAsync(factory)).Where(e => e.TargetType == "User").ToList();

            CollectionAssert.AreEqual(new[] { "user.deactivate", "user.change_role" }, entries.Select(e => e.Action).ToArray());
            Assert.IsTrue(entries.All(e => e.Result == AuditResult.Denied));
            Assert.IsTrue(entries.All(e => e.ActorId == adminUser.Id && e.TargetId == adminUser.Id.ToString()));
            AppUser unchanged = (await factory.ListUsersAsync()).Single(u => u.Id == adminUser.Id);
            Assert.IsTrue(unchanged.IsActive);
        }
    }

    [TestMethod]
    public async Task ErroDeValidacaoDoFormulario_NaoGravaAuditoria()
    {
        (WebFactory factory, HttpClient admin, _, AppUser ana) = await StartAsync();
        using (factory)
        using (admin)
        {
            await admin.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", new Dictionary<string, string>
            {
                ["FullName"] = "Bruno Lima",
                ["Email"] = "bruno",
                ["Role"] = RoleNames.Writer,
                ["ProvisionalPassword"] = "12345"
            });
            await admin.PostFormAsync($"/painel/usuarios/{ana.Id}/redefinir-senha", $"/painel/usuarios/{ana.Id}/redefinir-senha", new Dictionary<string, string> { ["ProvisionalPassword"] = "12345" });

            Assert.AreEqual(0, (await EntriesAsync(factory)).Count(e => e.TargetType == "User"), "nada mudou, então não há ação a registrar");
        }
    }
}
