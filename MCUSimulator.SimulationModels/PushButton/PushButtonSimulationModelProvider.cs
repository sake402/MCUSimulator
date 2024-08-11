using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.Icons;
using MCUSimulator.Core.SimulationModel;
using Microsoft.AspNetCore.Components;

namespace  MCUSimulator.SimulationModels.PushButton
{
    public class PushButtonSimulationModelProvider : ISimulationModelViewProvider
    {
        public string Title => "Push Button";
        public string Description => "Model a single output button";
        public IconDescriptor Icon => MaterialDesignIconNames.ToggleSwitch;

        public ISimulationModel Build()
        {
            return new SimulationModelBuilder()
                .DefineOutput("OUT")
                .Build<PushButtonSimulationModel>(this);
        }

        public RenderFragment GetView(ISimulationModel model)
        {
            return (builder) =>
            {
                builder.OpenComponent<PushButtonView>(1);
                builder.AddComponentParameter(2, nameof(PushButtonView.Model), (PushButtonSimulationModel)model);
                builder.CloseComponent();
            };
        }
    }
}
