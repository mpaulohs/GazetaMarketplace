using System.Linq;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
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
public sealed class IdentityModelTests
#pragma warning restore CA1515
{
    private static AppDbContext NewContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
        new FakeCurrentUser(), new FakeClock());

    [TestMethod]
    public void UsuarioEPapel_TemChaveInt()
    {
        using AppDbContext context = NewContext();

        Assert.AreEqual(typeof(int), context.Model.FindEntityType(typeof(AppUser)).FindPrimaryKey().Properties.Single().ClrType);
        Assert.AreEqual(typeof(int), context.Model.FindEntityType(typeof(AppRole)).FindPrimaryKey().Properties.Single().ClrType);
        Assert.IsTrue(typeof(IdentityUser<int>).IsAssignableFrom(typeof(AppUser)));
        Assert.IsTrue(typeof(IdentityRole<int>).IsAssignableFrom(typeof(AppRole)));
    }

    [TestMethod]
    public void UsuarioEstendido_TemNomeCompletoAtivoETrocaObrigatoria()
    {
        using AppDbContext context = NewContext();
        IEntityType user = context.Model.FindEntityType(typeof(AppUser));

        IProperty name = user.FindProperty(nameof(AppUser.FullName));
        Assert.IsFalse(name.IsNullable);
        Assert.AreEqual(100, name.GetMaxLength());
        Assert.AreEqual(true, user.FindProperty(nameof(AppUser.IsActive)).GetDefaultValue());
        Assert.AreEqual(false, user.FindProperty(nameof(AppUser.MustChangePassword)).GetDefaultValue());
        Assert.IsTrue(new AppUser().IsActive, "uma conta nova nasce ativa");
    }

    [TestMethod]
    public void PapeisFixos_VemDaMigration_ComOsNomesDoCore()
    {
        using AppDbContext context = NewContext();

        var roles = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(AppRole)).GetSeedData()
            .Select(d => (Id: (int)d["Id"], Name: (string)d["Name"], Normalized: (string)d["NormalizedName"]))
            .OrderBy(p => p.Id)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { (1, RoleNames.Administrator, "ADMINISTRADOR"), (2, RoleNames.Writer, "REDATOR") },
            roles);
    }

    [TestMethod]
    public void NoBancoDeTestes_OsDoisPapeisExistem()
    {
        using TestDatabase database = new();
        using AppDbContext context = database.NewContext();

        string[] names = context.Roles.Select(r => r.Name).OrderBy(n => n).ToArray();

        CollectionAssert.AreEqual(new[] { RoleNames.Administrator, RoleNames.Writer }, names);
    }

    [TestMethod]
    public void MesmoEmail_NaoGravaDuasContas_PeloIndiceDoNomeDeUsuario()
    {
        using TestDatabase database = new();
        using AppDbContext context = database.NewContext();
        context.Users.Add(new AppUser { UserName = "ana@exemplo.com.br", NormalizedUserName = "ANA@EXEMPLO.COM.BR", FullName = "Ana" });
        context.SaveChanges();
        context.Users.Add(new AppUser { UserName = "ANA@exemplo.com.br", NormalizedUserName = "ANA@EXEMPLO.COM.BR", FullName = "Outra Ana" });

        Assert.ThrowsExactly<DbUpdateException>(() => context.SaveChanges());
    }
}
