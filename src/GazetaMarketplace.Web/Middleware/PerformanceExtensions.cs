using System;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Desempenho das páginas (NFR-01, NFR-05): compressão das respostas públicas e cache longo dos arquivos estáticos com versão no endereço.
/// </summary>
public static class PerformanceExtensions
{
    /// <summary>O que vale cache de um ano: o endereço do arquivo muda quando o conteúdo muda (<c>?v=hash</c> do <c>asp-append-version</c>).</summary>
    public const string ImmutableCache = "public, max-age=31536000, immutable";

    /// <summary>
    /// Compressão (Brotli e gzip) de tudo que <b>não</b> é o painel. Motivo: as páginas do painel carregam o token antiforgery no HTML, e comprimir HTML com um segredo por HTTPS abre o ataque BREACH;
    /// o site público e a API não levam segredo na resposta. Os arquivos estáticos já saem pré-comprimidos do <c>MapStaticAssets</c> (a compressão é pulada quando a resposta já vem codificada).
    /// </summary>
    public static IServiceCollection AddPublicResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        return services;
    }

    public static IApplicationBuilder UsePublicResponseCompression(this IApplicationBuilder app) =>
        app.UseWhen(context => !IsPanelRequest(context), branch => branch.UseResponseCompression());

    /// <summary>
    /// Se o pedido é do painel. Uma resposta de erro do painel (404 sem corpo, falha) é reexecutada numa página pública (<c>/Home/Status/404</c>, <c>/Home/Error</c>) e o <c>Request.Path</c> passa a ser o dela;
    /// por isso também vale o caminho do pedido <b>original</b>, que o <c>UseStatusCodePagesWithReExecute</c> e o <c>UseExceptionHandler</c> guardam. Sem isso a página de erro de quem está logado saía comprimida (BREACH).
    /// </summary>
    internal static bool IsPanelRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/painel")
        || IsPanelPath(context.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath)
        || IsPanelPath(context.Features.Get<IExceptionHandlerPathFeature>()?.Path);

    private static bool IsPanelPath(string path) => path is not null && new PathString(path).StartsWithSegments("/painel");

    /// <summary>Arquivo estático pedido com <c>?v=</c> sai com cache de um ano e <c>immutable</c>; sem <c>v</c> continua revalidando (<c>no-cache</c> do <c>MapStaticAssets</c>).</summary>
    public static IApplicationBuilder UseVersionedStaticAssetCache(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            if (HttpMethods.IsGet(context.Request.Method) && context.Request.Query.ContainsKey("v"))
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == StatusCodes.Status200OK && context.GetEndpoint()?.Metadata.GetMetadata<StaticAssetDescriptor>() is not null)
                    {
                        context.Response.Headers.CacheControl = ImmutableCache;
                    }

                    return Task.CompletedTask;
                });
            }

            return next(context);
        });
}
