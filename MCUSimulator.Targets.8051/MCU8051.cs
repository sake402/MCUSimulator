using LivingThing.Core.Frameworks.Common.OneOf;
using MCUSimulator.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.Targets.Padauk.PDK13
{
    public class MCU8051 : MCU<MCU8051Parameter>
    {
        public MCU8051() :
            base("8051", "", -1, 8)
        {
        }

        public override MCUSimulatorEngine Build()
        {
            ClockBuilder.Define(Parameter.Frequency, "SystemClock", (simulation) =>
            {
                simulation.Step();
            });
            return base.Build();
        }
    }
}
