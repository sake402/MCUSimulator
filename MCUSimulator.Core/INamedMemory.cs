namespace MCUSimulator.Core
{
    public interface INamedMemory : IMemory
    {
        string? Name { get; }
    }
}
