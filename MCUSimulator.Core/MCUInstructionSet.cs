namespace MCUSimulator.Core
{
    public class MCUInstructionSet
    {
        public required string Mnemonic { get; set; }
        public Func<long[], string>? Formatter { get; set; }
        public required ulong MachineCode { get; set; }
        public required ulong MachineCodeMask { get; set; }
        public required IEnumerable<Range>? Arguments { get; set; }
        public required int Cycle { get; set; }
        public required OpCodeExecute Execute { get; set; }

        public bool Match(ulong machineCode, out long[]? args)
        {
            if ((machineCode & MachineCodeMask) == MachineCode)
            {
                if (Arguments?.Any() ?? false)
                {
                    long[] _args = new long[Arguments.Count()];
                    int i = 0;
                    foreach (var _argsRange in Arguments)
                    {
                        var bitCount = _argsRange.End.Value - _argsRange.Start.Value + 1;
                        var bitMask = ~(0xFFFFFFFFFFFFFFFF << bitCount);
                        _args[i] = (long)(((machineCode & ~MachineCodeMask) >> _argsRange.Start.Value) & bitMask);
                        i++;
                    }
                    args = _args;
                }
                else
                {
                    args = null;
                }
                return true;
            }
            args = null;
            return false;
        }

        public override string ToString()
        {
            return $"{Mnemonic}";
        }
    }
}
