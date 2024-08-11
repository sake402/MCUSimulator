using LivingThing.Core.Frameworks.Common.Serialization;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public abstract class BaseAnalyzerProtocolDecoder<TParameter> : IAnalyzerProtocolDecoder
        where TParameter : AnalyzerProtocolDecoderParameter, new()
    {
        public abstract string Title { get; }
        public abstract IEnumerable<string>? Decode(AnalyzerSimulationModel model);

        public TParameter GetParameters(AnalyzerSimulationModel model)
        {
            var sparameter = model.Parameter.ProtocolParameters?.GetValueOrDefault(GetType().FullName!);
            return sparameter.DeSerialize<TParameter>() ?? new TParameter();
        }

        AnalyzerProtocolDecoderParameter IAnalyzerProtocolDecoder.GetParameters(AnalyzerSimulationModel model)
        {
            return GetParameters(model)!;
        }

        void IAnalyzerProtocolDecoder.UpdateParameters(AnalyzerSimulationModel model, AnalyzerProtocolDecoderParameter parameters)
        {
            var sparameter = parameters.Serialize().Json!;
            model.Parameter.ProtocolParameters ??= new Dictionary<string, string>();
            model.Parameter.ProtocolParameters[GetType().FullName!] = sparameter;
        }
    }
}
