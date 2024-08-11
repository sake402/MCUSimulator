namespace MCUSimulator.Core
{
    public interface IMCUFactory
    {
        public string Name { get; }
        public string Description { get; }
        MCU CreateMCU();
    }
}
