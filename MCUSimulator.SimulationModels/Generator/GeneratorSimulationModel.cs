using LivingThing.Core.Frameworks.Common.Attributes;
using LivingThing.Core.Frameworks.Common.OneOf;
using LivingThing.Core.Frameworks.Common.String;
using MCUSimulator.Core;
using MCUSimulator.Core.SimulationModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.SimulationModels.Generator
{
    public class GeneratorSimulationModel : BaseSimulationModel<GeneratorSimulationModelParameter>
    {
        List<IDisposable> disposables = new List<IDisposable>();
        [Schema(Exclude = SchemaType.All)]
        public GeneratorPatternContext[]? ParallelPatterns { get; set; }

        /// <summary>
        /// convert a time in units of us or ms to us period
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ParseTime(string value)
        {
            var timeUnit = value.EndsWith("us") ? 1 : value.EndsWith("ms") ? 1000.0 : 1000000.0;
            var time = double.Parse(value.TrimEnd('u', 'm', 's'));
            double usTime = time * timeUnit;
            return usTime;
        }

        /// <summary>
        /// Parse the binary patter to an enumerable of "either wait time or a binary pattern"
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static IEnumerable<OneOf<double, BinaryPattern>> ParsePattern(GeneratorSimulationModelParameterPattern pattern)
        {
            if (pattern.Pattern == null)
                yield break;
            string collectedPattern = "";
            for (int i = 0; i < pattern.Pattern.Length; i++)
            {
                char c = pattern.Pattern[i];
                if (char.IsWhiteSpace(c))
                {
                    if (collectedPattern.Length > 0)
                    {
                        yield return collectedPattern.UnPackBinaryPattern();
                        collectedPattern = "";
                    }
                    continue;
                }
                if (c == '/') //comment, filter off
                {
                    if (collectedPattern.Length > 0)
                    {
                        yield return collectedPattern.UnPackBinaryPattern();
                        collectedPattern = "";
                    }
                    int ix = i;
                    while (pattern.Pattern[ix] != '\r' && pattern.Pattern[ix] != '\n')
                        ix++;
                    i = ix;
                }
                if (c == '1' || c == '0')
                    collectedPattern += c;
                else if (c == 'w' && pattern.Pattern[i + 1] == '(')//w(3ms)=>wait(XXms)
                {
                    var closingBrace = pattern.Pattern.IndexOf(')', i + 1);
                    if (closingBrace > i + 1)
                    {
                        var duration = pattern.Pattern.Substring(i + 2, closingBrace - i - 2);
                        var time = ParseTime(duration);
                        i = closingBrace;
                        yield return time;
                    }
                    if (collectedPattern.Length > 0)
                    {
                        yield return collectedPattern.UnPackBinaryPattern();
                        collectedPattern = "";
                    }
                }
            }
            if (collectedPattern.Length > 0)
            {
                yield return collectedPattern.UnPackBinaryPattern();
                collectedPattern = "";
            }
        }

        public override void Initialize(MCUSimulatorEngine simulator)
        {
            if (Parameter.Patterns?.Count() > 0 && Parameter.TimeBase != null)
            {
                double usTime = ParseTime(Parameter.TimeBase);
                disposables.ForEach(d => d.Dispose());
                disposables.Clear();
                var mbinaryCodes = Parameter.Patterns.Select(p => ParsePattern(p));
                ParallelPatterns = new GeneratorPatternContext[Parameter.Patterns.Count()];
                for (int i = 0; i < Parameter.Patterns.Count(); i++)
                {
                    ParallelPatterns[i] = new GeneratorPatternContext()
                    {
                        Patterns = mbinaryCodes.ElementAt(i),
                        PatternIndex = 0,
                        BitIndex = -1
                    };
                }
                int iOutputPin = 0;
                foreach (var outputPin in Outputs.Values)
                {
                    if (outputPin.ConnectedMemory != null)
                    {
                        outputPin.ConnectedMemory.Value = Parameter.InitialState?.ElementAtOrDefault(iOutputPin) == '1' ? 1 : 0;
                    }
                    iOutputPin++;
                }
                double waitTime = 0;
                int parallelPattern_i = 0;
                foreach (var _patterns in ParallelPatterns)
                {
                    int mParallelPattern_i = parallelPattern_i;
                    var patterns = _patterns;
                    var disposable = simulator.Clock.OnCycleUs(usTime, (simulator) =>
                     {
                         do
                         {
                             if (waitTime > 0)
                             {
                                 waitTime -= usTime;
                                 if (waitTime <= 0)
                                 {
                                     patterns.PatternIndex++;
                                     patterns.BitIndex = -1;
                                     patterns.Waiting = false;
                                     continue;
                                 }
                             }
                             else
                             {
                                 if (patterns.PatternIndex < patterns.Patterns.Count())
                                 {
                                     var binaryCode = patterns.Patterns.ElementAt(patterns.PatternIndex);
                                     if (binaryCode.IsT0)
                                     {
                                         patterns.Waiting = true;
                                         waitTime = binaryCode.AsT0;
                                         continue;
                                     }
                                     else if (binaryCode.IsT1)
                                     {
                                         if (patterns.BitIndex < binaryCode.AsT1.BinaryBits)
                                         {
                                             patterns.BitIndex++;
                                             if (patterns.BitIndex >= binaryCode.AsT1.BinaryBits)
                                             {
                                                 patterns.BitIndex = 0;
                                                 patterns.PatternIndex++;
                                                 if (patterns.PatternIndex >= patterns.Patterns.Count() &&
                                                    Parameter.Repeat)
                                                 {
                                                     patterns.PatternIndex = 0;
                                                 }
                                                 continue;
                                             }
                                             var outPutPin = Outputs.Values.ElementAtOrDefault(mParallelPattern_i);
                                             if (outPutPin?.ConnectedMemory != null)
                                             {
                                                 var bit = binaryCode.AsT1.GetBitCode(patterns.BitIndex);
                                                 outPutPin.ConnectedMemory.Value = bit ? 1 : 0;
                                             }
                                         }
                                     }
                                 }
                             }
                             break;
                         } while (true);
                     });
                    disposables.Add(disposable);
                    parallelPattern_i++;
                }
            }
            base.Initialize(simulator);
        }

        public override void Dispose()
        {
            disposables.ForEach(d => d.Dispose());
            disposables.Clear();
            base.Dispose();
        }
    }
}
