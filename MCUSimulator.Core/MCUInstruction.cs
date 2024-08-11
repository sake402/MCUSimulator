using System.Text;

namespace MCUSimulator.Core
{
    public class MCUInstruction
    {
        public MCUInstruction(MCUInstructionSet instructionSet, int startAddress, int width, ulong opCode)
        {
            InstructionSet = instructionSet;
            StartAddress = startAddress;
            Width = width;
            OpCode = opCode;
            if (instructionSet.Match(opCode, out var args))
            {
                Arguments = args;
            }
            else
            {
                throw new InvalidOperationException($"OpCode {OpCode:X} doesn't match Instruction set {instructionSet}");
            }
        }

        public MCUInstructionSet InstructionSet { get; }
        public int StartAddress { get; }
        public int Width { get; }
        public ulong OpCode { get; }
        public long[]? Arguments { get; }
        public bool BreakPoint { get; set; }
        public List<Func<bool>> BreakPoints { get; } = new List<Func<bool>>();

        public void ToggleBreakPoint()
        {
            BreakPoint = !BreakPoint;
        }

        public bool EvalueBreakPoints()
        {
            return BreakPoint || BreakPoints.Any(b => b());
        }

        public override string ToString()
        {
            if (InstructionSet.Formatter != null)
            {
                return InstructionSet.Formatter(Arguments ?? Array.Empty<long>());
            }
            else if (Arguments?.Any() ?? false)
            {
                StringBuilder builder = new StringBuilder();
                int arg_i = 0;
                for (int i = 0; i < InstructionSet.Mnemonic.Length;)
                {
                    if (InstructionSet.Mnemonic[i] == '{')
                    {
                        var end = InstructionSet.Mnemonic.IndexOf('}', i);
                        var argValue = InstructionSet.Mnemonic.Substring(i + 1, end - i);
                        if (!int.TryParse(argValue, out var argIndex))
                            argIndex = arg_i;
                        builder.Append($"0x{Arguments.ElementAt(argIndex):X}");
                        arg_i++;
                        i = end + 1;
                    }
                    else
                    {
                        builder.Append(InstructionSet.Mnemonic[i]);
                        i++;
                    }
                }
                return builder.ToString();
            }
            else
            {
                return $"{InstructionSet.Mnemonic}";
            }
        }
    }
}
