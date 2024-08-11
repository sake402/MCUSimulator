using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.Icons;
using MCUSimulator.Core.SimulationModel;
using Microsoft.AspNetCore.Components;

namespace  MCUSimulator.SimulationModels.LED
{
    public class LEDSimulationModelProvider : ISimulationModelViewProvider
    {
        public string Title => "LED";
        public string Description => "Model a light emiting diode";
        public IconDescriptor Icon => MaterialDesignIconNames.LedOutline;

        public ISimulationModel Build()
        {
            return new SimulationModelBuilder()
                .DefineInput("IN")
                .Build<LEDSimulationModel>(this);
        }

        public RenderFragment GetView(ISimulationModel model)
        {
            return (builder) =>
            {
                builder.OpenComponent<LEDView>(1);
                builder.AddComponentParameter(2, nameof(LEDView.Model), (LEDSimulationModel)model);
                builder.CloseComponent();
            };
        }
    }
}
