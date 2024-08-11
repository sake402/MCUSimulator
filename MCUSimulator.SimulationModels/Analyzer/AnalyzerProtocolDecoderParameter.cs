using LivingThing.Core.Frameworks.Common.Attributes;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public abstract class AnalyzerProtocolDecoderParameter
    {
        [Schema(Order = int.MinValue)]
        public bool Enabled { get; set; }
    }
}
