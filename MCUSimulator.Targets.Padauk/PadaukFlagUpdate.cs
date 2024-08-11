namespace MCUSimulator.Targets.Padauk
{
    [Flags]
    public enum PadaukFlagUpdate
    {
        Z = 1 << 0,
        C = 1 << 1,
        OV = 1 << 2,
        AC = 1 << 3,
        All = Z | C | OV | AC
    }
}