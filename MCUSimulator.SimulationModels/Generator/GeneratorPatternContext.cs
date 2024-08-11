using LivingThing.Core.Frameworks.Common.OneOf;
using LivingThing.Core.Frameworks.Common.String;

namespace MCUSimulator.SimulationModels.Generator
{
    public class GeneratorPatternContext
    {
        public IEnumerable<OneOf<double, BinaryPattern>> Patterns { get; set; } = default!;
        public int PatternIndex { get; set; }
        public int BitIndex { get; set; }
        public bool Waiting { get; set; }
    }
}
