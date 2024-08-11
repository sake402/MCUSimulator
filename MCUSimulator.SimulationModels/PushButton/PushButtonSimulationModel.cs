using LivingThing.Core.Frameworks.Common.Attributes;
using MCUSimulator.Core.SimulationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.PushButton
{
    public class PushButtonSimulationModel : BaseSimulationModel<PushButtonSimulationModelParameter>
    {
        [Schema(Exclude = SchemaType.All)]
        public ISimulationModelOutput OutputPin => Outputs.Single().Value;
    }
}
