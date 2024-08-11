using LivingThing.Core.Frameworks.Common.OneOf;
using LivingThing.Core.Frameworks.Common.String;
using LivingThing.Core.Frameworks.Common.Utility;
using System.Collections.Concurrent;

namespace MCUSimulator.Core
{
    public class MCUClockSystem
    {
        public IEnumerable<MCUClock> Clocks { get; }

        public MCUClockSystem(IEnumerable<MCUClock> clocks)
        {
            this.Clocks = clocks;
        }

        long baseClockCycles;
        int baseClock;
        public int BaseClock => baseClock;

        internal void Initialize(MCUSimulatorEngine simulator)
        {
            if (!Clocks.Any())
                throw new InvalidOperationException("There must be at least on clock defined");
            baseClock = Clocks.Max(c => c.GetFrequency(simulator));
        }

        public string CycleToTime(int cycle)
        {
            double us = CycleToUs(cycle);
            if (us >= 1000000)
            {
                return $"{(us / 1000000):0.##}s";
            }
            else if (us >= 1000)
            {
                return $"{(us / 1000):0.##}ms";
            }
            return $"{us:0.##}us";
        }

        public double CycleToUs(int cycle) => (((double)cycle / (double)baseClock) * 1000000.0);
        public double CycleToMs(int cycle) => (((double)cycle / (double)baseClock) * 1000.0);
        public double CycleToS(int cycle) => (((double)cycle / (double)baseClock) * 1.0);

        public int UsToCycle(double us) => (int)((us * baseClock) / 1000000);
        public int MsToCycle(double ms) => (int)((ms * baseClock) / 1000);
        public int SToCycle(double s) => (int)(s * baseClock);

        class CycleNotificationContext
        {
            //count or us
            public OneOf<int, double> Cycles;
            public required OnClockCycle Tick;
            public int Counter;

            public int GetCycles(int baseClock)
            {
                if (Cycles.IsT0)
                    return Cycles.AsT0;
                return (int)((Cycles.AsT1 * baseClock) / 1000000);
            }
        }

        ConcurrentDictionary<string, CycleNotificationContext> notifyOnCycles = new ConcurrentDictionary<string, CycleNotificationContext>();

        public IDisposable OnCycle(OneOf<int, double> cycles, OnClockCycle tick)
        {
            var id = 16.GetRandomString();
            notifyOnCycles[id] = new CycleNotificationContext
            {
                Cycles = cycles,
                Tick = tick
            };
            return new DisposableDelegate(() => notifyOnCycles.TryRemove(id, out _));
        }

        public IDisposable OnCycleUs(double us, OnClockCycle tick)
        {
            return OnCycle(us, tick);
        }

        public IDisposable OnCycleUMs(double ms, OnClockCycle tick)
        {
            return OnCycle(ms * 1000.0, tick);
        }

        public IDisposable OnCycleS(double s, OnClockCycle tick)
        {
            return OnCycle(s * 1000000, tick);
        }

        public Task Run(MCUSimulatorEngine simulator, int steps = -1)
        {
            //var maxClock = Clocks.MaxBy(c => c.GetFrequency(simulator)) ?? throw new InvalidOperationException("There must be at least on clock defined");
            //var minClock = Clocks.MinBy(c => c.Frequency) ?? throw new InvalidOperationException("There must be at least on clock defined");
            return Task.Run(() =>
            {
                Dictionary<MCUClock, int> counters = new Dictionary<MCUClock, int>();
                foreach (var clk in Clocks)
                {
                    counters[clk] = 0;
                }

                //copy the clocks to stack and convert to array to make them faster to access
                var clocks = Clocks.ToArray();

                var maxClock = Clocks.MaxBy(c => c.GetFrequency(simulator))!;
                int[] divider = new int[Clocks.Count()];

                void UpdateDivider(int iClock)
                {
                    //baseClock = Clocks.MaxBy(c => c.GetFrequency(simulator))!;
                    Span<int> freqs = stackalloc int[clocks.Length];
                    for (int i = 0; i < clocks.Length; i++)
                        freqs[i] = clocks[i].GetFrequency(simulator);
                    int maxFreq = freqs[0];
                    for (int i = 1; i < clocks.Length; i++)
                    {
                        var freq = freqs[i];
                        if (freq > maxFreq)
                        {
                            maxFreq = freq;
                        }
                    }
                    baseClock = maxFreq;
                    var clk = freqs[iClock];
                    divider[iClock] = clk == 0 ? 0 : maxFreq / clk;
                }

                for (int iClock = 0; iClock < clocks.Length; iClock++)
                {
                    UpdateDivider(iClock);
                }

                var simCycles = simulator.TotalCycles;
                simulator.Running = true;

                while (simulator.Running)
                {
                    //foreach (var ontick in maxClock.OnTick)
                    //{
                    //    ontick(simulator);
                    //}
                    for (int iClock = 0; iClock < clocks.Length; iClock++)
                    {
                        var clock = clocks[iClock];
                        var div = divider[iClock];
                        if (div != 0)
                        {
                            div--;
                            if (div == 0)
                            {
                                for (int itClock = 0; itClock < clock.OnTick.Length; itClock++)
                                {
                                    try
                                    {
                                        clock.OnTick[itClock](simulator);
                                    }
                                    catch (Exception e)
                                    {
                                        simulator.LogDiagnostics(MCUDiagnosticLogLevel.Fatal, e.Message);
                                        simulator.Pause();
                                    }
                                }
                                UpdateDivider(iClock);
                                counters[clock]++;
                            }
                            else
                            {
                                divider[iClock] = div;
                            }
                        }
                        else
                        {
                            UpdateDivider(iClock);
                        }
                    }

                    foreach (var context in notifyOnCycles.Values)
                    {
                        context.Counter++;
                        if (context.Counter >= context.GetCycles(baseClock))
                        {
                            try
                            {
                                context.Tick(simulator);
                            }
                            catch (Exception e)
                            {
                                simulator.LogDiagnostics(MCUDiagnosticLogLevel.Fatal, e.Message);
                                simulator.Pause();
                            }
                            context.Counter = 0;
                        }
                    }

                    baseClockCycles++;
                    if (simulator.TotalCycles != simCycles)
                    {
                        if (simulator.NextInstruction?.EvalueBreakPoints() ?? false)
                        {
                            simulator.Pause();
                            break;
                        }
                        else if (steps > 0)
                        {
                            steps--;
                            if (steps == 0)
                                break;
                        }
                    }
                }
                simulator.Running = false;
            });
        }
    }
}
