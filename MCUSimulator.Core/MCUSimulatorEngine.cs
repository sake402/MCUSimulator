using System.Diagnostics;

namespace MCUSimulator.Core
{

    public class MCUSimulatorEngine : IDisposable
    {
        internal MCUSimulatorEngine(MCU mcu)
        {
            MCU = mcu;
        }

        public MCU MCU { get; }
        public bool ConsoleLog { get; set; }
        //public long ClockFrequency { get; set; }
        public required int InstructionBytes { get; init; }
        public required uint RegisterMask { get; init; }
        public required int RegisterMax { get; init; }
        public required int RegisterBits { get; init; }
        //int _nextInstructionPointer;
        public int InstructionPointer { get; set; }
        //{
        //    get => _nextInstructionPointer;
        //    set
        //    {
        //        _nextInstructionPointer = value;
        //        if (NextInstruction?.EvalueBreakPoints() ?? false)
        //        {
        //            Pause();
        //        }
        //    }
        //}
        public int StepCycles { get; internal set; }
        public int TotalCycles { get; internal set; }
        public required MCUClockSystem Clock { get; init; }
        public required IEnumerable<MCUInstructionSet> InstructionSets { get; init; }
        public required MCURegisterCollection Registers { get; init; }
        public MCUWatchdogTimer WatchdogTimer { get; } = new MCUWatchdogTimer();
        public required IEnumerable<OnInstructionCycle> ExecuteOnInstructions { get; init; }
        public event EventHandler<MCUSimulatorEngine>? OnInstructions;
        public Dictionary<int, MCUInstruction>? Instructions { get; private set; }

        public MCUInstruction? NextInstruction => Instructions![InstructionPointer];
        public bool Running { get; internal set; }

        MCUInstructionSet? TryGetInstructionSet(ulong opCode)
        {
            return InstructionSets.FirstOrDefault(i => i.Match(opCode, out _));
        }
        public Dictionary<int, MCUInstruction> Load(byte[] program)
        {
            Dictionary<int, MCUInstruction> instructions = new Dictionary<int, MCUInstruction>();
            int where = 0;
            for (; where < program.Length;)
            {
                var instructionStartAddress = where;
                var instructionByteWidth = InstructionBytes < 0 ? 0 : InstructionBytes;
                ulong instructionOpCode = program[where];
                var instructionSet = InstructionBytes <= 0 || InstructionBytes == 1 ? TryGetInstructionSet(instructionOpCode) : null;
                if (instructionSet == null)
                {
                    instructionOpCode = program[where] | (uint)program[where + 1] << 8;
                    instructionSet = InstructionBytes <= 0 || InstructionBytes == 2 ? TryGetInstructionSet(instructionOpCode) : null;
                    if (instructionSet == null)
                    {
                        instructionOpCode = program[where] | (uint)program[where + 1] << 8 | (uint)program[where + 2] << 16;
                        instructionSet = InstructionBytes <= 0 || InstructionBytes == 3 ? TryGetInstructionSet(instructionOpCode) : null;
                        if (instructionSet == null)
                        {
                            instructionOpCode = program[where] | (uint)program[where + 1] << 8 | (uint)program[where + 2] << 16 | (uint)program[where + 3] << 24;
                            instructionSet = InstructionBytes <= 0 || InstructionBytes == 4 ? TryGetInstructionSet(instructionOpCode) : null;
                            where++;
                            if (instructionSet == null)
                            { }
                            else
                            {
                                instructionByteWidth = 4;
                            }
                        }
                        else
                        {
                            instructionByteWidth = 3;
                        }
                        where++;
                    }
                    else
                    {
                        instructionByteWidth = 2;
                    }
                    where++;
                }
                else
                {
                    instructionByteWidth = 1;
                }
                if (instructionSet == null)
                {
                    throw new InvalidOperationException($"Invalid Instruction @ {where} = 0x{where:X}");
                }
                var instruction = new MCUInstruction(instructionSet, instructionStartAddress, instructionByteWidth, instructionOpCode);
                instructions.Add(instructionStartAddress, instruction);
                where++;
            }
            Instructions = instructions;
            return instructions;
        }

        List<Action> pendingWorkItems = new List<Action>();

        /// <summary>
        /// Invoke this delegate on the simulation cycles
        /// </summary>
        /// <param name="action"></param>
        public void Invoke(Action action)
        {
            pendingWorkItems.Add(action);
        }

        public void Step()
        {
            foreach (var workItem in pendingWorkItems)
            {
                workItem();
            }
            pendingWorkItems.Clear();
            if (Running)
            {
                var nextInstruction = Instructions![InstructionPointer];
                int ins = InstructionPointer;
                InstructionPointer += nextInstruction.Width;
                var ctx = new OpCodeExecutionContext
                {
                    Simulator = this,
                    Cycle = nextInstruction.InstructionSet.Cycle,
                    OpCodeArgument = nextInstruction.Arguments ?? Array.Empty<long>()
                };
                nextInstruction.InstructionSet.Execute(ref ctx);
                TotalCycles += ctx.Cycle;
                foreach (var execute in ExecuteOnInstructions)
                    execute(this);
                OnInstructions?.Invoke(this, this);
                if (ConsoleLog)
                {
                    Console.WriteLine($"-------------------------------------");
                    Console.WriteLine($"{ins:X8}: {nextInstruction}");
                    Registers.Dump();
                    Console.WriteLine();
                }
            }
        }

        int runStartCycle;
        void PrepareToRun()
        {
            runStartCycle = TotalCycles;
            foreach (var mem in Registers.Memories)
            {
                switch (mem)
                {
                    case MCURegister reg:
                        reg.OldValue = reg.Value;
                        break;
                    case MCURegisterWithAddress reg:
                        reg.OldValue = reg.Value;
                        break;
                    case MCUStaticRAM reg:
                        reg.OldValue = reg.Value;
                        break;
                    case IReadWriteMemoryBitField reg:
                        reg.OldValue = reg.Value;
                        break;
                }
            }
        }

        public void Reset()
        {
            StepCycles = 0;
            TotalCycles = 0;
            InstructionPointer = 0;
            Registers.Reset();
        }

        public void Pause()
        {
            Running = false;
        }

        public async Task StepOne()
        {
            PrepareToRun();
            await Clock.Run(this, 1);
            StepCycles = TotalCycles - runStartCycle;
        }

        public async Task StepOver()
        {
            PrepareToRun();
            var currentInstruction = Instructions![InstructionPointer];
            var nextInstruction = Instructions![InstructionPointer + currentInstruction.Width];
            bool hasBreakPointAlready = false;
            if (nextInstruction != null)
            {
                hasBreakPointAlready = nextInstruction.BreakPoint;
                nextInstruction.BreakPoint = true;
            }
            await Clock.Run(this);
            if (nextInstruction != null && !hasBreakPointAlready)
            {
                nextInstruction.BreakPoint = false;
            }
            StepCycles = TotalCycles - runStartCycle;
        }

        public async Task Run()
        {
            PrepareToRun();
            await Clock.Run(this);
            StepCycles = TotalCycles - runStartCycle;
        }

        public void LogDiagnostics(MCUDiagnosticLogLevel level, string message)
        {
            Debugger.Break();
        }

        public void Dispose()
        {
            Running = false;
        }

        //public Task RunAsync(CancellationToken token)
        //{
        //    if (Instructions == null)
        //        throw new InvalidOperationException("Program file not loaded");
        //    return Task.Run(() =>
        //    {
        //        int pc = 0;
        //        var executeOnInstructions = ExecuteOnInstructions.ToList();
        //        while (!token.IsCancellationRequested)
        //        {
        //            var nextInstruction = Instructions.ElementAt(pc);
        //            NextInstruction = pc + 1;
        //            if (ConsoleLog)
        //            {
        //                Console.WriteLine($"{pc:X8}: {nextInstruction}");
        //            }
        //            nextInstruction.InstructionSet.Execute(new OpCodeExecuteContext
        //            {
        //                Simulator = this,
        //                OpCodeArgument = nextInstruction.Arguments ?? Array.Empty<long>()
        //            });
        //            TotalCycles += nextInstruction.InstructionSet.Cycle;
        //            executeOnInstructions.ForEach(e => e(this));
        //            pc = NextInstruction;
        //        }
        //    });
        //}
    }
}
