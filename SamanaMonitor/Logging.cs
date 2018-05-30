using System.Diagnostics;

namespace SamanaMonitor
{
    class Logging
    {
        private string source;
        private EventLog evt;
        private int debug_level;

        public Logging(string source, int d)
        {
            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(
                    source, "SamanaMonitorLog");
            }
            this.source = source;
            this.evt = new EventLog();
            this.evt.Source = this.source;
            this.evt.Log = "SamanaMonitorLog";
            this.debug_level = d;
        }
        public void error(string msg, int id)
        {
            this.evt.WriteEntry(msg, EventLogEntryType.Error, id);
        }
        public void warn(string msg, int id)
        {
            this.evt.WriteEntry(msg, EventLogEntryType.Warning, id);
        }
        public void info(string msg, int id)
        {
            this.evt.WriteEntry(msg, EventLogEntryType.Information, id);
        }
        public void debug(int dbglevel, string msg, int id)
        {
            if(dbglevel <= this.debug_level)
            {
                this.evt.WriteEntry("DEBUG: " + msg, EventLogEntryType.Information, id);
            }
        }
    }
}
