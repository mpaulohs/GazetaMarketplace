using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Gerenciar categorias (US-013) no SQL Server real: o fluxo completo com auditoria e cache, a identidade que continua depois do maior id da carga e
/// as corridas entre duas pessoas (duas criando o mesmo nome, dois movimentos, excluir contra criar filha). O SQLite dos testes de unidade
/// serializa tudo e não prova a transação serializável nem os índices únicos.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CategoryManagementTests
#pragma warning restore CA1515
{
    private const int Imoveis = 1;
    private const int Automoveis = 2;

    private static async Task<T> InScopeAsync<T>(IntegrationWebFactory factory, Func<IServiceProvider, Task<T>> work)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    private static Task<CategoryResult> CreateAsync(IntegrationWebFactory factory, int? parentId, string name) =>
        InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().CreateAsync(parentId, name, CancellationToken.None));

    private static Task<List<Category>> ChildrenAsync(IntegrationWebFactory factory, int? parentId) =>
        InScopeAsync(factory, sp => sp.GetRequiredService<AppDbContext>().Categories.AsNoTracking().Where(c => c.ParentId == parentId).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).ToListAsync());

    private static async Task<T[]> BothAtOnceAsync<T>(Func<Barrier, Task<T>> first, Func<Barrier, Task<T>> second)
    {
        using Barrier start = new(2);
        return await Task.WhenAll(Task.Run(() => first(start)), Task.Run(() => second(start)));
    }

    // Cada participante prepara o escopo, espera o outro na barreira e só então dispara
    private static async Task<CategoryResult> RunAsync(IntegrationWebFactory factory, Barrier start, Func<ICategoryManagement, Task<CategoryResult>> action)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ICategoryManagement management = scope.ServiceProvider.GetRequiredService<ICategoryManagement>();
        start.SignalAndWait();
        return await action(management);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FluxoCompleto_CriarRenomearMoverExcluir_ComAuditoriaECacheEmDia()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        ICategoryTree tree = factory.Services.GetRequiredService<ICategoryTree>();
        _ = await tree.GetAsync(CancellationToken.None); // aquece o cache: as operações precisam esvaziá-lo

        CategoryResult created = await CreateAsync(factory, Automoveis, "Quadriciclos");
        Assert.IsTrue(created.Succeeded);
        Assert.AreEqual(156, created.CategoryId, "a identidade continua depois do maior id da carga (155)");
        Assert.AreEqual("Quadriciclos", (await tree.GetAsync(CancellationToken.None)).Find(156).Name, "o cache já tem a categoria nova");

        CategoryResult renamed = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().RenameAsync(156, "Quadriciclos e UTVs", CancellationToken.None));
        Assert.IsTrue(renamed.Succeeded);
        Assert.AreEqual("quadriciclos", (await tree.GetAsync(CancellationToken.None)).Find(156).Slug, "o slug fica");
        Assert.AreEqual("Quadriciclos e UTVs", (await tree.GetAsync(CancellationToken.None)).Find(156).Name);

        CategoryResult moved = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().MoveAsync(156, MoveDirection.Up, CancellationToken.None));
        Assert.IsTrue(moved.Succeeded);
        StringAssert.Contains(moved.Message, "agora está antes de");

        CategoryResult deleted = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().DeleteAsync(156, CancellationToken.None));
        Assert.IsTrue(deleted.Succeeded);
        Assert.IsNull((await tree.GetAsync(CancellationToken.None)).Find(156));

        List<AuditEntry> audit = await InScopeAsync(factory, sp => sp.GetRequiredService<AppDbContext>().AuditEntries.AsNoTracking().Where(e => e.TargetType == "Category").OrderBy(e => e.Id).ToListAsync());
        CollectionAssert.AreEqual(new[] { "category.create", "category.rename", "category.move", "category.delete" }, audit.Select(e => e.Action).ToArray());
        Assert.AreEqual("Quadriciclos", audit[1].PreviousValue);
        Assert.AreEqual("Quadriciclos e UTVs", audit[1].NewValue);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DuasPessoasCriandoOMesmoNomeNoMesmoGrupoAoMesmoTempo_UmaCria_AOutraRecebeOAvisoDeNomeRepetido()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        for (int round = 1; round <= 6; round++)
        {
            string name = "Corrida " + round;
            CategoryResult[] results = await BothAtOnceAsync(
                start => RunAsync(factory, start, m => m.CreateAsync(Automoveis, name, CancellationToken.None)),
                start => RunAsync(factory, start, m => m.CreateAsync(Automoveis, name, CancellationToken.None)));

            Assert.AreEqual(1, results.Count(r => r.Succeeded), $"rodada {round}: exatamente uma cria");
            Assert.AreEqual(1, results.Count(r => !r.Succeeded && r.Message == CategoryMessages.DuplicateName), $"rodada {round}: a outra recebe o aviso de nome repetido");
            Assert.AreEqual(1, (await ChildrenAsync(factory, Automoveis)).Count(c => c.Name == name), $"rodada {round}: sobra uma linha");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DuasPessoasCriandoAMesmaCategoriaPrincipalAoMesmoTempo_NaoDuplica()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        CategoryResult[] results = await BothAtOnceAsync(
            start => RunAsync(factory, start, m => m.CreateAsync(null, "Colecionáveis", CancellationToken.None)),
            start => RunAsync(factory, start, m => m.CreateAsync(null, "colecionáveis", CancellationToken.None)));

        Assert.AreEqual(1, results.Count(r => r.Succeeded));
        Assert.AreEqual(1, (await ChildrenAsync(factory, null)).Count(c => c.Name.Equals("Colecionáveis", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DoisMovimentosDaMesmaCategoriaAoMesmoTempo_NuncaDeixamEmpateNemPerdemIrma()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        int[] original = [.. (await ChildrenAsync(factory, Imoveis)).Select(c => c.Id)];

        for (int round = 1; round <= 6; round++)
        {
            CategoryResult[] results = await BothAtOnceAsync(
                start => RunAsync(factory, start, m => m.MoveAsync(original[2], MoveDirection.Up, CancellationToken.None)),
                start => RunAsync(factory, start, m => m.MoveAsync(original[3], MoveDirection.Up, CancellationToken.None)));

            Assert.IsTrue(results.All(r => r.Succeeded || r.Message == CategoryMessages.Conflict), $"rodada {round}: sucesso ou aviso de conflito, nunca erro solto");
            List<Category> now = await ChildrenAsync(factory, Imoveis);
            Assert.AreEqual(now.Count, now.Select(c => c.DisplayOrder).Distinct().Count(), $"rodada {round}: nenhum empate");
            CollectionAssert.AreEquivalent(original, now.Select(c => c.Id).ToArray(), $"rodada {round}: nenhuma irmã sumiu nem entrou");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ExcluirUmPaiEmQuantoOutraPessoaCriaUmaFilha_NuncaDeixaFilhaSemPai()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        for (int round = 1; round <= 6; round++)
        {
            CategoryResult parent = await CreateAsync(factory, null, "Pai " + round);
            int parentId = parent.CategoryId!.Value;

            CategoryResult[] results = await BothAtOnceAsync(
                start => RunAsync(factory, start, m => m.DeleteAsync(parentId, CancellationToken.None)),
                start => RunAsync(factory, start, m => m.CreateAsync(parentId, "Filha " + round, CancellationToken.None)));

            CategoryResult deleted = results[0];
            CategoryResult child = results[1];
            Assert.IsFalse(deleted.Succeeded && child.Succeeded, $"rodada {round}: as duas não podem dar certo");
            List<Category> children = await ChildrenAsync(factory, parentId);
            bool parentExists = await InScopeAsync(factory, sp => sp.GetRequiredService<AppDbContext>().Categories.AnyAsync(c => c.Id == parentId));
            Assert.AreEqual(children.Count > 0, parentExists, $"rodada {round}: filha só existe se o pai existe");
            Assert.AreEqual(deleted.Succeeded, !parentExists, $"rodada {round}: o resultado da exclusão bate com o banco");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Banco_RecusaApagarUmPaiComFilhas_MesmoPorSqlDireto()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await using SqlConnection raw = new(connection);
        await raw.OpenAsync();
        await using SqlCommand command = raw.CreateCommand();
        command.CommandText = "DELETE FROM Categories WHERE Id = 1";

        SqlException error = await Assert.ThrowsExactlyAsync<SqlException>(() => command.ExecuteNonQueryAsync());

        Assert.AreEqual(547, error.Number, "violação de chave estrangeira (Restrict)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task NomeRepetido_ApesarDeDiferirSoNoAcento_SeguraAChecagemDoServico()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        CategoryResult result = await CreateAsync(factory, Automoveis, "Mótos"); // "Motos" já existe em Automóveis

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(CategoryMessages.DuplicateName, result.Message);
    }
}
