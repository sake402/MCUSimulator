using LivingThing.Core.Frameworks.Common;
using LivingThing.Core.Frameworks.Common.Attributes;

namespace MCUSimulator.SimulationModels.Generator
{
    public struct GeneratorSimulationModelParameterPattern
    {
        [Schema(InputType = InputType.TextArea)]
        public string? Pattern { get; set; }

        public override string ToString()
        {
            return Pattern ?? "";
        }
    }
}
