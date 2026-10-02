namespace GazetaMarketplace.Web.Models
{
    /// <summary>Dados de um estado de página (vazio, sem resultado ou erro). Todo texto é codificado pelo Razor.</summary>
    public sealed class EstadoPaginaViewModel
    {
        public string Titulo { get; init; }

        public string Mensagem { get; init; }

        /// <summary>Texto do botão de ação; cada partial tem um padrão quando não informado.</summary>
        public string AcaoTexto { get; init; }

        /// <summary>Só caminhos locais viram link; qualquer outro valor é ignorado.</summary>
        public string AcaoUrl { get; init; }

        /// <summary>O CorrelationId da requisição (mesmo valor do traceId e do log).</summary>
        public string CodigoReferencia { get; init; }
    }

    /// <summary>Esqueleto de carregamento com altura reservada (NFR-03).</summary>
    public sealed class EsqueletoViewModel
    {
        /// <summary>"card" (foto + linhas) ou "linha" (uma faixa de lista).</summary>
        public string Variante { get; init; } = "card";

        public int Quantidade { get; init; } = 1;
    }
}
