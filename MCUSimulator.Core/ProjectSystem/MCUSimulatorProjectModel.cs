namespace MCUSimulator.Core.ProjectSystem
{
    public class MCUSimulatorProjectModel
    {
        public string MCUFactory { get; set; } = default!;
        public string ProgramFile { get; set; } = default!;
        public string? MCUParameter { get; set; }
        public IEnumerable<MCUSimulatorComponentModel>? SimulationModels { get; set; }
        public List<int>? BreakPoints { get; set; }
        public List<MCUSimulatorWatch>? Watches { get; set; }
    }
}
