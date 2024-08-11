namespace MCUSimulator.Core
{
    public interface IReadableMemory : IMemory
    {
        int Value { get; }
        int OldValue { get; }
    }
}
