using System;
using System.Linq;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Arquitetura;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DapperTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void ReadRepositories_ImplementamInterfacesDoCore()
    {
        Type[] repositorios = typeof(AppDbContext).Assembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("ReadRepository", StringComparison.Ordinal))
            .ToArray();

        foreach (Type repositorio in repositorios)
        {
            bool implementaInterfaceDoCore = repositorio.GetInterfaces().Any(i =>
                i.Name.EndsWith("ReadRepository", StringComparison.Ordinal)
                && i.Assembly == typeof(IAuditLog).Assembly);
            Assert.IsTrue(implementaInterfaceDoCore, repositorio.Name + " deve implementar uma interface *ReadRepository do Core");
        }
    }
}
