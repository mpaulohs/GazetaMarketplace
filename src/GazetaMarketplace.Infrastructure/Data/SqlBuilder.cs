using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>SQL pronto e seus parâmetros, para entregar ao Dapper.</summary>
public sealed record ConsultaSql(string Sql, DynamicParameters Parametros);

/// <summary>
/// Monta consultas Dapper de filtro dinâmico sem concatenar valores (ADR-004): só fragmentos fixos,
/// valores sempre como parâmetros nomeados e ordenação só por colunas de uma lista permitida.
/// A validação dos fragmentos é uma segunda barreira; a primeira é nunca passar texto de usuário como fragmento.
/// </summary>
public sealed partial class SqlBuilder
{
    private readonly List<string> _joins = [];
    private readonly List<string> _filtros = [];
    private readonly DynamicParameters _parametros = new();
    private readonly HashSet<string> _nomesDeParametros = new(StringComparer.Ordinal);
    private string _select;
    private string _from;
    private string _ordenacao;
    private int? _offset;
    private int? _take;

    public SqlBuilder Select(string colunas)
    {
        _select = Validar(colunas);
        return this;
    }

    public SqlBuilder From(string tabela)
    {
        _from = Validar(tabela);
        return this;
    }

    public SqlBuilder Join(string fragmento)
    {
        _joins.Add(Validar(fragmento));
        return this;
    }

    public SqlBuilder Where(string fragmento)
    {
        _filtros.Add(Validar(fragmento));
        return this;
    }

    /// <summary>Filtro único de "somente publicados" (<see cref="SqlFragments.SomentePublicados"/>).</summary>
    public SqlBuilder SomentePublicados()
    {
        Where(SqlFragments.SomentePublicados);
        return Parametro(SqlFragments.ParametroStatusPublicado, SqlFragments.StatusPublicado);
    }

    public SqlBuilder Parametro(string nome, object valor)
    {
        if (string.IsNullOrEmpty(nome) || !NomeValido().IsMatch(nome))
        {
            throw new ArgumentException("Nome de parâmetro inválido.", nameof(nome));
        }

        _parametros.Add(nome, valor);
        _nomesDeParametros.Add(nome);
        return this;
    }

    /// <summary>
    /// Ordena por uma chave da lista permitida (chave → coluna). Chave fora da lista cai na ordem padrão;
    /// direção diferente de asc/desc vira ASC. O texto recebido nunca entra no SQL.
    /// </summary>
    public SqlBuilder OrderBy(string chave, string direcao, IReadOnlyDictionary<string, string> permitidas, string ordemPadrao)
    {
        ArgumentNullException.ThrowIfNull(permitidas);

        if (chave is not null && permitidas.TryGetValue(chave, out string coluna))
        {
            string sentido = string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            _ordenacao = Validar(coluna) + " " + sentido;
        }
        else
        {
            _ordenacao = Validar(ordemPadrao);
        }

        return this;
    }

    public SqlBuilder Paginar(int pagina, int tamanho)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanho, 100);

        _offset = (pagina - 1) * tamanho;
        _take = tamanho;
        return this;
    }

    public ConsultaSql Build()
    {
        if (_select is null || _from is null)
        {
            throw new InvalidOperationException("A consulta precisa de Select e From.");
        }

        if (_offset is not null && _ordenacao is null)
        {
            throw new InvalidOperationException("Paginação exige ordenação.");
        }

        StringBuilder sql = new();
        sql.Append("SELECT ").Append(_select).Append(" FROM ").Append(_from);
        foreach (string join in _joins)
        {
            sql.Append(' ').Append(join);
        }

        if (_filtros.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", _filtros));
        }

        if (_ordenacao is not null)
        {
            sql.Append(" ORDER BY ").Append(_ordenacao);
        }

        DynamicParameters parametros = new(_parametros);
        if (_offset is not null)
        {
            sql.Append(" OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY");
            parametros.Add("Offset", _offset.Value);
            parametros.Add("Take", _take.Value);
        }

        string texto = sql.ToString();
        string[] semValor = Placeholder().Matches(texto).Select(m => m.Groups[1].Value)
            .Where(n => !_nomesDeParametros.Contains(n) && n is not ("Offset" or "Take"))
            .Distinct()
            .ToArray();
        if (semValor.Length > 0)
        {
            throw new InvalidOperationException("Parâmetros sem valor: " + string.Join(", ", semValor));
        }

        return new ConsultaSql(texto, parametros);
    }

    // Aceita só identificadores, pontos, colchetes, vírgulas, operadores e @parâmetros. Recusa aspas,
    // ponto e vírgula, comentários e números soltos (um valor concatenado seria um literal).
    private static string Validar(string fragmento)
    {
        if (string.IsNullOrWhiteSpace(fragmento))
        {
            throw new ArgumentException("Fragmento de SQL vazio.", nameof(fragmento));
        }

        bool perigoso = !CaracteresPermitidos().IsMatch(fragmento)
            || fragmento.Contains("--", StringComparison.Ordinal)
            || fragmento.Contains("/*", StringComparison.Ordinal)
            || NumeroSolto().IsMatch(fragmento);
        if (perigoso)
        {
            throw new ArgumentException("Fragmento de SQL não permitido: use parâmetros para valores.", nameof(fragmento));
        }

        return fragmento.Trim();
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\[\]\.\s,=<>!()@*+\-]+$")]
    private static partial Regex CaracteresPermitidos();

    [GeneratedRegex(@"(?<![\w@\.\[])\d+(?!\w)")]
    private static partial Regex NumeroSolto();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex NomeValido();

    [GeneratedRegex(@"@([A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex Placeholder();
}
