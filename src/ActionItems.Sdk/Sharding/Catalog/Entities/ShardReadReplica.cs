namespace ActionItems.Sdk.Sharding.Catalog.Entities;

public class ShardReadReplica
{
    public int Id { get; set; }
    public string ShardKey { get; set; } = string.Empty;
    public string KeyVaultSecretName { get; set; } = string.Empty;
    public int Order { get; set; }
}

