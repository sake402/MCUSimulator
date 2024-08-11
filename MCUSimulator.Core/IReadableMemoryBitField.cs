namespace MCUSimulator.Core
{
    public interface IReadableMemoryBitField : IMemoryBitField, IReadableMemory
    {
    }

    public interface IReadableMemoryBitField<T> : IReadableMemoryBitField, IMemoryBitField<T>
    {
        T TValue { get; }
    }
}
