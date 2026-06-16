namespace ActionItems.Sdk.Sharding.Clients;

public interface IWorkAreaClientIdProvider
{
    /// <summary>
    /// Returns an existing client id for the work area (if it exists externally), otherwise null.
    /// </summary>
    Task<string?> GetClientIdAsync(Guid workAreaId, CancellationToken cancellationToken = default);
}

