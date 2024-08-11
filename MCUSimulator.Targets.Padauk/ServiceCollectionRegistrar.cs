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
            services.AddScoped<IMCUFactory>(x => new MCUFactory<PDK13MCU>("Padauk PDK13",
            @"These devices feature a 13-bit wide code memory. Byte order is little endian.

PMC150, PMC153, PMC156, PMC166, PMS150, PMS150B, PMS150C, PMS150G, PMS153, PMS156, PMS15A, PMS15B"));
        }
    }
}
