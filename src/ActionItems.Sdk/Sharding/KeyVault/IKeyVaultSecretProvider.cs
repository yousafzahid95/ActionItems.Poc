namespace ActionItems.Sdk.Sharding.KeyVault;

public interface IKeyVaultSecretProvider
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
}
