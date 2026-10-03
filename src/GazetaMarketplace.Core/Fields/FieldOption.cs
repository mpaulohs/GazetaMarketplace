using System;
using System.Collections.Generic;
using System.Linq;

namespace GazetaMarketplace.Core.Fields;

/// <summary>Uma opção de lista: o id é o que vai para o banco; o rótulo é o que a pessoa lê.</summary>
public sealed record FieldOption(int Id, string Label);

/// <summary>Uma lista de opções com nome estável (o do GazetaOnline, quando herdada). Imutável.</summary>
public sealed class FieldList
{
    private readonly Dictionary<int, FieldOption> _byId;

    public FieldList(string name, IEnumerable<FieldOption> options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);
        Name = name;
        Options = [.. options];
        _byId = Options.ToDictionary(o => o.Id);
    }

    public string Name { get; }

    /// <summary>As opções na ordem em que aparecem na tela.</summary>
    public IReadOnlyList<FieldOption> Options { get; }

    public FieldOption Find(int id) => _byId.GetValueOrDefault(id);

    public bool Contains(int id) => _byId.ContainsKey(id);
}
