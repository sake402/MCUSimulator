namespace MCUSimulator.Core
{
    public delegate void OnInstructionCycle(MCUSimulatorEngine context);

    public abstract class MCU
    {
        public string Name { get; }
        public string Description { get; }
        public int InstructionsBit { get; }
        public int RegistersBit { get; }
        public virtual MCUEndianess Endianess => MCUEndianess.Little;
        public MCU(string name, string description, int instructionsBit, int registersBit)
        {
            Name = name;
            Description = description;
            InstructionsBit = instructionsBit;
            RegistersBit = registersBit;
            InstructionBuilder = new MCUInstructionSetBuilder();
            RegisterBuilder = new MCURegistersBuilder(registersBit);
            ClockBuilder = new MCUClockBuilder();
        }

        List<OnInstructionCycle> executeOnInstructions = new List<OnInstructionCycle>();
        public MCUInstructionSetBuilder InstructionBuilder { get; }
        public MCURegistersBuilder RegisterBuilder { get; }
        public MCUClockBuilder ClockBuilder { get; }
        public abstract object? BuilderParameter { get; }
        public MCU OnInstructionCycle(OnInstructionCycle cycle)
        {
            executeOnInstructions.Add(cycle);
            return this;
        }

        public virtual MCUSimulatorEngine Build()
        {
            var registers = RegisterBuilder.Build();
            var simulator = new MCUSimulatorEngine(this)
            {
                InstructionBytes = (InstructionsBit + 7) / 8,
                RegisterBits = RegistersBit,
                RegisterMask = (uint)0xFFFFFFFF >> (64 - RegistersBit),
                RegisterMax = 1 << RegistersBit,
                Clock = ClockBuilder.Build(),
                InstructionSets = InstructionBuilder.Build(),
                Registers = registers,
                ExecuteOnInstructions = executeOnInstructions
            };
            simulator.Clock.Initialize(simulator);
            return simulator;
        }
    }
    public abstract class MCU<TParameter> : MCU
        where TParameter : MCUParameter, new()
    {
        public MCU(string name, string description, int instructionsBit, int registersBit) : base(name, description, instructionsBit, registersBit)
        {
        }

        TParameter? parameter;
        public TParameter Parameter => parameter ??= new TParameter();

        public override object BuilderParameter => Parameter;
    }
}
