using LivingThing.Core.Frameworks.Client;
using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Common.String;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public partial class AnalyzerView : BaseComponent
    {
        [ServiceInject] public IEnumerable<IAnalyzerProtocolDecoder> Decoders { get; set; } = default!;
        [Parameter] public AnalyzerSimulationModel Model { get; set; } = default!;

        async Task EditDecoder(IAnalyzerProtocolDecoder decoder)
        {
            var model = await UnitOfWork.UI.Overlay.Form<ViewConfiguration, AnalyzerProtocolDecoderParameter>(title: $"Configure {decoder.Title}".AsRenderFragment<object?>(), model: decoder.GetParameters(Model));
            decoder.UpdateParameters(Model, model);
        }
    }
}
