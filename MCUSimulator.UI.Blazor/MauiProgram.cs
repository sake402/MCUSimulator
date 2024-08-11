﻿using LivingThing.Core.Frameworks.Common;
using LivingThing.Core.Frameworks.Common.Services;
using MCUSimulator.SimulationModels;
using MCUSimulator.Targets.Padauk;
using Microsoft.Extensions.Logging;
using System.Reflection;
using LivingThing.Core.Frameworks.Common.Logging;

namespace MCUSimulator.UI.Blazor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        var startup = new StartupOptions
        {
            Runtime = ApplicationRuntimeType.DesktopApp
        };

        //Load every assembly in application path so we can register them in the container.
        //We prefer dynamic loading so that MCU target and simulation model plugins can be released for this application.
        //Plugin dll can be copied simply into the application folder and will be loaded/discovered even though the application has no foreknowledge of the plugin
        var path = AppDomain.CurrentDomain.BaseDirectory;
        var dlls = Directory.GetFiles(path, "*.dll");
        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName()).ToList();
        foreach (var dll in dlls)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(dll);
                if (!loadedAssemblyNames.Contains(assemblyName))
                    Assembly.Load(assemblyName);
            }
            catch (Exception e)
            {
                LoggingFactory.LogException(e);
            }
        }

        //scan assemblies and register them into DI
        builder.Services.RegisterAssemblyServices(builder.Configuration, startup, AppDomain.CurrentDomain.GetAssemblies());

        builder.Services.AddScoped<IApplicationDeploymentProvider, MCUSimulatorDeploymentProvider>();
        builder.Services.AddGeneratedComponentOptimizers();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
