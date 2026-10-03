namespace GazetaMarketplace.Web.Models
{
    /// <summary>Dados de um estado de página (vazio, sem resultado ou erro). Todo texto é codificado pelo Razor.</summary>
    public sealed class PageStateViewModel
    {
        public string Title { get; init; }

        public string Message { get; init; }

        /// <summary>Texto do botão de ação; cada partial tem um padrão quando não informado.</summary>
        public string ActionText { get; init; }

        /// <summary>Só caminhos locais viram link; qualquer outro valor é ignorado.</summary>
        public string ActionUrl { get; init; }

        /// <summary>O CorrelationId da requisição (mesmo valor do traceId e do log).</summary>
        public string ReferenceCode { get; init; }
    }

    /// <summary>Esqueleto de carregamento com altura reservada (NFR-03).</summary>
    public sealed class SkeletonViewModel
    {
        /// <summary>"card" (foto + linhas) ou "linha" (uma faixa de lista).</summary>
        public string Variant { get; init; } = "card";

        public int Count { get; init; } = 1;
    }
}
