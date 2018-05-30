using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace SamanaMonitor
{
    public enum EventPath { System, Application };
    public enum EventLevel { Error=2, Warning=3, Info=4 };

    public class EventList : JSONItemList
    {
        private int sample_rate;
        private Logging log;
        private int event_level;
        private string path;
        private int hours;
        private int debug_level;

        public EventList(ServerDataConfig c, EventPath p)
        {
            log = new Logging("Event Data Collector", c.debug_level);
            debug_level = c.debug_level;
            if (p == EventPath.System)
            {
                path = "System";
                hours = c.sys_hours;
                event_level = c.sys_level;
                sample_rate = c.sys_sample_rate;
            }
            else if (p == EventPath.Application)
            {
                hours = c.app_hours;
                path = "Application";
                event_level = c.app_level;
                sample_rate = c.app_sample_rate;
            }
            log.debug(1, "Created eventlist " + path, 1);
        }
        public EventList(string _path, int _event_level, int _hours, int _sample_rate, int _debug)
        {
            log = new Logging("Event Data Collector", _debug);
            path = _path;
            event_level = _event_level;
            hours = _hours;
            sample_rate = _sample_rate;
            log.debug(1, "Created eventlist " + path, 1);
        }
        public void Poll(int Tick)
        {
            if (sample_rate <= 0 || Tick % sample_rate != 0) return;
            log.debug(1, "Polling event data " + path, 0);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.Clear();

            string queryString =
                "<QueryList>" +
                    "  <Query Id=\"0\" Path=\"" + path + "\">" +
                    "    <Select Path=\"" + path + "\">" +
                    "        *[System[(Level &lt;= " + event_level + ") and (Level != 0) and " +
                    "        TimeCreated[timediff(@SystemTime) &lt;= " + (hours * 3600000) + "]]]" +
                    "    </Select>" +
                    "  </Query>" +
                    "</QueryList>";
            log.debug(2, "QueryString=" + queryString, 0);
            EventLogQuery eventsQuery = new EventLogQuery(path, PathType.FilePath, queryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);

            for (EventRecord e = logReader.ReadEvent(); e != null; e = logReader.ReadEvent())
            {
                EventItem temp;
                try
                {
                    temp = new EventItem(e.Id,
                        e.FormatDescription(),
                        e.LevelDisplayName,
                        (int)e.Level,
                        e.ProviderName,
                        e.TimeCreated.ToString());
                }
                catch
                {
                    temp = new EventItem(e.Id,
                        "Unable to get data",
                        "Unable to get data",
                        (int)e.Level,
                        e.ProviderName,
                        e.TimeCreated.ToString());
                }
                this.Add(temp);
            }
            sw.Stop();
            this.ticks = sw.ElapsedMilliseconds;
        }
    }

    public class EventItem : JSONItem
    {
        public long eventId;
        public string message;
        public string entryType;
        public int level;
        public string source;
        public string timeGenerated;
        private Logging log;

        public EventItem(long _eventId, string _message, string _entryType, int _level, string _source, string _timeGenerated)
        {
            Stopwatch sw = new Stopwatch();
            log = new Logging("Event Item", 0);
            sw.Start();
            eventId = _eventId;
            message = _message;
            entryType = _entryType;
            level = _level;
            source = _source;
            timeGenerated = _timeGenerated;
            sw.Stop();
            ticks = sw.ElapsedMilliseconds;
        }

        public override string json()
        {
            string ret;

            try
            {
                ret = "{ \"eventId\":" + eventId.ToString() + ", " +
                    "\"message\": " + escape(message) + ", " +
                    "\"entryType\": " + escape(entryType.ToString()) + ", " +
                    "\"level\": " + level + ", " +
                    "\"source\": " + escape(source) + ", " +
                    "\"timeGenerated\": " + escape(timeGenerated) + ", " +
                    "\"ticks\": " + ticks +
                    " }";
            }
            catch (Exception e)
            {
                log.error(e.Message + e.StackTrace, 1);
                ret = "{ }";
            }
            return ret;
        }

    }
}
