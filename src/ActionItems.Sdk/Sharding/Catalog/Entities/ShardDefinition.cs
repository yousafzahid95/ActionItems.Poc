namespace ActionItems.Sdk.Sharding.Catalog.Entities;

public class ShardDefinition
{
    public string ShardKey { get; set; } = string.Empty;

    /// <summary>Key Vault secret for the master (read/write) connection. Also used for reads when no replicas are configured.</summary>
    public string MasterKeyVaultSecretName { get; set; } = string.Empty;
}
