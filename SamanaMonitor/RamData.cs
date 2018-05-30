using System.Diagnostics;

namespace SamanaMonitor
{
    public class RamData : JSONItem
    {
        private ulong ramTotal;
        private ulong _ramFree;
        private ulong ramLoad;
        private ulong ramPercUsage;
        private float pfLoad;
        private ServerDataConfig config;
        private Logging log;

        public RamData(ulong t, ServerDataConfig c)
        {
            Stopwatch sw = new Stopwatch();
            log = new Logging("RAM Data Collector", c.debug_level);
            sw.Start();
            config = c;
            ramTotal = t;
            ramLoad = 0;
            ramFree = t;
            ramPercUsage = 0;
            sw.Stop();
            ticks = sw.ElapsedMilliseconds;
            log.debug(1, "Created RAM Data ", 1);
        }

        public void Poll(int Tick)
        {
            if (config.RAMSampleRate <= 0 || Tick % config.RAMSampleRate != 0) return;
            log.debug(1, "Polling RAM data", 0);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            PerformanceCounter ramCounter;
            PerformanceCounter pfCounter;
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            pfCounter = new PerformanceCounter("Memory", "Page Faults/sec");
            ramFree = (ulong)ramCounter.NextValue();
            pfLoad = 0;
            int samples = 0;

            for (int i = 0; i < 10; i++)
            {
                samples++;
                pfLoad += pfCounter.NextValue();
                System.Threading.Thread.Sleep(50);
            }
            if (samples > 0)
            {
                pfLoad /= samples;
            }

            sw.Stop();
            ticks = sw.ElapsedMilliseconds;
        }

        public ulong ramFree
        {
            get { return _ramFree; }
            set
            {
                _ramFree = value;
                ramLoad = ramTotal - _ramFree;
                if (ramTotal > 0)
                {
                    ramPercUsage = ramLoad * 100 / ramTotal;
                }
            }
        }

        public override string json()
        {
            return "{" + "\"ramTotal\": " + ramTotal + ", " +
                "\"ramFree\": " + _ramFree + ", " +
                "\"ramLoad\": " + ramLoad + ", " +
                "\"ramPercUsage\": " + ramPercUsage + ", " +
                "\"pageFaults\": " + pfLoad + ", " +
                "\"ticks\": " + ticks + " }";

        }
    }
}
