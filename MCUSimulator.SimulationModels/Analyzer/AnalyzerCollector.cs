using MCUSimulator.Core;
using System.Collections.Concurrent;

namespace MCUSimulator.SimulationModels.Analyzer
{
    public class AnalyzerCollector
    {
        MCUSimulatorEngine simulator;

        public AnalyzerCollector(MCUSimulatorEngine simulator)
        {
            this.simulator = simulator;
        }

        public SortedDictionary<int, int> Collected { get; } = new SortedDictionary<int, int>();

        public void Clear()
        {
            lock (this)
            {
                Collected.Clear();
            }
        }

        public void Collect(int bit)
        {
            lock (this)
            {
                Collected[simulator.TotalCycles] = bit;
            }
        }

        public KeyValuePair<int, int> FindPositiveEdge(int n, int startCycle = 0)
        {
            lock (this)
            {
                foreach (var kv in Collected.Skip(1))
                {
                    if (kv.Value == 1)
                    {
                        if (kv.Key > startCycle)
                        {
                            if (--n == 0)
                                return kv;
                        }
                    }
                }
            }
            return default;
        }

        public KeyValuePair<int, int> FindNegativeEdge(int n, int startCycle = 0)
        {
            lock (this)
            {
                foreach (var kv in Collected.Skip(1))
                {
                    if (kv.Value == 0)
                    {
                        if (kv.Key > startCycle)
                        {
                            if (--n == 0)
                                return kv;
                        }
                    }
                }
            }
            return default;
        }

        public int GetSample(int cycle)
        {
            KeyValuePair<int, int> last = default;
            lock (this)
            {
                foreach (var data in Collected)
                {
                    if (cycle > last.Key && cycle < data.Key)
                    {
                        return last.Value;
                    }
                    last = data;
                }
                if (cycle < simulator.TotalCycles)
                    return Collected.Last().Value;
            }
            return -1;
        }

        public int MinPulseWidth
        {
            get
            {
                if (Collected.Count < 2)
                    return -1;
                int minDiff = int.MaxValue;
                lock (this)
                {
                    int lastTime = Collected.First().Key;
                    foreach (var time in Collected.Keys.Skip(1))
                    {
                        var diff = time - lastTime;
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                        }
                        lastTime = time;
                    }
                }
                return minDiff;
            }
        }

        public int MaxPulseWidth
        {
            get
            {
                if (Collected.Count < 2)
                    return -1;
                lock (this)
                {
                    //the first edge is at the second data sampled
                    return simulator.TotalCycles - Collected.ElementAt(1).Key;
                }
            }
        }
    }
}
