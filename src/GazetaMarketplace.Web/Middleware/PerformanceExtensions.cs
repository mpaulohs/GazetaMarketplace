using System;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Desempenho das páginas (NFR-01, NFR-05): compressão das respostas públicas e cache longo dos arquivos estáticos com versão no endereço.
/// </summary>
internal static class PerformanceExtensions
{
    /// <summary>O que vale cache de um ano: o endereço do arquivo muda quando o conteúdo muda (<c>?v=hash</c> do <c>asp-append-version</c>).</summary>
    internal const string ImmutableCache = "public, max-age=31536000, immutable";

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
        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/painel"), branch => branch.UseResponseCompression());

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
