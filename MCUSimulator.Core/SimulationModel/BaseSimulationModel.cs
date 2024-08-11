using LivingThing.Core.Frameworks.Common.Attributes;

namespace MCUSimulator.Core.SimulationModel
{
    public abstract class BaseBaseSimulationModel : ISimulationModel
    {
        public string? Name { get; set; }
        [Schema(Exclude = SchemaType.All)]
        public ISimulationModelProvider? Provider { get; init; }
        [Schema(Exclude = SchemaType.All)]
        public IDictionary<string, ISimulationModelOutput> Outputs { get; internal set; } = default!;
        [Schema(Exclude = SchemaType.All)]
        public IDictionary<string, ISimulationModelInput> Inputs { get; internal set; } = default!;
        object ISimulationModel.Parameter => default!;
        public virtual void Initialize(MCUSimulatorEngine simulator)
        {
        }

        public virtual void Dispose()
        {
        }
    }

    public abstract class BaseSimulationModel<TParameter> : BaseBaseSimulationModel,ISimulationModel
        where TParameter : BaseSimulationModelParameter, new()
    {

        TParameter? _parameter;
        [Schema(Exclude = SchemaType.Form)]
        public TParameter Parameter => _parameter ??= new TParameter();
        object ISimulationModel.Parameter => Parameter;
    }

    public abstract class BaseSimulationModel : BaseSimulationModel<NOPSimulationModelParameter>
    {

    }
}
