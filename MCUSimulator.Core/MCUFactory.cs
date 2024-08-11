namespace MCUSimulator.Core
{
    public class MCUFactory<TMCU> : IMCUFactory
        where TMCU : MCU, new()
    {
        public MCUFactory(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }

        public MCU CreateMCU()
        {
            return new TMCU();
        }
    }
}
