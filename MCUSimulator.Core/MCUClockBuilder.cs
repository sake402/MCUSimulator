using LivingThing.Core.Frameworks.Common.OneOf;

namespace MCUSimulator.Core
{
    public delegate void OnClockCycle(MCUSimulatorEngine context);
    public delegate int ClockProvider(MCUSimulatorEngine context);
    public class MCUClockBuilder
    {
        List<MCUClock> clocks = new List<MCUClock>();
        public MCUClockBuilder Define(OneOf<int, ClockProvider> clock, string? name, OnClockCycle? onTick)
        {
            var mclock = new MCUClock
            {
                Frequency = clock,
                Name = name,
                OnTick = onTick != null ? [onTick] : []
            };
            clocks.Add(mclock);
            return this;
        }

        public MCUClockSystem Build()
        {
            return new MCUClockSystem(clocks);
        }
    }
}
