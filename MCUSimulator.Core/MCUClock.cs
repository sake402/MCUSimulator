using LivingThing.Core.Frameworks.Common.OneOf;

namespace MCUSimulator.Core
{
    public class MCUClock
    {
        public string? Name { get; set; }
        public OneOf<int, ClockProvider> Frequency { get; set; }
        /// <summary>
        /// This is deliberatel made an array, instead of list to make accessing it faster
        /// </summary>
        public OnClockCycle[] OnTick { get; internal set; } = default!;

        public int GetFrequency(MCUSimulatorEngine simulator)
        {
            if (Frequency.IsT0)
                return Frequency.AsT0;
            return Frequency.AsT1(simulator);
        }

        public override string ToString()
        {
            return $"{Name} {Frequency}";
        }
    }
}
