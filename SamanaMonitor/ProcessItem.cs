using System.Diagnostics;
using System;

namespace SamanaMonitor
{
    class ProcessList : JSONItemList
    {
        private int sample_rate;
        private Logging log;
        private int debug_level;

        public ProcessList(ServerDataConfig c)
        {
            log = new Logging("Process Data Collector", c.debug_level);
            debug_level = c.debug_level;
            sample_rate = c.ProcessSampleRate;
            log.debug(1, "Created Process Data Collector", 1);
        }
        public void Poll(int Tick)
        {
            if (this.sample_rate == 0 || Tick % this.sample_rate != 0) return;
            log.debug(1, "Polling Process data", 0);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            log.debug(1, "Polling Processes", 1);
            this.Clear();
            Process[] pl = Process.GetProcesses();
            for (int i = 0; i < pl.Length; i++)
            {
                if (pl[i].Id == 0) continue; // Skip Idle process
                this.Add(new ProcessItem(pl[i]));
            }
            sw.Stop();
            this.ticks = sw.ElapsedMilliseconds;
        }
    }

        class ProcessItem : JSONItem
    {
        private Process _data;

        public ProcessItem(Process p)
        {
            _data = p;
        }

        public override string json()
        {
            string outstring;

            try
            {
                outstring = "{ " +
                "\"id\": " + _data.Id + ", " +
                "\"ProcessName\": \"" + _data.ProcessName + "\", " +
                "\"SessionId\": " + _data.SessionId + ", " +
                "\"StartTime\": \"" + _data.StartTime.ToString() + "\", " +
                "\"HandleCount\": " + _data.HandleCount + ", " +
                "\"Threads\": " + _data.Threads.Count + ", " +
                "\"TotalProcessorTime\": " + _data.TotalProcessorTime.Seconds + ", " +
                "\"UserProcessorTime\": " + _data.UserProcessorTime.Seconds + ", " +
                "\"PrivilegedProcessorTime\": " + _data.PrivilegedProcessorTime.Seconds + ", " +
                "\"NonpagedSystemMemorySize\": " + _data.NonpagedSystemMemorySize64 + ", " +
                "\"PagedSystemMemorySize\": " + _data.PagedSystemMemorySize64 + ", " +
                "\"PagedMemorySize\": " + _data.PagedMemorySize64 + ", " +
                "\"PeakPagedMemorySize\": " + _data.PeakPagedMemorySize64 + ", " +
                "\"PeakWorkingSet\": " + _data.PeakWorkingSet64 + ", " +
                "\"PrivateMemorySize\": " + _data.PrivateMemorySize64 + ", " +
                "\"PeakVirtualMemorySize\": " + _data.PeakVirtualMemorySize64 + ", " +
                "\"VirtualMemorySize\": " + _data.VirtualMemorySize64 + ", " +
                "\"MaxWorkingSet\": " + _data.MaxWorkingSet + ", " +
                "\"MinWorkingSet\": " + _data.MinWorkingSet + ", " +
                "\"WorkingSet64\": " + _data.WorkingSet64 + ", " +

                "\"ticks\": " + ticks + " }";
            }
            catch
            {
                outstring = "{ " +
                    "\"id\": " + _data.Id + ", " +
                    "\"ProcessName\": \"" + _data.ProcessName + "\", " +
                    "\"ticks\": " + ticks + " }";
            }

            return outstring;
        }
    }

}
