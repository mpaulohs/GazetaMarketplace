using System.Linq;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

/// <summary>ADR-003 e ARCHITECTURE.md §6.2: Identity com chave int, usuário estendido e os dois papéis fixos.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class IdentidadeModeloTests
#pragma warning restore CA1515
{
    private static AppDbContext NewContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
        new FakeCurrentUser(), new FakeClock());

    [TestMethod]
    public void UsuarioEPapel_TemChaveInt()
    {
        using AppDbContext context = NewContext();

        Assert.AreEqual(typeof(int), context.Model.FindEntityType(typeof(UsuarioIdentity)).FindPrimaryKey().Properties.Single().ClrType);
        Assert.AreEqual(typeof(int), context.Model.FindEntityType(typeof(PapelIdentity)).FindPrimaryKey().Properties.Single().ClrType);
        Assert.IsTrue(typeof(IdentityUser<int>).IsAssignableFrom(typeof(UsuarioIdentity)));
        Assert.IsTrue(typeof(IdentityRole<int>).IsAssignableFrom(typeof(PapelIdentity)));
    }

    [TestMethod]
    public void UsuarioEstendido_TemNomeCompletoAtivoETrocaObrigatoria()
    {
        using AppDbContext context = NewContext();
        IEntityType user = context.Model.FindEntityType(typeof(UsuarioIdentity));

        IProperty name = user.FindProperty(nameof(UsuarioIdentity.FullName));
        Assert.IsFalse(name.IsNullable);
        Assert.AreEqual(100, name.GetMaxLength());
        Assert.AreEqual(true, user.FindProperty(nameof(UsuarioIdentity.IsActive)).GetDefaultValue());
        Assert.AreEqual(false, user.FindProperty(nameof(UsuarioIdentity.MustChangePassword)).GetDefaultValue());
        Assert.IsTrue(new UsuarioIdentity().IsActive, "uma conta nova nasce ativa");
    }

    [TestMethod]
    public void PapeisFixos_VemDaMigration_ComOsNomesDoCore()
    {
        using AppDbContext context = NewContext();

        var papeis = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PapelIdentity)).GetSeedData()
            .Select(d => (Id: (int)d["Id"], Name: (string)d["Name"], Normalizado: (string)d["NormalizedName"]))
            .OrderBy(p => p.Id)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { (1, RoleNames.Administrator, "ADMINISTRADOR"), (2, RoleNames.Writer, "REDATOR") },
            papeis);
    }

    [TestMethod]
    public void NoBancoDeTestes_OsDoisPapeisExistem()
    {
        using TestDatabase database = new();
        using AppDbContext context = database.NewContext();

        string[] nomes = context.Roles.Select(r => r.Name).OrderBy(n => n).ToArray();

        CollectionAssert.AreEqual(new[] { RoleNames.Administrator, RoleNames.Writer }, nomes);
    }

    [TestMethod]
    public void MesmoEmail_NaoGravaDuasContas_PeloIndiceDoNomeDeUsuario()
    {
        using TestDatabase database = new();
        using AppDbContext context = database.NewContext();
        context.Users.Add(new UsuarioIdentity { UserName = "ana@exemplo.com.br", NormalizedUserName = "ANA@EXEMPLO.COM.BR", FullName = "Ana" });
        context.SaveChanges();
        context.Users.Add(new UsuarioIdentity { UserName = "ANA@exemplo.com.br", NormalizedUserName = "ANA@EXEMPLO.COM.BR", FullName = "Outra Ana" });

        Assert.ThrowsExactly<DbUpdateException>(() => context.SaveChanges());
    }
}
