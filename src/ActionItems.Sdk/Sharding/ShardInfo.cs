namespace ActionItems.Sdk.Sharding;

public sealed record ShardInfo(
    Guid WorkAreaId,
    string ShardKey,
    string ConnectionString,
    ApplicationIntent ApplicationIntent);
