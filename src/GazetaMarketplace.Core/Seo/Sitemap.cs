using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace GazetaMarketplace.Core.Seo;

/// <summary>Uma página do mapa do site: o caminho (começa com <c>/</c>) e, se a página tem, a data da última mudança.</summary>
public sealed record SitemapEntry(string Path, DateTime? LastModified);

/// <summary>O mapa do site (<c>/sitemap.xml</c>, protocolo sitemaps.org): um só arquivo, no máximo <see cref="MaxUrls"/> endereços.</summary>
public static class SitemapDocument
{
    /// <summary>O limite do protocolo para um arquivo de mapa; o volume previsto da v1 está muito abaixo (decisão do Product Owner: um só arquivo).</summary>
    public const int MaxUrls = 50_000;

    public static string Build(string baseUrl, IEnumerable<SitemapEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(entries);

        string root = baseUrl.TrimEnd('/');
        StringBuilder text = new();
        using (XmlWriter writer = XmlWriter.Create(new StringWriter(text), new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false), OmitXmlDeclaration = true }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            int count = 0;
            foreach (SitemapEntry entry in entries)
            {
                if (++count > MaxUrls)
                {
                    break;
                }

                writer.WriteStartElement("url");
                writer.WriteElementString("loc", root + entry.Path);
                if (entry.LastModified is { } date)
                {
                    writer.WriteElementString("lastmod", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + text;
    }
}

/// <summary>O <c>/robots.txt</c>: libera o site público, fecha o painel e a API e aponta o mapa do site.</summary>
public static class RobotsText
{
    public static string Build(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        // A busca e os favoritos não são bloqueados aqui de propósito: o robô precisa abrir a página para ler o noindex dela
        return string.Join('\n',
            "User-agent: *",
            "Disallow: /painel",
            "Disallow: /api/",
            "Disallow: /favoritos/lista",
            "Sitemap: " + baseUrl.TrimEnd('/') + "/sitemap.xml",
            string.Empty);
    }
}
