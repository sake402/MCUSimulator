using LivingThing.Core.Frameworks.Common;
using LivingThing.Core.Frameworks.Common.Attributes;

namespace MCUSimulator.SimulationModels.Generator
{
    public class GeneratorPatternModel
    {
        /// <summary>
        /// The code to encode. This can be in binary(prefix 0b), Hex(prefix 0x) or decimal
        /// </summary>
        [Schema(InputType = InputType.TextArea, Description = "Code to encode. This can be in Binary(prefix 0b), Hex(prefix 0x) or decimal")]
        public string? Codes { get; set; }
        /// <summary>
        /// Encode MSB first
        /// </summary>
        public bool MSBFirst { get; set; }
        /// <summary>
        /// What to encode binary code 1 as
        /// </summary>
        [Schema(Description = "What to encode binary code 1 as")]
        public string One { get; set; } = "1";
        /// <summary>
        /// What to encode binary code 0 as
        /// </summary>
        [Schema(Description = "What to encode binary code 0 as")]
        public string Zero { get; set; } = "0";
        public string? PrefixCode { get; set; }
        public string? SuffixCode { get; set; }
    }
}
