using System;
using System.Collections.Generic;
using System.IO;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Configuration;

/// <summary>
/// RC-16 e S3 (ADR-011, SECURITY_REQUIREMENTS §4): as chaves do Data Protection (cookie de login, token antiforgery, links de redefinição de senha) ficam numa pasta persistente fora da raiz do site.
/// Sem isso, na hospedagem compartilhada cada reciclagem do pool pode gerar um anel de chaves novo e derrubar a sessão da equipe e os links já enviados.
/// </summary>
[TestClass]
public sealed class DataProtectionKeysTests
{
    private string _folder;

    [TestInitialize]
    public void CreateFolderName() => _folder = Path.Combine(Path.GetTempPath(), "gazeta-chaves-" + Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void DeleteFolder()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private Dictionary<string, string> Production()
    {
        Dictionary<string, string> configuration = WebFactory.ProductionConfiguration();
        configuration["DataProtection:KeysDirectory"] = _folder;
        return configuration;
    }

    [TestMethod]
    public void TheKeys_AreWrittenToTheConfiguredFolder_AndAFreshHostReadsWhatTheFirstOneProtected()
    {
        string payload;
        using (WebFactory first = new("Production", Production(), withDatabase: true))
        {
            payload = first.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("teste").Protect("segredo");
        }

        Assert.IsTrue(Directory.Exists(_folder), "a pasta configurada foi criada");
        Assert.IsNotEmpty(Directory.GetFiles(_folder, "key-*.xml"), "a chave foi gravada na pasta configurada");

        // Um processo novo (a reciclagem do pool) lê o mesmo anel de chaves
        using WebFactory second = new("Production", Production(), withDatabase: true);
        string text = second.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("teste").Unprotect(payload);

        Assert.AreEqual("segredo", text);
        Assert.HasCount(1, Directory.GetFiles(_folder, "key-*.xml"), "o segundo host reaproveitou a chave em vez de gerar outra");
    }

    [TestMethod]
    public void WithoutAFolder_InDevelopmentOrTests_TheDefaultKeyRingStillWorks()
    {
        using WebFactory testing = new("Testing", withDatabase: true);

        string payload = testing.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("teste").Protect("x");

        Assert.AreEqual("x", testing.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("teste").Unprotect(payload));
    }
}
