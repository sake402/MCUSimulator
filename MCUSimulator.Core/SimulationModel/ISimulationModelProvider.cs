namespace MCUSimulator.Core.SimulationModel
{
    public interface ISimulationModelProvider
    {
        string Title { get; }
        string Description { get; }
        ISimulationModel Build();
    }
}
