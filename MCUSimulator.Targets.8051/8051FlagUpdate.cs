namespace MCUSimulator._8051
{
    [Flags]
    public enum _8051FlagUpdate
    {
        Z = 1 << 0,
        C = 1 << 1,
        OV = 1 << 2,
        AC = 1 << 3,
        All = Z | C | OV | AC
    }
}