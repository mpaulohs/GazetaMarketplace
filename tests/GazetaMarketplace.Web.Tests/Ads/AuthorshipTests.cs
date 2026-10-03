using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>Quem lê, edita e muda a situação do anúncio (regra pura; o serviço a aplica no servidor).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AuthorshipTests
#pragma warning restore CA1515
{
    private const int Author = 10;
    private const int OtherWriter = 11;

    private static readonly AdActor Admin = new(1, true);
    private static readonly AdActor AuthorWriter = new(Author, false);
    private static readonly AdActor OtherWriterActor = new(OtherWriter, false);
    private static readonly AdActor Anonymous = new(null, false);

    [TestMethod]
    public void Redator_LeOsProprios_EmQualquerSituacao_ENuncaOsDeOutro()
    {
        foreach (byte status in AdStatus.All)
        {
            Ad ad = AdFactory.At(status, Author);
            Assert.IsTrue(AdAccess.CanView(AuthorWriter, ad), $"autor lê o próprio em {AdStatus.Label(status)}");
            Assert.IsFalse(AdAccess.CanView(OtherWriterActor, ad), $"outro Redator não lê em {AdStatus.Label(status)}");
            Assert.IsFalse(AdAccess.CanView(Anonymous, ad), $"sem identidade não lê em {AdStatus.Label(status)}");
            Assert.IsTrue(AdAccess.CanView(Admin, ad), $"Administrador lê tudo em {AdStatus.Label(status)}");
        }
    }

    [TestMethod]
    public void Redator_EditaOsProprios_SoEmRascunhoOuRejeitado()
    {
        HashSet<byte> editable = [AdStatus.Draft, AdStatus.Rejected];
        foreach (byte status in AdStatus.All)
        {
            Ad ad = AdFactory.At(status, Author);
            Assert.AreEqual(editable.Contains(status), AdAccess.CanEdit(AuthorWriter, ad), $"autor em {AdStatus.Label(status)}");
            Assert.IsFalse(AdAccess.CanEdit(OtherWriterActor, ad), $"outro Redator em {AdStatus.Label(status)}");
            Assert.IsFalse(AdAccess.CanEdit(Anonymous, ad), $"anônimo em {AdStatus.Label(status)}");
        }
    }

    [TestMethod]
    public void Administrador_EditaQualquerUm_MenosOArquivado()
    {
        foreach (byte status in AdStatus.All)
        {
            Assert.AreEqual(status != AdStatus.Archived, AdAccess.CanEdit(Admin, AdFactory.At(status, Author)), AdStatus.Label(status));
        }
    }

    [TestMethod]
    public void Passagens_Autor_SoEnvia_Administrador_FazTodas()
    {
        Ad draft = AdFactory.At(AdStatus.Draft, Author);

        foreach (AdTransition transition in AdStatusRules.All)
        {
            Assert.IsTrue(AdAccess.CanTransition(Admin, draft, transition), transition.Action);
            Assert.AreEqual(transition.AuthorAllowed, AdAccess.CanTransition(AuthorWriter, draft, transition), $"autor: {transition.Action}");
            Assert.IsFalse(AdAccess.CanTransition(OtherWriterActor, draft, transition), $"outro Redator: {transition.Action}");
            Assert.IsFalse(AdAccess.CanTransition(Anonymous, draft, transition), $"anônimo: {transition.Action}");
        }
    }

    [TestMethod]
    public void AdministradorAutor_ContaComoAdministrador_ENaoComoRedator()
    {
        AdActor adminAuthor = new(Author, true);

        Assert.IsTrue(AdAccess.CanEdit(adminAuthor, AdFactory.At(AdStatus.Published, Author)), "Administrador edita publicado, mesmo sendo autor");
        Assert.IsFalse(AdAccess.CanEdit(adminAuthor, AdFactory.At(AdStatus.Archived, Author)));
        Assert.AreEqual(9, AdStatusRules.All.Count(t => AdAccess.CanTransition(adminAuthor, AdFactory.At(t.From, Author), t)));
    }
}
