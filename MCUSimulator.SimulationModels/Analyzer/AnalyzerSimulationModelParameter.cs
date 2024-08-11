using LivingThing.Core.Frameworks.Common;
using LivingThing.Core.Frameworks.Common.Attributes;
using MCUSimulator.Core.SimulationModel;
using System.ComponentModel;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public class AnalyzerSimulationModelParameter : BaseSimulationModelParameter
    {
        [Schema(Exclude = SchemaType.All & ~SchemaType.Json)]
        public Dictionary<string, string>? ProtocolParameters { get; set; }
    }
}
