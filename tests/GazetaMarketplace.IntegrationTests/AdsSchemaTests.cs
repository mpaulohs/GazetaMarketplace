using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O esquema real de <c>Ads</c> e <c>AdPhotos</c> no SQL Server (ARCHITECTURE §6.2 e §6.4): colunas calculadas persistidas, CHECKs, chaves
/// estrangeiras sem cascata e índices. O SQLite dos testes de unidade não tem <c>JSON_VALUE</c> nem <c>ISJSON</c> (BACKLOG): isto é a prova.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdsSchemaTests
#pragma warning restore CA1515
{
    private const int CheckViolation = 547;
    private const int InvalidJson = 13609;
    private const int DuplicateKey = 2601;
    private const int Truncated = 2628;

    private static async Task<(string Connection, int Author)> StartAsync()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        return (connection, await AdData.AddUserAsync(connection));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ColunasCalculadas_SaoPersistidas_ComOsCaminhosEmTryCast()
    {
        (string connection, _) = await StartAsync();

        List<string> columns = await AdData.QueryAsync(connection,
            "SELECT name + '|' + CAST(is_persisted AS varchar(1)) + '|' + definition FROM sys.computed_columns WHERE object_id = OBJECT_ID('Ads') ORDER BY name");

        // O SQL Server guarda a definição como foi escrita (maiúsculas) ou normalizada; compara sem caixa e sem colchetes
        CollectionAssert.AreEqual(
            new[]
            {
                "AreaM2|1|(try_cast(json_value(attributes,'$.areaM2') as decimal(12,2)))",
                "Km|1|(try_cast(json_value(attributes,'$.km') as int))",
                "ModelYear|1|(try_cast(json_value(attributes,'$.modelYear') as int))",
                "VehicleBrandId|1|(try_cast(json_value(attributes,'$.brandId') as int))",
                "VehicleModelId|1|(try_cast(json_value(attributes,'$.modelId') as int))"
            }.Select(c => c.ToLowerInvariant()).ToArray(),
            columns.Select(c => c.Replace("[", string.Empty, System.StringComparison.Ordinal).Replace("]", string.Empty, System.StringComparison.Ordinal).ToLowerInvariant()).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task IndicesDoArchitecture_Existem_ComFiltroEOrdemDescendente()
    {
        (string connection, _) = await StartAsync();

        List<string> indexes = await AdData.QueryAsync(connection, """
            SELECT i.name + '|' + COALESCE(i.filter_definition, '-') + '|' + CAST(i.is_unique AS varchar(1)) + '|' +
                   (SELECT STRING_AGG(c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END, ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                    FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0)
            FROM sys.indexes i WHERE i.object_id = OBJECT_ID('Ads') AND i.name LIKE '%[_]Ads[_]%' ORDER BY i.name
            """);

        CollectionAssert.AreEqual(
            new[]
            {
                "IX_Ads_ArchivedById|-|0|ArchivedById",
                "IX_Ads_AreaM2|-|0|AreaM2",
                "IX_Ads_AuthorId_Status_UpdatedAt|-|0|AuthorId,Status,UpdatedAt",
                "IX_Ads_CategoryId|-|0|CategoryId",
                "IX_Ads_Km|-|0|Km",
                "IX_Ads_PublishedById|-|0|PublishedById",
                "IX_Ads_RejectedById|-|0|RejectedById",
                "IX_Ads_Status_CategoryId_PublishedAt|-|0|Status,CategoryId,PublishedAt DESC",
                "IX_Ads_Status_PriceCents|([PriceCents] IS NOT NULL)|0|Status,PriceCents",
                "IX_Ads_Status_PublishedAt_Id|-|0|Status,PublishedAt DESC,Id DESC",
                "IX_Ads_Status_Uf_City|-|0|Status,Uf,City",
                "IX_Ads_VehicleBrandId_ModelYear|-|0|VehicleBrandId,ModelYear"
            },
            indexes);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task RascunhoSoComOTitulo_Grava_SemCategoriaPrecoNemEndereco()
    {
        (string connection, int author) = await StartAsync();

        int id = await AdData.AddDraftAsync(connection, author, title: "Moto para retirar peças");

        Ad stored = await AdData.LoadAsync(connection, id);
        Assert.AreEqual("Moto para retirar peças", stored.Title);
        Assert.AreEqual("moto para retirar pecas", stored.TitleSearch);
        Assert.IsNull(stored.CategoryId);
        Assert.IsNull(stored.PriceCents);
        Assert.IsNull(stored.Cep);
        Assert.AreEqual("{}", stored.Attributes);
        Assert.IsNull(stored.VehicleBrandId);
        Assert.IsNotNull(stored.RowVersion);
        Assert.AreEqual(author, stored.AuthorId);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OBancoRecusa_JsonInvalido_ArrayEEscalar()
    {
        (string connection, int author) = await StartAsync();

        int broken = await AdData.InsertRawAsync(connection, author, ("Attributes", "{\"km\":"));
        Assert.IsTrue(broken is CheckViolation or InvalidJson, $"JSON malformado deveria ser recusado (erro {broken})");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Attributes", "[1,2]")), "ISJSON aceita arrays; o CHECK exige objeto");
        // Escalares não são JSON para o SQL Server: a coluna calculada falha antes do CHECK
        Assert.IsTrue(await AdData.InsertRawAsync(connection, author, ("Attributes", "42")) is CheckViolation or InvalidJson);
        Assert.IsTrue(await AdData.InsertRawAsync(connection, author, ("Attributes", "\"texto\"")) is CheckViolation or InvalidJson);
        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Attributes", "  {\"km\": 10}")), "objeto com espaço na frente é aceito");
        Assert.AreEqual(1, (await AdData.QueryAsync(connection, "SELECT COUNT(*) FROM Ads")).Select(int.Parse).Single());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OBancoRecusa_StatusForaDeUmACinco()
    {
        (string connection, int author) = await StartAsync();

        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Status", (byte)0)));
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Status", (byte)6)));
        foreach (byte status in AdStatus.All)
        {
            Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Status", status)), AdStatus.Label(status));
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OBancoRecusa_PrecoZeroNegativoEAcimaDoTeto_AceitaNuloEOLimite()
    {
        (string connection, int author) = await StartAsync();

        foreach (long cents in new long[] { 0, -1, 10_000_000_000 })
        {
            Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("PriceCents", cents)), $"preço {cents}");
        }

        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("PriceCents", null)), "sem preço (Serviços)");
        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("PriceCents", 1L)));
        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("PriceCents", 9_999_999_999L)));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OBancoRecusa_CepUfTituloEDescricaoMalformados()
    {
        (string connection, int author) = await StartAsync();

        foreach (string cep in new[] { "1234567", "1234567a", "abcdefgh", "123 4567" })
        {
            Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Cep", cep)), $"CEP '{cep}'");
        }

        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Cep", "13015100")));
        Assert.AreEqual(Truncated, await AdData.InsertRawAsync(connection, author, ("Uf", "SPP")), "não cabe em char(2)");
        foreach (string uf in new[] { "S", "12", "S1" })
        {
            Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Uf", uf)), $"UF '{uf}'");
        }

        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Uf", "SP")));
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Title", "   ")), "título só com espaços");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("Description", new string('x', 6001))));
        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Description", new string('x', 6000))));
        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Description", null)));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task TituloAcimaDe120_NaoCabeNaColuna()
    {
        (string connection, int author) = await StartAsync();

        int tooLong = await AdData.InsertRawAsync(connection, author, ("Title", new string('a', 121)));

        Assert.AreEqual(Truncated, tooLong, "string or binary data would be truncated");
        Assert.AreEqual(0, await AdData.InsertRawAsync(connection, author, ("Title", new string('a', 120))));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ChavesEstrangeiras_SemCascata_ImpedemApagarCategoriaEUsuarioComAnuncios()
    {
        (string connection, int author) = await StartAsync();
        int adId = await AdData.AddDraftAsync(connection, author, categoryId: 58);

        Assert.AreEqual(CheckViolation, await AdData.TryExecuteAsync(connection, "DELETE FROM Categories WHERE Id = 58"), "categoria com anúncio");
        Assert.AreEqual(CheckViolation, await AdData.TryExecuteAsync(connection, "DELETE FROM AspNetUsers WHERE Id = @id", ("@id", author)), "autor com anúncio");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("CategoryId", 99999)), "categoria que não existe");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, 99999), "autor que não existe");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("PublishedById", 99999)), "quem publicou não existe");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("RejectedById", 99999)), "quem rejeitou não existe");
        Assert.AreEqual(CheckViolation, await AdData.InsertRawAsync(connection, author, ("ArchivedById", 99999)), "quem arquivou não existe");
        Assert.AreEqual(adId, (await AdData.LoadAsync(connection, adId)).Id);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FotosDoAnuncio_ChaveEstrangeiraSemCascata_ChecksEChaveDeArquivoUnica()
    {
        (string connection, int author) = await StartAsync();
        int adId = await AdData.AddDraftAsync(connection, author);
        const string photo = "INSERT INTO AdPhotos (AdId, SortOrder, StorageKey, Width, Height, SizeBytes, CreatedAt) VALUES (@ad, @sort, @key, @w, @h, @size, SYSUTCDATETIME())";

        Assert.AreEqual(0, await AdData.TryExecuteAsync(connection, photo, ("@ad", adId), ("@sort", 0), ("@key", "a_1600.webp"), ("@w", 1600), ("@h", 1200), ("@size", 80_000)));
        Assert.AreEqual(DuplicateKey, await AdData.TryExecuteAsync(connection, photo, ("@ad", adId), ("@sort", 1), ("@key", "a_1600.webp"), ("@w", 1600), ("@h", 1200), ("@size", 80_000)), "chave de arquivo repetida");
        Assert.AreEqual(CheckViolation, await AdData.TryExecuteAsync(connection, photo, ("@ad", adId), ("@sort", -1), ("@key", "b.webp"), ("@w", 1600), ("@h", 1200), ("@size", 1)));
        Assert.AreEqual(CheckViolation, await AdData.TryExecuteAsync(connection, photo, ("@ad", adId), ("@sort", 1), ("@key", "c.webp"), ("@w", 0), ("@h", 1200), ("@size", 1)));
        Assert.AreEqual(CheckViolation, await AdData.TryExecuteAsync(connection, photo, ("@ad", 99999), ("@sort", 1), ("@key", "d.webp"), ("@w", 1), ("@h", 1), ("@size", 1)), "anúncio que não existe");
        Assert.AreEqual(CheckViolation, await AdData.TryExecuteAsync(connection, "DELETE FROM Ads WHERE Id = @id", ("@id", adId)), "anúncio com foto não é apagado em cascata");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FotoGravadaPeloEf_RecebeCreatedAtDoContexto_EOriginalPodeSerNulo()
    {
        (string connection, int author) = await StartAsync();
        int adId = await AdData.AddDraftAsync(connection, author);

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        AdPhoto photo = new() { AdId = adId, SortOrder = 0, StorageKey = "1_1600.webp", Width = 1600, Height = 1200, SizeBytes = 90_000, OriginalKey = "_originals/2026-10/abc.heic" };
        context.AdPhotos.Add(photo);
        await context.SaveChangesAsync();

        AdPhoto stored = await context.AdPhotos.AsNoTracking().SingleAsync(p => p.Id == photo.Id);
        Assert.IsTrue((System.DateTime.UtcNow - stored.CreatedAt).Duration() < System.TimeSpan.FromMinutes(1));
        Assert.AreEqual("_originals/2026-10/abc.heic", stored.OriginalKey);
        Assert.AreEqual(1, await context.AdPhotos.Where(p => p.AdId == adId).ExecuteUpdateAsync(s => s.SetProperty(p => p.OriginalKey, (string)null)), "a limpeza de 30 dias anula em uma instrução (3.6)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task EfNaoGravaAsColunasCalculadas_ERecalculaAoAtualizarOJson()
    {
        (string connection, int author) = await StartAsync();
        int id = await AdData.AddDraftAsync(connection, author, 33, """{"brandId":12,"km":45000}""");
        Assert.AreEqual(12, (await AdData.LoadAsync(connection, id)).VehicleBrandId);

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = await context.Ads.SingleAsync(a => a.Id == id);
        Assert.IsTrue(AdAttributes.TryParse("""{"brandId":13,"km":50000,"modelYear":2020}""", out AdAttributes attributes));
        ad.SetAttributes(attributes);
        await context.SaveChangesAsync();

        Ad stored = await AdData.LoadAsync(connection, id);
        Assert.AreEqual(13, stored.VehicleBrandId);
        Assert.AreEqual(50000, stored.Km);
        Assert.AreEqual(2020, stored.ModelYear);
        Assert.IsNull(stored.VehicleModelId);
    }
}
