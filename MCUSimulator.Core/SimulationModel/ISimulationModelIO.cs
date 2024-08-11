using LivingThing.Core.Frameworks.Common.Data;

namespace MCUSimulator.Core.SimulationModel
{
    public interface ISimulationModelIO : INamed
    {
        new string Name { get; }
    }
}
