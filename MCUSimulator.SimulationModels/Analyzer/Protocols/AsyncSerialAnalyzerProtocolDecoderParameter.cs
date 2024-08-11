namespace MCUSimulator.SimulationModels.Analyzer.Protocols
{
    public class AsyncSerialAnalyzerProtocolDecoderParameter : AnalyzerProtocolDecoderParameter
    {
        public string? Terminal { get; set; }
        public int BaudRate { get; set; } = 9600;
        public AsyncSerialAnalyzerProtocolDecoderParameterParity Parity { get; set; }
        public AsyncSerialAnalyzerProtocolDecoderParameterStopBits StopBits { get; set; }
    }
}
