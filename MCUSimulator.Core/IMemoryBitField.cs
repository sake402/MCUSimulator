namespace MCUSimulator.Core
{
    public interface IMemoryBitField : IMemory, INamedMemory
    {
        IMemory Parent { get; }
        Type Type { get; }
        int BitStart { get; }
        int FromValue(int value);
    }

    public interface IMemoryBitField<T> : IMemoryBitField
    {
    }
}
