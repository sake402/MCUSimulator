namespace MCUSimulator.Core.SimulationModel
{
    class SimulationModelInput : ISimulationModelInput
    {
        public SimulationModelInput(string name)
        {
            Name = name;
        }

        public IReadableMemory? ConnectedMemory { get; private set; }
        public string Name { get; }

        public event EventHandler<MemoryChangedEventArg>? OnChanged;

        void MemoryChanged(object? sender, MemoryChangedEventArg arg)
        {
            OnChanged?.Invoke(this, arg);
        }

        public void Connect(IReadableMemory? memory)
        {
            if (ConnectedMemory != null)
                ConnectedMemory.OnChanged -= MemoryChanged;
            ConnectedMemory = memory;
            if (ConnectedMemory != null)
                ConnectedMemory.OnChanged += MemoryChanged;
        }
    }
}
