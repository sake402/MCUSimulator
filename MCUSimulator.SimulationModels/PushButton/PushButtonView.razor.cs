using LivingThing.Core.Frameworks.Client.Components;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  MCUSimulator.SimulationModels.PushButton
{
    public partial class PushButtonView : BaseComponent
    {
        [Parameter] public PushButtonSimulationModel Model { get; set; } = default!;
        bool state;

        protected override void OnInitialized()
        {
            state = Model.Parameter.InitialState;
            if (Model.OutputPin.ConnectedMemory != null)
            {
                Model.OutputPin.ConnectedMemory.Value = state ? 1 : 0;
            }
            base.OnInitialized();
        }
        void Clicked()
        {
            if (Model.OutputPin.ConnectedMemory != null)
            {
                state = !state;
                Model.OutputPin.ConnectedMemory.Value = state ? 1 : 0;
            }
        }
    }
}
