using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GazetaMarketplace.Infrastructure.Categories;

/// <inheritdoc cref="ICategoryManagement"/>
public sealed class CategoryManagement(AppDbContext context, IAuditLog audit, ICategoryUsage usage, ICategoryTree tree) : ICategoryManagement
{
    private const string TargetType = "Category";

    // Espaço entre as posições ao renumerar as irmãs, para uma troca futura não exigir mexer em todas
    private const int OrderStep = 10;

    private sealed record Outcome(CategoryResult Result, bool Commit);

    public Task<CategoryResult> CreateAsync(int? parentId, string name, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            CategoryTreeSnapshot snapshot = await LoadSnapshotAsync(cancellationToken);
            CategoryViolation violation = CategoryRules.Check(snapshot, parentId, name);
            if (violation != CategoryViolation.None)
            {
                return Refused(FromViolation(violation));
            }

            Category parent = parentId is int pid ? await context.Categories.SingleAsync(c => c.Id == pid, cancellationToken) : null;
            if (parent is { IsPostable: true })
            {
                // Uma folha que ainda recebe anúncios deixaria de ser folha: os anúncios precisam sair antes
                IReadOnlyDictionary<int, int> ads = await usage.CountAdsAsync([parent.Id], cancellationToken);
                if (ads[parent.Id] > 0)
                {
                    return Refused(CategoryResult.Failure(nameof(parentId), CategoryMessages.ParentHasAds));
                }
            }

            string trimmed = name.Trim();
            HashSet<string> taken = [.. snapshot.All.Select(n => n.Slug)];
            Category category = new()
            {
                ParentId = parentId,
                Name = trimmed,
                Slug = SlugGenerator.Unique(trimmed, isPostable: true, taken),
                DisplayOrder = snapshot.ChildrenOf(parentId).Select(c => c.DisplayOrder).DefaultIfEmpty(0).Max() + OrderStep,
                IsPostable = true, // nasce folha; só deixa de ser se alguém criar uma subcategoria dentro dela
                IsSystem = false
            };
            context.Categories.Add(category);
            bool parentBecameGroup = parent is { IsPostable: true };
            if (parentBecameGroup)
            {
                parent.IsPostable = false;
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken); // o id só existe depois de gravar
            }
            catch (DbUpdateException)
            {
                // Duas pessoas criando o mesmo nome (ou o mesmo slug) ao mesmo tempo: o índice único recusou a segunda
                return Refused(CategoryResult.Failure(nameof(name), CategoryMessages.DuplicateName));
            }

            await audit.RecordAsync(
                new AuditRecord("category.create", TargetType, Id(category.Id), AuditResult.Success, null, Describe(category.Name, parentId)), cancellationToken);
            if (parentBecameGroup)
            {
                await audit.RecordAsync(
                    new AuditRecord("category.change_postable", TargetType, Id(parent.Id), AuditResult.Success, "aceita anúncios", "grupo (só subcategorias)"), cancellationToken);
            }

            return new Outcome(CategoryResult.Ok(category.Id), true);
        }, cancellationToken);

    public Task<CategoryResult> RenameAsync(int id, string name, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            CategoryTreeSnapshot snapshot = await LoadSnapshotAsync(cancellationToken);
            CategoryNode node = snapshot.Find(id);
            if (node is null)
            {
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.NotFound));
            }

            CategoryViolation violation = CategoryRules.Check(snapshot, node.ParentId, name, ignoreId: id);
            if (violation != CategoryViolation.None)
            {
                return Refused(FromViolation(violation));
            }

            string trimmed = name.Trim();
            if (string.Equals(node.Name, trimmed, StringComparison.Ordinal))
            {
                return new Outcome(CategoryResult.Ok(id), false); // nada mudou: não grava nem audita
            }

            // O slug não muda: os endereços públicos da categoria continuam valendo
            Category category = await context.Categories.SingleAsync(c => c.Id == id, cancellationToken);
            category.Name = trimmed;
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Refused(CategoryResult.Failure(nameof(name), CategoryMessages.DuplicateName));
            }

            await audit.RecordAsync(new AuditRecord("category.rename", TargetType, Id(id), AuditResult.Success, node.Name, trimmed), cancellationToken);
            return new Outcome(CategoryResult.Ok(id), true);
        }, cancellationToken);

    public Task<CategoryResult> MoveAsync(int id, MoveDirection direction, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            CategoryTreeSnapshot snapshot = await LoadSnapshotAsync(cancellationToken);
            CategoryNode node = snapshot.Find(id);
            if (node is null)
            {
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.NotFound));
            }

            // Só as irmãs (mesmo pai) trocam de lugar; o primeiro não sobe e o último não desce
            List<Category> siblings = await context.Categories
                .Where(c => c.ParentId == node.ParentId)
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id)
                .ToListAsync(cancellationToken);
            int index = siblings.FindIndex(c => c.Id == id);
            int target = direction == MoveDirection.Up ? index - 1 : index + 1;
            if (target < 0 || target >= siblings.Count)
            {
                return new Outcome(CategoryResult.Ok(id), false);
            }

            // Empates na ordem (a carga inicial pode ter) viram posições distintas antes da troca
            if (siblings.Select(c => c.DisplayOrder).Distinct().Count() != siblings.Count)
            {
                for (int i = 0; i < siblings.Count; i++)
                {
                    siblings[i].DisplayOrder = (i + 1) * OrderStep;
                }
            }

            Category moving = siblings[index];
            Category neighbor = siblings[target];
            (moving.DisplayOrder, neighbor.DisplayOrder) = (neighbor.DisplayOrder, moving.DisplayOrder);

            await context.SaveChangesAsync(cancellationToken);
            await audit.RecordAsync(
                new AuditRecord("category.move", TargetType, Id(id), AuditResult.Success, $"posição {index + 1}", $"posição {target + 1}"), cancellationToken);

            string message = direction == MoveDirection.Up
                ? $"{moving.Name} agora está antes de {neighbor.Name}"
                : $"{moving.Name} agora está depois de {neighbor.Name}";
            return new Outcome(CategoryResult.Ok(id, message), true);
        }, cancellationToken);

    public Task<CategoryResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            CategoryTreeSnapshot snapshot = await LoadSnapshotAsync(cancellationToken);
            CategoryNode node = snapshot.Find(id);
            if (node is null)
            {
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.NotFound));
            }

            // Do bloqueio mais duro para o mais fácil de contornar (decisão do Product Owner): campos específicos, subcategorias, anúncios
            if (CategoryRules.IsProtectedFromDeletion(node))
            {
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.Protected));
            }

            if (snapshot.ChildrenOf(id).Count > 0)
            {
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.HasSubcategories));
            }

            int ads = (await usage.CountAdsAsync([id], cancellationToken))[id];
            if (ads > 0)
            {
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.HasAds(ads)));
            }

            Category category = await context.Categories.SingleAsync(c => c.Id == id, cancellationToken);
            context.Categories.Remove(category);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Alguém criou uma subcategoria (ou um anúncio) aqui no mesmo instante: a chave estrangeira recusou a exclusão
                return Refused(CategoryResult.Failure(string.Empty, CategoryMessages.Conflict));
            }

            await audit.RecordAsync(
                new AuditRecord("category.delete", TargetType, Id(id), AuditResult.Success, Describe(node.Name, node.ParentId), null), cancellationToken);
            return new Outcome(CategoryResult.Ok(id), true);
        }, cancellationToken);

    private static string Id(int id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string Describe(string name, int? parentId) => parentId is null ? name : $"{name} (pai {parentId})";

    private static Outcome Refused(CategoryResult result) => new(result, false);

    private static CategoryResult FromViolation(CategoryViolation violation) => violation switch
    {
        CategoryViolation.EmptyName => CategoryResult.Failure("name", CategoryMessages.EmptyName),
        CategoryViolation.NameTooLong => CategoryResult.Failure("name", CategoryMessages.NameTooLong),
        CategoryViolation.DuplicateName => CategoryResult.Failure("name", CategoryMessages.DuplicateName),
        CategoryViolation.TooDeep => CategoryResult.Failure("parentId", CategoryMessages.TooDeep),
        CategoryViolation.ParentNotFound => CategoryResult.Failure("parentId", CategoryMessages.ParentNotFound),
        _ => throw new ArgumentOutOfRangeException(nameof(violation), violation, null)
    };

    // A decisão sempre parte do banco, nunca do cache: duas edições seguidas não podem validar contra uma árvore de dez minutos atrás
    private async Task<CategoryTreeSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken) =>
        CategoryTreeSnapshot.Build(await context.Categories.AsNoTracking()
            .Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))
            .ToListAsync(cancellationToken));

    // Serializable: duas pessoas editando o mesmo grupo ao mesmo tempo não passam pelas mesmas checagens. A transação é explícita, então o cache
    // da árvore (esvaziado pelo AppDbContext dentro do SaveChanges, antes do commit) é esvaziado de novo depois do commit.
    private async Task<CategoryResult> RunAsync(Func<Task<Outcome>> work, CancellationToken cancellationToken)
    {
        try
        {
            IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
            CategoryResult result = await strategy.ExecuteAsync(async () =>
            {
                context.ChangeTracker.Clear();
                await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                Outcome outcome = await work();
                if (outcome.Commit)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return outcome.Result;
            });
            tree.Invalidate();
            return result;
        }
        catch (ConflictException)
        {
            tree.Invalidate();
            return CategoryResult.Failure(string.Empty, CategoryMessages.Conflict);
        }
    }
}
