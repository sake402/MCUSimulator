namespace MCUSimulator.Core
{
    public interface IReadWriteMemoryBitField : IReadableMemoryBitField, IWritableMemoryBitField
    {
        new int Value { get; set; }
        new int OldValue { get; set; }
    }

    public interface IReadWriteMemoryBitField<T> : IReadWriteMemoryBitField, IReadableMemoryBitField<T>, IWritableMemoryBitField<T>
    {
        new T TValue { get; set; }
    }
}
