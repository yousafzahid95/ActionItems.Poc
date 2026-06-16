using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ActionItems.Sdk.Sharding.KeyVault;

/// <summary>
/// POC stand-in for Azure Key Vault. Reads secrets from a local JSON file.
/// Replace with an Azure Key Vault provider in production.
/// </summary>
public sealed class FileKeyVaultSecretProvider : IKeyVaultSecretProvider
{
    private readonly IReadOnlyDictionary<string, string> _secrets;

    public FileKeyVaultSecretProvider(IConfiguration configuration)
    {
        _secrets = LoadSecrets(configuration);
    }

    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(secretName, out var value))
        {
            return Task.FromResult(value);
        }

        throw new InvalidOperationException($"Key Vault secret '{secretName}' was not found.");
    }

    internal static IReadOnlyDictionary<string, string> LoadSecrets(IConfiguration configuration)
    {
        var inlineSecrets = configuration.GetSection("KeyVault:Secrets").Get<Dictionary<string, string>>();
        if (inlineSecrets is { Count: > 0 })
        {
            return inlineSecrets;
        }

        var secretsFile = configuration["KeyVault:SecretsFile"] ?? "keyvault-secrets.json";
        var path = Path.IsPathRooted(secretsFile)
            ? secretsFile
            : Path.Combine(AppContext.BaseDirectory, secretsFile);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Key Vault secrets file '{path}' was not found. Set KeyVault:SecretsFile or KeyVault:Secrets in configuration.");
        }

        var json = File.ReadAllText(path);
        var fromFile = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidOperationException($"Key Vault secrets file '{path}' is empty or invalid.");

        return fromFile;
    }
}
