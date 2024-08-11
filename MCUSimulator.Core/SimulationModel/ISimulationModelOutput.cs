namespace MCUSimulator.Core.SimulationModel
{
    public interface ISimulationModelOutput : ISimulationModelIO
    {
        IWritableMemory? ConnectedMemory { get; }
        void Connect(IWritableMemory? memory);

    }
}
