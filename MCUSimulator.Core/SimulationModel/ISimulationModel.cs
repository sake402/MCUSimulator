using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.Core.SimulationModel
{
    public interface ISimulationModel : IDisposable
    {
        string? Name { get; set; }
        ISimulationModelProvider? Provider { get; }
        IDictionary<string, ISimulationModelOutput> Outputs { get; }
        IDictionary<string, ISimulationModelInput> Inputs { get; }
        object Parameter { get; }
        void Initialize(MCUSimulatorEngine simulator);
    }
}
