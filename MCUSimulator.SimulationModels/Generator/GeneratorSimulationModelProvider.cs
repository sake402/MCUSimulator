using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.Icons;
using MCUSimulator.Core.SimulationModel;
using Microsoft.AspNetCore.Components;

namespace MCUSimulator.SimulationModels.Generator
{
    public class GeneratorSimulationModelProvider : ISimulationModelViewProvider
    {
        public string Title => "Generator";
        public string Description => "Generate digital signals";
        public IconDescriptor Icon => MaterialDesignIconNames.ClockDigital;

        public ISimulationModel Build()
        {
            int n = 8;
            var mb = new SimulationModelBuilder();
            for (int i = 0; i < n; i++)
                mb.DefineOutput("OUT" + (i + 1));
            return mb.Build<GeneratorSimulationModel>(this);
        }

        public RenderFragment GetView(ISimulationModel model)
        {
            return (builder) =>
            {
                builder.OpenComponent<GeneratorView>(1);
                builder.AddComponentParameter(2, nameof(GeneratorView.Model), (GeneratorSimulationModel)model);
                builder.CloseComponent();
            };
        }
    }
}
