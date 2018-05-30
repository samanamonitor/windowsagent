using System.Diagnostics;
using System;

namespace SamanaMonitor
{
    class CPUData : JSONItem
    {
        private float cpuLoad;
        private float cpuqLoad;
        private float[] cpu5minLoad;
        private float[] cpuq5minLoad;
        private int cpu5ptr;
        private int cpuq5ptr;
        private float cpu5avg;
        private float cpuq5avg;
        private float cpu5max;
        private float cpuq5max;
        private ServerDataConfig config;
        private int datasize;
        private Logging log;

        public CPUData(ServerDataConfig c)
        {
            Stopwatch sw = new Stopwatch();
            log = new Logging("CPU Data Collector", c.debug_level);
            sw.Start();
            config = c;
            datasize = 300000 / c.Interval;
            cpuLoad = 0;
            cpuqLoad = 0;
            cpu5minLoad = new float[datasize];
            cpuq5minLoad = new float[datasize];
            cpu5ptr = 0;
            cpuq5ptr = 0;
            cpu5avg = 0;
            cpuq5avg = 0;
            cpu5max = 0;
            cpuq5max = 0;
            sw.Stop();
            ticks = sw.ElapsedMilliseconds;
            log.debug(1, "Created CPU Data ", 1);
        }

        public void Poll(int Tick)
        {
            if (config.CPUSampleRate == 0 || Tick % config.CPUSampleRate != 0) return;
            log.debug(1, "Polling CPU data", 0);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            PerformanceCounter cpuCounter;
            PerformanceCounter cpuqCounter;
            cpuLoad = 0;
            cpuqLoad = 0;
            int samples = 0;

            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuqCounter = new PerformanceCounter("System", "Processor Queue Length");
                for (int i = 0; i < 5; i++)
                {
                    samples++;
                    cpuLoad += cpuCounter.NextValue();
                    cpuqLoad += cpuqCounter.NextValue();
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                log.error(e.Message + e.StackTrace, 201);
                cpuLoad = 0;
                cpuqLoad = 0;
            }

            if (samples > 0)
            {
                cpuLoad /= samples;
                cpuqLoad /= samples;
            }
            cpu5avg = (cpu5avg * (datasize - 1) - cpu5minLoad[cpu5ptr] + cpuLoad) / datasize;
            cpu5minLoad[cpu5ptr] = cpuLoad;
            cpuq5avg = (cpuq5avg * (datasize - 1) - cpuq5minLoad[cpuq5ptr] + cpuqLoad) / datasize;

            cpu5max = cpu5minLoad[0];
            cpuq5max = cpuq5minLoad[0];

            for (int i = 1; i < datasize; i++)
            {
                if (cpu5minLoad[i] > cpu5max) cpu5max = cpu5minLoad[i];
                if (cpuq5minLoad[i] > cpuq5max) cpuq5max = cpuq5minLoad[i];
            }
            cpu5ptr = (cpu5ptr + 1) % datasize;
            cpuq5ptr = (cpuq5ptr + 1) % datasize;
            sw.Stop();
            ticks = sw.ElapsedMilliseconds;
        }

        public override string json()
        {
            return "{ " +
                "\"cpuLoad\": " + cpuLoad + ", " +
                "\"cpu5avg\": " + cpu5avg + ", " +
                "\"cpu5max\": " + cpu5max + ", " +
                "\"cpuqLoad\": " + cpuqLoad + ", " +
                "\"cpuq5avg\": " + cpuq5avg + ", " +
                "\"cpuq5max\": " + cpuq5max + ", " +
                "\"ticks\": " + ticks +
                " }";
        }
    }
}
