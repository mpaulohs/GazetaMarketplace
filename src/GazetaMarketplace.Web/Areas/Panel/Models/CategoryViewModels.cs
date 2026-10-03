using System.Collections.Generic;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Uma linha da árvore de categorias, na ordem em que a árvore é desenhada (categoria, depois as filhas).</summary>
public sealed class CategoryRowViewModel
{
    public int Id { get; init; }

    public string Name { get; init; }

    /// <summary>1 = categoria principal, 2 = subcategoria, 3 = subcategoria de terceiro nível.</summary>
    public int Depth { get; init; }

    /// <summary>Nome do pai direto; nulo na categoria principal. Serve ao leitor de tela ("Subcategoria de …").</summary>
    public string ParentName { get; init; }

    public int AdsCount { get; init; }

    /// <summary>Primeira entre as irmãs: o botão "Mover para cima" fica inativo.</summary>
    public bool IsFirst { get; init; }

    /// <summary>Última entre as irmãs: o botão "Mover para baixo" fica inativo.</summary>
    public bool IsLast { get; init; }

    /// <summary>Tem campos específicos (A7 b): só renomeia. O "Excluir" vai direto à mensagem, sem janela de confirmação.</summary>
    public bool IsProtected { get; init; }
}

public sealed class CategoriesIndexViewModel
{
    public IReadOnlyList<CategoryRowViewModel> Rows { get; init; } = [];

    /// <summary>Aviso de sucesso da última ação (<c>role="status"</c>).</summary>
    public string Message { get; init; }

    /// <summary>Bloqueio ou erro da última ação (<c>role="alert"</c>), por exemplo uma exclusão recusada.</summary>
    public string Alert { get; init; }

    /// <summary>Id do botão que deve receber o foco depois de mover, para quem usa teclado ou leitor de tela não perder o lugar.</summary>
    public string FocusId { get; init; }
}

/// <summary>Uma opção da lista "Categoria pai".</summary>
public sealed record ParentOption(int? Id, string Label);

/// <summary>Formulário de categoria. Em "nova" escolhe-se o pai; em "editar" só o nome muda (renomear).</summary>
public sealed class CategoryFormViewModel
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int? ParentId { get; set; }

    /// <summary>Opções da lista de pais (principais e subcategorias; nunca as de terceiro nível). Vazia em "editar".</summary>
    public IReadOnlyList<ParentOption> Parents { get; set; } = [];

    /// <summary>Em "editar": nome do pai, só para mostrar.</summary>
    public string ParentName { get; set; }
}

public sealed class ConfirmDeleteViewModel
{
    public int Id { get; init; }

    public string Name { get; init; }
}
