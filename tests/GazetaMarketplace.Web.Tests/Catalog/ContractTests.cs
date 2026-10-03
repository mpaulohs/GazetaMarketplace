using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Controllers.Api;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Catalog;

/// <summary>O controlador e o contrato (<c>architecture/api/openapi.yaml</c>) dizem a mesma coisa: rotas, operações e o parâmetro <c>kind</c> obrigatório.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ContractTests
#pragma warning restore CA1515
{
    private static string OpenApi() => File.ReadAllText(RepositoryHelper.Project("architecture/api/openapi.yaml"));

    private static string[] Routes() =>
    [
        .. typeof(VehicleCatalogController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.GetCustomAttribute<HttpGetAttribute>()?.Template)
            .Where(t => t is not null)
            .Select(t => "/api/v1/vehicle-catalog/" + Regex.Replace(t, @"\{(\w+)(:[^}]*)?\}", "{$1}"))
    ];

    [TestMethod]
    public void CadaRotaDoControlador_EstaNoOpenApi()
    {
        string yaml = OpenApi();

        Assert.HasCount(4, Routes());
        foreach (string route in Routes())
        {
            StringAssert.Contains(yaml, "  " + route + ":", "rota ausente do openapi.yaml: " + route);
        }
    }

    [TestMethod]
    public void AsQuatroOperacoes_ExigemOKind()
    {
        string yaml = OpenApi();

        foreach (string operation in new[] { "listVehicleBrands", "listVehicleModels", "listVehicleModelYears", "listVehicleVersions" })
        {
            int start = yaml.IndexOf("operationId: " + operation, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, start, operation);
            int end = yaml.IndexOf("  /api/", start, StringComparison.Ordinal);
            string block = yaml[start..(end < 0 ? yaml.Length : end)];
            StringAssert.Contains(block, "#/components/parameters/VehicleKind", operation + " precisa do parâmetro kind");
            StringAssert.Contains(block, "'400'", operation + " precisa documentar o 400");
        }

        Match kind = Regex.Match(yaml, @"VehicleKind:\s+name: kind\s+in: query\s+required: true");
        Assert.IsTrue(kind.Success, "kind é parâmetro de consulta obrigatório");
    }

    [TestMethod]
    public void Controlador_EPublico_ENaoGravaNada()
    {
        Assert.IsNull(typeof(VehicleCatalogController).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());
        Assert.IsEmpty(typeof(VehicleCatalogController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute)).ToArray());
    }
}
