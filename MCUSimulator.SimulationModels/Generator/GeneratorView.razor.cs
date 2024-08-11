using LivingThing.Core.Frameworks.Client;
using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Common.String;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.Generator
{
    public partial class GeneratorView : BaseComponent
    {
        [Parameter] public GeneratorSimulationModel Model { get; set; } = default!;

        GeneratorPatternModel? model;
        async Task GeneratePattern()
        {
            model = await UnitOfWork.UI.Overlay.Form<ViewConfiguration, GeneratorPatternModel>(title: "Pattern Generator".AsRenderFragment<object?>(), model: model);
            if (!string.IsNullOrEmpty(model.Codes))
            {
                var patterns = model.Codes.Split(' ', ',', '\r', '\n').Where(s => !string.IsNullOrEmpty(s)).Select(code =>
                {
                    if (code.StartsWith("w"))
                        return code;
                    var numeric = code.ParseBinary(out var bits);
                    var bpattern = new BinaryPattern
                    {
                        MSBFirst = model.MSBFirst,
                        BinaryBits = bits,
                        BinaryCode = [(ulong)numeric]
                    };
                    var pattern = bpattern.EncodeBits(model.Zero, model.One);
                    return model.PrefixCode + pattern + model.SuffixCode;
                });
                var pattern = new GeneratorSimulationModelParameterPattern
                {
                    Pattern = string.Join("\r\n", patterns)
                };
                Model.Parameter.Patterns = Model.Parameter.Patterns?.Concat([pattern]) ?? [pattern];
            }
        }
    }
}
