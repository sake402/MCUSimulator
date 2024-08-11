namespace MCUSimulator.Core
{
    public class MCUWatchdogTimer
    {
        public long Count { get; internal set; }
        public void Reset()
        {
            Count = 0;
        }
    }
}
