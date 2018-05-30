using System;
using System.Diagnostics;
using System.ServiceProcess;
using SamanaMonitor;

class ServiceList : JSONItemList
{
    private Logging log;
    int sample_rate;
    int debug_level;

    public ServiceList(ServerDataConfig c)
    {
        log = new Logging("Service Data Collector", c.debug_level);
        debug_level = c.debug_level;
        sample_rate = c.ServiceSampleRate;
        log.debug(1, "Created Service Data", 1);
    }
    public void Poll(int Tick)
    {
        if (sample_rate <= 0 || Tick % sample_rate != 0) return;
        log.debug(1, "Polling Service data", 0);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        this.Clear();
        try
        {
            foreach (ServiceController s in ServiceController.GetServices())
            {
                this.Add(new ServiceItem(s.ServiceName, s.DisplayName, s.Status.ToString()));
            }
        } 
        catch (Exception e)
        {
            log.error(e.Message + e.StackTrace, 1);
        }
        sw.Stop();
        this.ticks = sw.ElapsedMilliseconds;
    }
}

class ServiceItem : JSONItem
{
    public string name;
    public string displayname;
    public string status;

    public ServiceItem(string n, string d, string s)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        name = n;
        displayname = d;
        status = s;
        sw.Stop();
        ticks = sw.ElapsedMilliseconds;
    }

    public override string json()
    {
        return "{ \"name\": " + escape(name) + ", " +
                "\"displayname\": " + escape(displayname) + ", " +
                "\"status\": " + escape(status) + ", " +
                "\"ticks\": " + ticks + 
                " }";
    }
}
