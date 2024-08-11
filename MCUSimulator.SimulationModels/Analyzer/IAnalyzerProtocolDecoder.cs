using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public interface IAnalyzerProtocolDecoder
    {
        string Title { get; }
        AnalyzerProtocolDecoderParameter GetParameters(AnalyzerSimulationModel model);
        void UpdateParameters(AnalyzerSimulationModel model, AnalyzerProtocolDecoderParameter parameters);
        IEnumerable<string>? Decode(AnalyzerSimulationModel model);
    }
}
