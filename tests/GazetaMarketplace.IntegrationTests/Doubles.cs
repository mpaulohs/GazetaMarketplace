using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>Usuário atual controlável; nulo representa uma ação do sistema.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public int? UserId { get; set; }

    public string CorrelationId { get; set; } = "integration-test";
}
