using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Core.Showcase;

/// <summary>O envelope de lista paginada das respostas da API (NFR-22, <c>api-conventions.md</c>).</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
