using System.Linq;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

/// <summary>ADR-003 e ARCHITECTURE.md §6.2: Identity com chave int, usuário estendido e os dois papéis fixos.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class IdentidadeModeloTests
#pragma warning restore CA1515
{
    private static AppDbContext NovoContexto() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
        new UsuarioFalso(), new RelogioFalso());

    [TestMethod]
    public void UsuarioEPapel_TemChaveInt()
    {
        using AppDbContext contexto = NovoContexto();

        Assert.AreEqual(typeof(int), contexto.Model.FindEntityType(typeof(UsuarioIdentity)).FindPrimaryKey().Properties.Single().ClrType);
        Assert.AreEqual(typeof(int), contexto.Model.FindEntityType(typeof(PapelIdentity)).FindPrimaryKey().Properties.Single().ClrType);
        Assert.IsTrue(typeof(IdentityUser<int>).IsAssignableFrom(typeof(UsuarioIdentity)));
        Assert.IsTrue(typeof(IdentityRole<int>).IsAssignableFrom(typeof(PapelIdentity)));
    }

    [TestMethod]
    public void UsuarioEstendido_TemNomeCompletoAtivoETrocaObrigatoria()
    {
        using AppDbContext contexto = NovoContexto();
        IEntityType usuario = contexto.Model.FindEntityType(typeof(UsuarioIdentity));

        IProperty nome = usuario.FindProperty(nameof(UsuarioIdentity.FullName));
        Assert.IsFalse(nome.IsNullable);
        Assert.AreEqual(100, nome.GetMaxLength());
        Assert.AreEqual(true, usuario.FindProperty(nameof(UsuarioIdentity.IsActive)).GetDefaultValue());
        Assert.AreEqual(false, usuario.FindProperty(nameof(UsuarioIdentity.MustChangePassword)).GetDefaultValue());
        Assert.IsTrue(new UsuarioIdentity().IsActive, "uma conta nova nasce ativa");
    }

    [TestMethod]
    public void PapeisFixos_VemDaMigration_ComOsNomesDoCore()
    {
        using AppDbContext contexto = NovoContexto();

        var papeis = contexto.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PapelIdentity)).GetSeedData()
            .Select(d => (Id: (int)d["Id"], Nome: (string)d["Name"], Normalizado: (string)d["NormalizedName"]))
            .OrderBy(p => p.Id)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { (1, Papeis.Administrador, "ADMINISTRADOR"), (2, Papeis.Redator, "REDATOR") },
            papeis);
    }

    [TestMethod]
    public void NoBancoDeTestes_OsDoisPapeisExistem()
    {
        using BancoDeTestes banco = new();
        using AppDbContext contexto = banco.NovoContexto();

        string[] nomes = contexto.Roles.Select(r => r.Name).OrderBy(n => n).ToArray();

        CollectionAssert.AreEqual(new[] { Papeis.Administrador, Papeis.Redator }, nomes);
    }

    [TestMethod]
    public void MesmoEmail_NaoGravaDuasContas_PeloIndiceDoNomeDeUsuario()
    {
        using BancoDeTestes banco = new();
        using AppDbContext contexto = banco.NovoContexto();
        contexto.Users.Add(new UsuarioIdentity { UserName = "ana@exemplo.com.br", NormalizedUserName = "ANA@EXEMPLO.COM.BR", FullName = "Ana" });
        contexto.SaveChanges();
        contexto.Users.Add(new UsuarioIdentity { UserName = "ANA@exemplo.com.br", NormalizedUserName = "ANA@EXEMPLO.COM.BR", FullName = "Outra Ana" });

        Assert.ThrowsExactly<DbUpdateException>(() => contexto.SaveChanges());
    }
}
