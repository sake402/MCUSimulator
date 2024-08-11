namespace MCUSimulator.Core
{
    public interface IWritableMemoryBitField : IMemoryBitField, IWritableMemory
    {
    }

    public interface IWritableMemoryBitField<T> : IWritableMemoryBitField, IMemoryBitField<T>
    {
        T TValue { set; }
    }
}
