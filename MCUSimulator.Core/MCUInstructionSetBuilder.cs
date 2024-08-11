using LivingThing.Core.Frameworks.Common.String;

namespace MCUSimulator.Core
{
    public delegate void OpCodeExecute(ref OpCodeExecutionContext context);
    public class MCUInstructionSetBuilder
    {
        List<MCUInstructionSet> instructionSets = new List<MCUInstructionSet>();

        public MCUInstructionSetBuilder()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mnemonic">Name of opcode</param>
        /// <param name="pattern">
        /// Pattern can be written in binary as 11100011
        /// Or in shorthand form 1*3,0*3,1*2
        /// </param>
        /// <param name="execute"></param>
        /// <returns></returns>
        public MCUInstructionSetBuilder Define(string mnemonic, string pattern, OpCodeExecute execute, int instructionCycle = 1, Func<long[], string>? formatter = null)
        {
            //var splitted = pattern.Split(',');
            ////determine how many bits is this instruction first
            //int instructionBits = 0;
            //foreach (var s in splitted)
            //{
            //    if (s.Contains('*'))
            //    {
            //        instructionBits += int.Parse(s.Split('*')[1]);
            //    }
            //    else
            //    {
            //        instructionBits += s.Length;
            //    }
            //}
            var binary = pattern.UnPackBinaryPattern();
            //foreach (var s in splitted)
            //{
            //    if (s.Contains('*'))
            //    {
            //        var ss = s.Split('*');
            //        var bit = ss[0][0] == '0' ? 0 : ss[0][0] == '1' ? 1 : ss[0][0] == 'X' ? 0 : throw new InvalidOperationException("Bad binary patter");
            //        var bitEnable = ss[0][0] == 'X' ? 0 : 1;
            //        var count = int.Parse(ss[1]);
            //        if (ss[0][0] == 'X')
            //        {
            //            argStartBits ??= new List<Range>();
            //            argStartBits.Add((index - count + 1)..index);
            //        }
            //        for (int i = 0; i < count; i++)
            //        {
            //            machineCode |= ((ulong)bit << index);
            //            machineCodeMask |= ((ulong)bitEnable << index);
            //            index--;
            //        }
            //    }
            //    else
            //    {
            //        foreach (var b in s)
            //        {
            //            var bit = b == '0' ? 0 : b == '1' ? 1 : b == 'X' ? 0 : throw new InvalidOperationException("Bad binary patter");
            //            var bitEnable = b == 'X' ? 0 : 1;
            //            if (b == 'X')
            //            {
            //                if (argStartBit == -1)
            //                    argStartBit = index;
            //            }
            //            else
            //            {
            //                if (argStartBit > 0)
            //                {
            //                    argStartBits ??= new List<Range>();
            //                    argStartBits.Add(argStartBit..index);
            //                }
            //                argStartBit = -1;
            //            }
            //            machineCode |= ((ulong)bit << index);
            //            machineCodeMask |= ((ulong)bitEnable << index);
            //            index--;
            //        }
            //    }
            //}
            var ins = new MCUInstructionSet
            {
                Mnemonic = mnemonic,
                MachineCode = binary.BinaryCode[0],
                MachineCodeMask = binary.BinaryCodeMask[0],
                Arguments = binary.ArgumentsRange,
                Cycle = instructionCycle,
                Execute = execute,
                Formatter = formatter
            };
            var existing = instructionSets.FirstOrDefault(i => i.Match(binary.BinaryCode[0], out _));
            if (existing != null)
            {
                throw new InvalidOperationException($"Instruction set conflict {ins} & {existing}");
            }
            instructionSets.Add(ins);
            return this;
        }

        public List<MCUInstructionSet> Build()
        {
            return instructionSets;
        }
    }
}
