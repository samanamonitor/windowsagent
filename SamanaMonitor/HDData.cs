using System.Diagnostics;
using System.IO;
using SamanaMonitor;
using System;

public class HDList : JSONItemList
{
    private Logging log;
    int sample_rate;

    public HDList(ServerDataConfig c)
    {
        log = new Logging("HD Data Collector", 0);
        sample_rate = c.HDSampleRate;
        log.debug(1, "Created HD Data", 1);
    }
    public void Poll(int Tick)
    {
        if (sample_rate <= 0 || Tick % sample_rate != 0) return;
        log.debug(1, "Polling HD data", 0);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        this.Clear();
        try
        {
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.DriveType == System.IO.DriveType.Fixed)
                {
                    if(d.TotalSize < 1)
                    {
                        log.error("Disk " + d.Name + " has invalid size " + d.TotalSize + " skipping drive.", 203);
                        continue;
                    }
                    if (d.TotalFreeSpace < 1)
                    {
                        log.error("Disk " + d.Name + " has invalid free size " + d.TotalSize + " skipping drive.", 204);
                        continue;
                    }
                    this.Add(new HDData(d.Name, d.TotalSize, d.TotalFreeSpace));
                }
            }
        } 
        catch (Exception e)
        {
            log.error(e.Message + e.StackTrace, 205);
        }
        sw.Stop();
        this.ticks = sw.ElapsedMilliseconds;
    }
}

public class HDData : JSONItem
{
    private string Name;
    private long TotalSize;
    private long _Used;
    private long _Free;
    private int PercUsed;

    

    public HDData(string n, long t, long f)
    {
        /*
         * Assuming t > 0 and f > 0
         */
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Name = n;
        TotalSize = t / 1048576;
        _Free = f / 1048576;
        _Used = TotalSize - _Free;
        PercUsed = (int)(_Used * 100 / TotalSize);
        sw.Stop();
        ticks = sw.ElapsedMilliseconds;
    }

    public long Free
    {
        get { return _Free;  }
        set
        {
            _Free = value / 1048576;
            _Used = TotalSize - _Free;
            PercUsed = (int)(_Used * 100 / TotalSize);
        }
    }

    public override string json()
    {
        return "{ " +
            "\"name\": " + escape(Name) + ", " +
            "\"totalsize\": " + TotalSize + ", " +
            "\"used\": " + _Used + ", " +
            "\"free\": " + _Free + ", " +
            "\"percused\": " + PercUsed + ", " +
            "\"ticks\": " + ticks + " }";
    }
}
