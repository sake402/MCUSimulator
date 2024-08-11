using LivingThing.Core.Frameworks.Common.Services;
using MCUSimulator.SimulationModels.Analyzer;
using MCUSimulator.SimulationModels.Analyzer.Protocols;
using MCUSimulator.SimulationModels.Generator;
using MCUSimulator.SimulationModels.LED;
using MCUSimulator.SimulationModels.PushButton;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels
{
    public class ServiceCollectionRegistrar : IServiceCollectionBuilder
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration, StartupOptions options)
        {
            services.AddScoped<ISimulationModelViewProvider, PushButtonSimulationModelProvider>();
            services.AddScoped<ISimulationModelViewProvider, LEDSimulationModelProvider>();
            services.AddScoped<ISimulationModelViewProvider, GeneratorSimulationModelProvider>();
            services.AddScoped<ISimulationModelViewProvider, AnalyzerSimulationModelProvider>();
            services.AddScoped<IAnalyzerProtocolDecoder, AsyncSerialAnalyzerProtocolDecoder>();
        }
    }
}
