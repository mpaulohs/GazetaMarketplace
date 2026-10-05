using System.Collections.Generic;

namespace GazetaMarketplace.Web.Models;

/// <summary>O controle de páginas (<c>_Pagination</c>): a página atual, o total e os parâmetros que os links preservam.</summary>
public sealed record PaginationViewModel(int Page, int TotalPages, string BasePath, IReadOnlyList<KeyValuePair<string, string>> Query);
