namespace MCUSimulator.Core.ProjectSystem
{
    public class MCUSimulatorComponentModel
    {
        public string? Name { get; set; }
        public string? Provider { get; set; }
        public Dictionary<string, string?>? Inputs { get; set; }
        public Dictionary<string, string?>? Outputs { get; set; }
        public string? Parameters { get; set; }
    }
}
