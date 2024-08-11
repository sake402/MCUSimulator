using LivingThing.Core.Frameworks.Client.Components;
using MCUSimulator.Core;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  MCUSimulator.SimulationModels.LED
{
    public partial class LEDView : BaseComponent
    {
        [Parameter] public LEDSimulationModel Model { get; set; } = default!;

        protected override void OnInitialized()
        {
            Model.InputPin.OnChanged += InputPin_OnChanged;
            base.OnInitialized();
        }

        private void InputPin_OnChanged(object? sender, MemoryChangedEventArg e)
        {
            _ = StateChanged();
        }
        public override void Dispose()
        {
            Model.InputPin.OnChanged -= InputPin_OnChanged;
        }
    }
}
