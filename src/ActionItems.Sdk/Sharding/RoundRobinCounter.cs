namespace ActionItems.Sdk.Sharding;

public sealed class RoundRobinCounter : IRoundRobinCounter
{
    private int _index;

    public int Next() => Interlocked.Increment(ref _index);
}
