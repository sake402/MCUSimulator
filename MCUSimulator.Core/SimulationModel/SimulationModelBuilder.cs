namespace MCUSimulator.Core.SimulationModel
{
    public class SimulationModelBuilder
    {
        Dictionary<string, ISimulationModelOutput> outputs = new Dictionary<string, ISimulationModelOutput>();
        Dictionary<string, ISimulationModelInput> inputs = new Dictionary<string, ISimulationModelInput>();
        public SimulationModelBuilder DefineInput(string name)
        {
            inputs.Add(name, new SimulationModelInput(name));
            return this;
        }
        public SimulationModelBuilder DefineOutput(string name)
        {
            outputs.Add(name, new SimulationModelOutput(name));
            return this;
        }

        public T Build<T>(ISimulationModelProvider? provider)
            where T : BaseBaseSimulationModel, new()
        {
            return new T()
            {
                Inputs = inputs,
                Outputs = outputs,
                Provider = provider
            };
        }
    }
}
