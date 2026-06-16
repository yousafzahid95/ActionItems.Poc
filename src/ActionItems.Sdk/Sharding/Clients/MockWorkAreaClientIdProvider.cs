using Microsoft.Extensions.Configuration;

namespace ActionItems.Sdk.Sharding.Clients;

/// <summary>
/// POC stand-in for an external service that returns ClientId given a WorkAreaId.
/// </summary>
public sealed class MockWorkAreaClientIdProvider : IWorkAreaClientIdProvider
{
    private readonly IReadOnlyDictionary<Guid, string> _workAreaToClient;

    public MockWorkAreaClientIdProvider(IConfiguration configuration)
    {
        var map = configuration.GetSection("MockExternalService:WorkAreaToClientId")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

        _workAreaToClient = map
            .Select(kvp => new { Key = Guid.Parse(kvp.Key), Value = kvp.Value })
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public Task<string?> GetClientIdAsync(Guid workAreaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_workAreaToClient.TryGetValue(workAreaId, out var clientId) ? clientId : null);
    }
}

