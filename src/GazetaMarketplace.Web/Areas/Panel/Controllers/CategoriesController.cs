using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// Categorias do site (US-013): criar até o terceiro nível, renomear, mover entre irmãs e excluir. Só o Administrador entra; o Redator cai em "acesso negado".
/// As regras e a auditoria ficam em <see cref="ICategoryManagement"/>. Tudo funciona sem JavaScript: mover e excluir são formulários e páginas
/// (POST/redirecionar/GET); o JavaScript só troca a página de confirmação por uma janela e devolve o foco depois de mover.
/// </summary>
[Authorize(Policy = AccessPolicies.Administrator)]
[Route("painel/categorias")]
public sealed class CategoriesController(ICategoryTree tree, ICategoryUsage usage, ICategoryManagement management) : PanelControllerBase
{
    private const string MessageKey = "CategoriesMessage";
    private const string AlertKey = "CategoriesAlert";
    private const string FocusKey = "CategoriesFocus";

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        IReadOnlyList<CategoryNode> all = snapshot.All;
        IReadOnlyDictionary<int, int> ads = await usage.CountAdsAsync([.. all.Select(n => n.Id)], cancellationToken);

        return View(new CategoriesIndexViewModel
        {
            Rows = [.. all.Select(node =>
            {
                IReadOnlyList<CategoryNode> siblings = snapshot.ChildrenOf(node.ParentId);
                return new CategoryRowViewModel
                {
                    Id = node.Id,
                    Name = node.Name,
                    Depth = node.Depth,
                    ParentName = node.ParentId is int parent ? snapshot.Find(parent)?.Name : null,
                    AdsCount = ads.GetValueOrDefault(node.Id),
                    IsFirst = siblings[0].Id == node.Id,
                    IsLast = siblings[^1].Id == node.Id,
                    IsProtected = CategoryRules.IsProtectedFromDeletion(node)
                };
            })],
            Message = TempData[MessageKey] as string,
            Alert = TempData[AlertKey] as string,
            FocusId = TempData[FocusKey] as string
        });
    }

    [HttpGet("nova")]
    public async Task<IActionResult> New(CancellationToken cancellationToken) =>
        View(new CategoryFormViewModel { Parents = await ParentOptionsAsync(cancellationToken) });

    [HttpPost("nova")]
    public async Task<IActionResult> New(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        CategoryResult result = await management.CreateAsync(model.ParentId, model.Name, cancellationToken);
        if (!result.Succeeded)
        {
            model.Parents = await ParentOptionsAsync(cancellationToken);
            return Failure(result, model, nameof(New));
        }

        return Saved($"Categoria {model.Name.Trim()} criada.", result.CategoryId);
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        CategoryNode node = (await tree.GetAsync(cancellationToken)).Find(id);
        return node is null ? NotFound() : View(await ToFormAsync(node, node.Name, cancellationToken));
    }

    [HttpPost("{id:int}/editar")]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        CategoryNode node = (await tree.GetAsync(cancellationToken)).Find(id);
        if (node is null)
        {
            return NotFound();
        }

        CategoryResult result = await management.RenameAsync(id, model.Name, cancellationToken);
        if (!result.Succeeded)
        {
            // O pai não vem do formulário: volta do cadastro, para a tela não perdê-lo
            return Failure(result, await ToFormAsync(node, model.Name, cancellationToken), nameof(Edit));
        }

        return Saved($"Categoria renomeada para {model.Name.Trim()}.", id);
    }

    [HttpPost("{id:int}/mover/{direction}")]
    public async Task<IActionResult> Move(int id, string direction, CancellationToken cancellationToken)
    {
        MoveDirection? where = direction switch { "cima" => MoveDirection.Up, "baixo" => MoveDirection.Down, _ => null };
        if (where is null)
        {
            return NotFound();
        }

        CategoryResult result = await management.MoveAsync(id, where.Value, cancellationToken);
        if (!result.Succeeded)
        {
            return result.Message == CategoryMessages.NotFound ? NotFound() : Blocked(result.Message, id);
        }

        TempData[MessageKey] = result.Message;
        TempData[FocusKey] = $"mover-{id}-{direction}";
        return Redirect(Url.Action(nameof(Index)) + "#categoria-" + id);
    }

    [HttpGet("{id:int}/excluir")]
    public async Task<IActionResult> ConfirmDelete(int id, CancellationToken cancellationToken)
    {
        CategoryNode node = (await tree.GetAsync(cancellationToken)).Find(id);
        if (node is null)
        {
            return NotFound();
        }

        // Bloqueio permanente (campos específicos): sem passo de confirmação, a mensagem aparece já ao clicar em "Excluir" (US-013-S10)
        return CategoryRules.IsProtectedFromDeletion(node)
            ? Blocked(CategoryMessages.Protected, id)
            : View(new ConfirmDeleteViewModel { Id = node.Id, Name = node.Name });
    }

    [HttpPost("{id:int}/excluir")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        CategoryNode node = (await tree.GetAsync(cancellationToken)).Find(id);
        if (node is null)
        {
            return NotFound();
        }

        CategoryResult result = await management.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return result.Message == CategoryMessages.NotFound ? NotFound() : Blocked(result.Message, id);
        }

        TempData[MessageKey] = $"Categoria {node.Name} excluída.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Opções de "Categoria pai": nenhuma (principal), as principais e as subcategorias; nunca as de terceiro nível (US-013-S11).</summary>
    private async Task<IReadOnlyList<ParentOption>> ParentOptionsAsync(CancellationToken cancellationToken)
    {
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        List<ParentOption> options = [new ParentOption(null, "— Nenhuma (categoria principal) —")];
        options.AddRange(snapshot.All
            .Where(n => n.Depth < CategoryRules.MaxDepth)
            .Select(n => new ParentOption(n.Id, new string('—', n.Depth - 1) + (n.Depth > 1 ? " " : string.Empty) + n.Name)));
        return options;
    }

    private async Task<CategoryFormViewModel> ToFormAsync(CategoryNode node, string name, CancellationToken cancellationToken) =>
        new()
        {
            Id = node.Id,
            Name = name,
            ParentId = node.ParentId,
            ParentName = node.ParentId is int parent ? (await tree.GetAsync(cancellationToken)).Find(parent)?.Name : null
        };

    private IActionResult Saved(string message, int? id)
    {
        TempData[MessageKey] = message;
        return Redirect(Url.Action(nameof(Index)) + (id is null ? string.Empty : "#categoria-" + id));
    }

    private RedirectResult Blocked(string message, int id)
    {
        TempData[AlertKey] = message;
        return Redirect(Url.Action(nameof(Index)) + "#categoria-" + id);
    }

    /// <summary>Mostra de novo o formulário, com a mensagem no campo dela; as sem campo vão para o aviso do topo.</summary>
    private IActionResult Failure(CategoryResult result, CategoryFormViewModel model, string view)
    {
        string field = result.Field switch
        {
            "name" => nameof(CategoryFormViewModel.Name),
            "parentId" => nameof(CategoryFormViewModel.ParentId),
            _ => string.Empty
        };
        ModelState.AddModelError(field, result.Message);
        return View(view, model);
    }
}
