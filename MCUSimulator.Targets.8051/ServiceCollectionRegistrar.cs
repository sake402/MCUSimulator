using LivingThing.Core.Frameworks.Common.Services;
using MCUSimulator.Core;
using MCUSimulator.Targets.Padauk.PDK13;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.Targets.Padauk
{
    public class ServiceCollectionRegistrar : IServiceCollectionBuilder
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration, StartupOptions options)
        {
            services.AddScoped<IMCUFactory>(x => new MCUFactory<MCU8051>("Generic 8051 CPU",
            @""));
        }
    }
}
