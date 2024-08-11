using LivingThing.Core.Frameworks.Common;
using LivingThing.Core.Frameworks.Common.Attributes;
using MCUSimulator.Core.SimulationModel;
using System.ComponentModel;

namespace MCUSimulator.SimulationModels.Generator
{

    public class GeneratorSimulationModelParameter : BaseSimulationModelParameter
    {
        [Schema(InputType = InputType.Auto | InputType.AutoInline)]
        public IEnumerable<GeneratorSimulationModelParameterPattern>? Patterns { get; set; }
        public string? TimeBase { get; set; }
        public string? InitialState { get; set; }
        public bool Repeat { get; set; }
    }
}
