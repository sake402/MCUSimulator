using LivingThing.Core.Frameworks.Common.Data;
using MCUSimulator.Core.SimulationModel;
using Microsoft.AspNetCore.Components;

namespace MCUSimulator.SimulationModels
{
    public interface ISimulationModelViewProvider : ISimulationModelProvider, IDisplayable
    {
        RenderFragment GetView(ISimulationModel model);
    }
}
