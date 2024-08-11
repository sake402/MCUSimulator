using LivingThing.Core.Frameworks.Common.Attributes;
using MCUSimulator.Core.SimulationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  MCUSimulator.SimulationModels.LED
{
    public class LEDSimulationModel : BaseSimulationModel
    {
        [Schema(Exclude = SchemaType.All)]
        public ISimulationModelInput InputPin => Inputs.Single().Value;
    }
}
