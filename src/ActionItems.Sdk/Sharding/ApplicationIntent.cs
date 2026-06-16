namespace ActionItems.Sdk.Sharding;

/// <summary>
/// Routes shard connections to read replicas or the master database.
/// In production (SQL Server), connection strings from Key Vault include ApplicationIntent=ReadOnly or ReadWrite.
/// </summary>
public enum ApplicationIntent
{
    Read,
    ReadWrite
}
