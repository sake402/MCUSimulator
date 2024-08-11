namespace MCUSimulator.Core
{
    public struct OpCodeExecutionContext
    {
        /// <summary>
        /// How many cycles the instruction took to execute
        /// </summary>
        public int Cycle { get; set; }
        public MCUSimulatorEngine Simulator { get; set; }
        public long[] OpCodeArgument { get; set; }
    }
}
