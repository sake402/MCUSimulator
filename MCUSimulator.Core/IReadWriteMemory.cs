namespace MCUSimulator.Core
{
    public interface IReadWriteMemory : IReadableMemory, IWritableMemory
    {
        new int Value { get; set; }
    }
}
