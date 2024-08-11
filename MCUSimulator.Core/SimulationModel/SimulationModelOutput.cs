namespace MCUSimulator.Core.SimulationModel
{
    class SimulationModelOutput : ISimulationModelOutput
    {
        public SimulationModelOutput(string name)
        {
            Name = name;
        }

        public IWritableMemory? ConnectedMemory { get; private set; }
        public string Name { get; }
        public void Connect(IWritableMemory? memory)
        {
            ConnectedMemory = memory;
        }
    }
}
