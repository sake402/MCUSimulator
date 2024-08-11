using LivingThing.Core.Frameworks.Client.Components.SVG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.Analyzer.Protocols
{
    public class AsyncSerialAnalyzerProtocolDecoder : BaseAnalyzerProtocolDecoder<AsyncSerialAnalyzerProtocolDecoderParameter>
    {
        public override string Title => "Async Serial";
        public override IEnumerable<string>? Decode(AnalyzerSimulationModel model)
        {
            var parameters = GetParameters(model);
            if (parameters.Enabled && parameters.Terminal != null && model.Inputs.TryGetValue(parameters.Terminal, out var terminal))
            {
                var collector = model.Collectors.FirstOrDefault(e => e.Key == terminal).Value;
                if (collector?.Collected.Count >= 2)
                {
                    var sampleUs = 1000000.0 / parameters.BaudRate;
                    var sampleCycle = model.Simulator!.Clock.UsToCycle(sampleUs);
                    int startBitCycle = collector.FindNegativeEdge(1).Key;
                    List<string> decoded = new List<string>();
                    do
                    {
                        //move sample period to center of start bit;
                        startBitCycle += sampleCycle / 2;
                        //move sample point to first bit
                        var samplePoint = startBitCycle + sampleCycle;
                        int value = 0;
                        string? error = null;
                        int parityCheck = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            int bit = collector.GetSample(samplePoint);
                            if (bit == -1)
                                break;
                            parityCheck += bit;
                            if (bit == 1)
                                value |= (1 << i);
                            samplePoint += sampleCycle;
                        }
                        if (parameters.Parity != AsyncSerialAnalyzerProtocolDecoderParameterParity.None)
                        {
                            int parity = collector.GetSample(samplePoint);
                            if (parity == -1)
                                break;
                            parityCheck &= 1;
                            if (parameters.Parity == AsyncSerialAnalyzerProtocolDecoderParameterParity.Odd)
                            {
                                if (parity == parityCheck)
                                {
                                    error = $"Invalid parity, expected {parityCheck}, got {parity}";
                                }
                            }
                            else
                            {
                                if (parity != parityCheck)
                                {
                                    error = $"Invalid parity, expected {parityCheck}, got {parity}";
                                }
                            }
                            samplePoint += sampleCycle;
                        }
                        int stopBit = collector.GetSample(samplePoint);
                        if (stopBit == -1)
                            break;
                        if (stopBit != 1)
                        {
                            error = $"Invalid stop bit, expected 1, got {stopBit}";
                        }
                        if (parameters.StopBits == AsyncSerialAnalyzerProtocolDecoderParameterStopBits.OnePointFive)
                        {
                            samplePoint += sampleCycle / 2;
                            int stopBit2 = collector.GetSample(samplePoint);
                            if (stopBit2 == -1)
                                break;
                            if (stopBit2 != 1)
                            {
                                error = $"Invalid stop bit, expected 1, got {stopBit2}";
                            }
                        }
                        else if (parameters.StopBits == AsyncSerialAnalyzerProtocolDecoderParameterStopBits.Two)
                        {
                            samplePoint += sampleCycle;
                            int stopBit2 = collector.GetSample(samplePoint);
                            if (stopBit2 == -1)
                                break;
                            if (stopBit2 != 1)
                            {
                                error = $"Invalid stop bit, expected 1, got {stopBit2}";
                            }
                        }
                        decoded.Add($"0x{value:X2}, {value:B8}, {value}{(error != null ? $" ({error})" : "")}");
                        //find the next negtive edge after samplePoint
                        var nextNegative = collector.FindNegativeEdge(1, samplePoint);
                        if (nextNegative.Key < startBitCycle)
                            break;
                        startBitCycle = nextNegative.Key;
                    } while (true);
                    return decoded;
                }
            }
            return null;
        }
    }
}
