using LivingThing.Core.Frameworks.Common.Attributes;
using LivingThing.Core.Frameworks.Common.OneOf;
using LivingThing.Core.Frameworks.Common.String;
using MCUSimulator.Core;
using MCUSimulator.Core.SimulationModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public class AnalyzerSimulationModel : BaseSimulationModel<AnalyzerSimulationModelParameter>
    {
        [Schema(Exclude = SchemaType.All)]
        public Dictionary<ISimulationModelInput, AnalyzerCollector> Collectors { get; } = new Dictionary<ISimulationModelInput, AnalyzerCollector>();

        internal MCUSimulatorEngine? Simulator { get; set; }

        public void ClearAll()
        {
            foreach (var kv in Collectors)
                kv.Value.Clear();
        }

        public override void Initialize(MCUSimulatorEngine simulator)
        {
            this.Simulator = simulator;
            Collectors.Clear();
            foreach (var inputPin in Inputs)
            {
                if (inputPin.Value.ConnectedMemory != null)
                {
                    Collectors[inputPin.Value] = new AnalyzerCollector(simulator);
                    //collect the initial value
                    Collectors[inputPin.Value].Collect(inputPin.Value.ConnectedMemory.Value);
                    inputPin.Value.OnChanged -= InputPin_OnChanged;
                    inputPin.Value.OnChanged += InputPin_OnChanged;
                }
            }
            base.Initialize(simulator);
        }

        private void InputPin_OnChanged(object? sender, MemoryChangedEventArg e)
        {
            var inputPin = sender as ISimulationModelInput;
            if (inputPin?.ConnectedMemory != null && Simulator != null)
            {
                Collectors[inputPin].Collect(inputPin.ConnectedMemory.Value);
            }
        }

        public override void Dispose()
        {
            int iInputsPin = 0;
            foreach (var inputPin in Inputs.Values)
            {
                if (inputPin.ConnectedMemory != null)
                {
                    inputPin.OnChanged -= InputPin_OnChanged;
                }
                iInputsPin++;
            }
            base.Dispose();
        }
    }
}
