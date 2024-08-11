using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.Icons;
using MCUSimulator.Core.SimulationModel;
using Microsoft.AspNetCore.Components;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public class AnalyzerSimulationModelProvider : ISimulationModelViewProvider
    {
        public string Title => "Logic Analyzer";
        public string Description => "Collect and analyze streams of logic bits";
        public IconDescriptor Icon => MaterialDesignIconNames.ClockDigital;

        public ISimulationModel Build()
        {
            int n = 8;
            var mb = new SimulationModelBuilder();
            for (int i = 0; i < n; i++)
                mb.DefineInput("IN" + (i + 1));
            return mb.Build<AnalyzerSimulationModel>(this);
        }

        public RenderFragment GetView(ISimulationModel model)
        {
            return (builder) =>
            {
                builder.OpenComponent<AnalyzerView>(1);
                builder.AddComponentParameter(2, nameof(AnalyzerView.Model), (AnalyzerSimulationModel)model);
                builder.CloseComponent();
            };
        }
    }
}
