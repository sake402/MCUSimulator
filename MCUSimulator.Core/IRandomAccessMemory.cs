namespace MCUSimulator.Core
{
    public interface IRandomAccessMemory : IReadWriteMemory
    {
        int Address { get; }
    }
}
