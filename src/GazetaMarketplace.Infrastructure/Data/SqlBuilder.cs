using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>SQL pronto e seus parâmetros, para entregar ao Dapper.</summary>
public sealed record SqlQuery(string Sql, DynamicParameters Parameters);

/// <summary>
/// Monta consultas Dapper de filtro dinâmico sem concatenar valores (ADR-004): só fragmentos fixos,
/// valores sempre como parâmetros nomeados e ordenação só por colunas de uma lista permitida.
/// A validação dos fragmentos é uma segunda barreira; a primeira é nunca passar texto de usuário como fragmento.
/// </summary>
public sealed partial class SqlBuilder
{
    private readonly List<string> _joins = [];
    private readonly List<string> _filters = [];
    private readonly DynamicParameters _parameters = new();
    private readonly HashSet<string> _parameterNames = new(StringComparer.Ordinal);
    private string _select;
    private string _from;
    private string _ordering;
    private int? _offset;
    private int? _take;

    public SqlBuilder Select(string columns)
    {
        _select = Validate(columns);
        return this;
    }

    public SqlBuilder From(string table)
    {
        _from = Validate(table);
        return this;
    }

    public SqlBuilder Join(string fragment)
    {
        _joins.Add(Validate(fragment));
        return this;
    }

    public SqlBuilder Where(string fragment)
    {
        _filters.Add(Validate(fragment));
        return this;
    }

    /// <summary>Filtro único de "somente publicados" (<see cref="SqlFragments.OnlyPublished"/>).</summary>
    public SqlBuilder OnlyPublished()
    {
        Where(SqlFragments.OnlyPublished);
        return Parameter(SqlFragments.PublishedStatusParameter, SqlFragments.PublishedStatus);
    }

    public SqlBuilder Parameter(string name, object value)
    {
        if (string.IsNullOrEmpty(name) || !IsValidName().IsMatch(name))
        {
            throw new ArgumentException("Nome de parâmetro inválido.", nameof(name));
        }

        _parameters.Add(name, value);
        _parameterNames.Add(name);
        return this;
    }

    /// <summary>
    /// Ordena por uma chave da lista permitida (chave → coluna). Chave fora da lista cai na ordem padrão;
    /// direção diferente de asc/desc vira ASC. O texto recebido nunca entra no SQL.
    /// </summary>
    public SqlBuilder OrderBy(string key, string direction, IReadOnlyDictionary<string, string> allowed, string defaultOrder)
    {
        ArgumentNullException.ThrowIfNull(allowed);

        if (key is not null && allowed.TryGetValue(key, out string column))
        {
            string sentido = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            _ordering = Validate(column) + " " + sentido;
        }
        else
        {
            _ordering = Validate(defaultOrder);
        }

        return this;
    }

    public SqlBuilder Page(int page, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 100);

        _offset = (page - 1) * size;
        _take = size;
        return this;
    }

    public SqlQuery Build()
    {
        if (_select is null || _from is null)
        {
            throw new InvalidOperationException("A consulta precisa de Select e From.");
        }

        if (_offset is not null && _ordering is null)
        {
            throw new InvalidOperationException("Paginação exige ordenação.");
        }

        StringBuilder sql = new();
        sql.Append("SELECT ").Append(_select).Append(" FROM ").Append(_from);
        foreach (string join in _joins)
        {
            sql.Append(' ').Append(join);
        }

        if (_filters.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", _filters));
        }

        if (_ordering is not null)
        {
            sql.Append(" ORDER BY ").Append(_ordering);
        }

        DynamicParameters parameters = new(_parameters);
        if (_offset is not null)
        {
            sql.Append(" OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY");
            parameters.Add("Offset", _offset.Value);
            parameters.Add("Take", _take.Value);
        }

        string text = sql.ToString();
        string[] withoutValue = Placeholder().Matches(text).Select(m => m.Groups[1].Value)
            .Where(n => !_parameterNames.Contains(n) && n is not ("Offset" or "Take"))
            .Distinct()
            .ToArray();
        if (withoutValue.Length > 0)
        {
            throw new InvalidOperationException("Parâmetros sem valor: " + string.Join(", ", withoutValue));
        }

        return new SqlQuery(text, parameters);
    }

    // Aceita só identificadores, pontos, colchetes, vírgulas, operadores e @parâmetros. Recusa aspas,
    // ponto e vírgula, comentários e números soltos (um valor concatenado seria um literal).
    private static string Validate(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
        {
            throw new ArgumentException("Fragmento de SQL vazio.", nameof(fragment));
        }

        bool dangerous = !AllowedCharacters().IsMatch(fragment)
            || fragment.Contains("--", StringComparison.Ordinal)
            || fragment.Contains("/*", StringComparison.Ordinal)
            || LooseNumber().IsMatch(fragment);
        if (dangerous)
        {
            throw new ArgumentException("Fragmento de SQL não permitido: use parâmetros para valores.", nameof(fragment));
        }

        return fragment.Trim();
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\[\]\.\s,=<>!()@*+\-]+$")]
    private static partial Regex AllowedCharacters();

    [GeneratedRegex(@"(?<![\w@\.\[])\d+(?!\w)")]
    private static partial Regex LooseNumber();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IsValidName();

    [GeneratedRegex(@"@([A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex Placeholder();
}
