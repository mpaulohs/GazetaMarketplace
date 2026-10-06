using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Assets;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Performance;

/// <summary>
/// NFR-05: o script de página sai com cache de um ano, então o endereço dele (<c>?v=</c>) tem de mudar quando muda qualquer arquivo que ele importa. Sem isso, um deploy que alterasse só um módulo compartilhado
/// (<c>api.js</c>, <c>favorites-ui.js</c>) deixava o visitante com a página antiga e o módulo novo por até um ano.
/// </summary>
[TestClass]
public sealed class ModuleVersionsTests
{
    private static readonly Regex RelativeImport = new(@"[""'](?<path>\.{1,2}/[^""'?#]+\.js)[""']", RegexOptions.CultureInvariant);

    private string _root;

    [TestInitialize]
    public void CreateFolder()
    {
        _root = Path.Combine(Path.GetTempPath(), "gazeta-modules-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_root, recursive: true);

    private void Write(string path, string content)
    {
        string full = Path.Combine(_root, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, content);
    }

    private ModuleVersions Versions(bool cache = false) => new(new PhysicalFileProvider(_root), cache);

    private void WriteSmallSite()
    {
        Write("/js/pages/page.js", "import { a } from \"../modules/a.js\";\nimport \"../modules/side.js\";\na();");
        Write("/js/modules/a.js", "import { b } from './b.js';\nexport function a() { b(); }");
        Write("/js/modules/b.js", "export function b() {}");
        Write("/js/modules/side.js", "export { c } from \"./c.js\";");
        Write("/js/modules/c.js", "export const c = 1;");
        Write("/js/modules/unrelated.js", "export const u = 1;");
    }

    [TestMethod]
    [DataRow("/js/pages/page.js", "page.js (o próprio arquivo)")]
    [DataRow("/js/modules/a.js", "a.js (importado direto)")]
    [DataRow("/js/modules/b.js", "b.js (importado pelo que a página importa)")]
    [DataRow("/js/modules/c.js", "c.js (por export ... from)")]
    public void ChangingAnyFileInTheImportChain_ChangesThePageVersion(string changed, string why)
    {
        WriteSmallSite();
        string before = Versions().For("/js/pages/page.js");

        Write(changed, File.ReadAllText(Path.Combine(_root, changed.TrimStart('/'))) + "\n// mudou");

        Assert.AreNotEqual(before, Versions().For("/js/pages/page.js"), why);
    }

    [TestMethod]
    public void ChangingAnUnrelatedModule_KeepsThePageVersion()
    {
        WriteSmallSite();
        string before = Versions().For("/js/pages/page.js");

        Write("/js/modules/unrelated.js", "export const u = 2;");

        Assert.AreEqual(before, Versions().For("/js/pages/page.js"));
    }

    [TestMethod]
    public void TheSameFiles_GiveTheSameVersion_AndTheTildePathMeansTheSameFile()
    {
        WriteSmallSite();

        Assert.AreEqual(Versions().For("/js/pages/page.js"), Versions().For("~/js/pages/page.js"));
        Assert.AreEqual(Versions().For("/js/pages/page.js"), Versions().For("/js/pages/page.js"));
    }

    [TestMethod]
    public void ModulesThatImportEachOther_DoNotLoopForever()
    {
        Write("/js/modules/x.js", "import './y.js';");
        Write("/js/modules/y.js", "import './x.js';");

        Assert.IsFalse(string.IsNullOrEmpty(Versions().For("/js/modules/x.js")));
    }

    [TestMethod]
    public void ADynamicImport_IsFollowedToo()
    {
        Write("/js/pages/lazy.js", "const m = await import(\"../modules/heavy.js\");");
        Write("/js/modules/heavy.js", "export const h = 1;");
        string before = Versions().For("/js/pages/lazy.js");

        Write("/js/modules/heavy.js", "export const h = 2;");

        Assert.AreNotEqual(before, Versions().For("/js/pages/lazy.js"));
    }

    [TestMethod]
    public void AMissingImport_StillGivesAVersion_AndAddingTheFileChangesIt()
    {
        Write("/js/pages/broken.js", "import './nao-existe.js';");
        string without = Versions().For("/js/pages/broken.js");

        Write("/js/modules/../pages/nao-existe.js", "export {};");

        Assert.AreNotEqual(without, Versions().For("/js/pages/broken.js"));
    }

    [TestMethod]
    public void WithTheCacheOn_TheVersionIsComputedOnce()
    {
        WriteSmallSite();
        ModuleVersions versions = Versions(cache: true);
        string first = versions.For("/js/pages/page.js");

        Write("/js/modules/b.js", "export function b() { /* mudou */ }");

        Assert.AreEqual(first, versions.For("/js/pages/page.js"), "em produção os arquivos não mudam durante a vida do processo");
        Assert.AreNotEqual(first, Versions().For("/js/pages/page.js"), "sem cache o arquivo novo é visto");
    }

    /// <summary>
    /// A prova contra o código de verdade: para cada <c>import</c> relativo de cada arquivo de <c>wwwroot/js</c> (achado aqui por outra expressão, de propósito mais simples que a do produto),
    /// mudar o arquivo importado muda a versão de quem o importa.
    /// </summary>
    [TestMethod]
    public void InTheRealScripts_EveryImportedFile_ChangesTheVersionOfWhoImportsIt()
    {
        string real = RepositoryRoot.Wwwroot("js");
        foreach (string file in Directory.GetFiles(real, "*.js", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(real, file).Replace(Path.DirectorySeparatorChar, '/');
            Write("/js/" + relative, File.ReadAllText(file));
        }

        List<string> wrong = [];
        int checkedImports = 0;
        foreach (string importer in Directory.GetFiles(Path.Combine(_root, "js"), "*.js", SearchOption.AllDirectories))
        {
            string importerPath = "/js/" + Path.GetRelativePath(Path.Combine(_root, "js"), importer).Replace(Path.DirectorySeparatorChar, '/');
            foreach (Match match in RelativeImport.Matches(File.ReadAllText(importer)))
            {
                string target = new Uri(new Uri("http://x" + importerPath), match.Groups["path"].Value).AbsolutePath;
                string targetFile = Path.Combine(_root, target.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(targetFile))
                {
                    wrong.Add($"{importerPath} importa {target}, que não existe");
                    continue;
                }

                string original = File.ReadAllText(targetFile);
                string before = Versions().For(importerPath);
                File.WriteAllText(targetFile, original + "\n// mudou");
                if (before == Versions().For(importerPath))
                {
                    wrong.Add($"{importerPath}: mudar {target} não mudou a versão");
                }

                File.WriteAllText(targetFile, original);
                checkedImports++;
            }
        }

        Assert.IsGreaterThan(10, checkedImports, "a varredura devia achar os imports reais");
        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task EveryPageScript_ComesOutOfTheSiteWithAVersion_AndNoneWithTheOldFormat()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        foreach (string url in new[] { "/", "/busca", "/favoritos" })
        {
            string html = await visitor.GetStringAsync(url);
            MatchCollection scripts = Regex.Matches(html, @"<script[^>]*type=""module""[^>]*src=""(?<src>/js/pages/[^""]+)""");
            Assert.IsNotEmpty(scripts.Cast<Match>().ToList(), $"{url} devia ter ao menos um script de página");
            foreach (Match script in scripts)
            {
                StringAssert.Matches(script.Groups["src"].Value, new Regex(@"^/js/pages/[a-z-]+\.js\?v=[A-Za-z0-9_-]{16}$"), $"{url}: endereço do script de página");
            }
        }
    }
}
