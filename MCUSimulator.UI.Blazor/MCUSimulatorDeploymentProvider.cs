using LivingThing.Core.Frameworks.Common.Services;
using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.OneOf;

namespace MCUSimulator.UI.Blazor
{
    public class MCUSimulatorDeploymentProvider : IApplicationDeploymentProvider
    {
        public string? MediaPlaceholderPath { get; }
        public Type? LoadingComponent { get; }
        public string? Title => "MCUSimulator";
        public string? Description { get; }
        public IconDescriptor Icon { get; }
        public OneOf<string, Type> LoginComponent { get; }
    }
}
