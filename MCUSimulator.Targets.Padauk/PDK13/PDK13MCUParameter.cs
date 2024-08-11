using MCUSimulator.Core;

namespace MCUSimulator.Targets.Padauk.PDK13
{
    public class PDK13MCUParameter : MCUParameter
    {
        public int IHRCFrequency { get; set; } = 16000000;
        public int ILRCFrequency { get; set; } = 62500;
    }
}
