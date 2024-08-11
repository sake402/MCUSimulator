using LivingThing.Core.Frameworks.Common.Attributes;

namespace MCUSimulator.SimulationModels.Analyzer.Protocols
{
    public enum AsyncSerialAnalyzerProtocolDecoderParameterStopBits
    {
        [Schema(Title = "1")]
        One,
        [Schema(Title = "1.5")]
        OnePointFive,
        [Schema(Title = "2")]
        Two
    }
}
