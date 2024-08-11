namespace MCUSimulator.Core.SimulationModel
{
    public interface ISimulationModelInput : ISimulationModelIO
    {
        event EventHandler<MemoryChangedEventArg>? OnChanged;
        IReadableMemory? ConnectedMemory { get; }
        void Connect(IReadableMemory? memory);
    }
}
