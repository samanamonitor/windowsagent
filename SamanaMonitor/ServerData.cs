using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SamanaMonitor
{
    public class SamanaException : Exception
    {
        public int status;
        public string body;

        public SamanaException(int s, string b)
        {
            status = s;
            body = "<HTML><HEAD><TITLE>" + b + "</TITLE></HEAD><BODY><H1>" + b + "</H1></BODY></HTML>";
        }
    }

    public class ServerDataConfig
    {
        public int debug_level;

        public int app_level;
        public int app_sample_rate;
        public int app_hours;

        public int sys_level;
        public int sys_sample_rate;
        public int sys_hours;

        public int LogSampleRate;
        public int ServiceSampleRate;
        public int HDSampleRate;
        public int CPUSampleRate;
        public int RAMSampleRate;
        public int ProcessSampleRate;
        public int SessionSampleRate;

        public int Interval;

        public ServerDataConfig()
        {
            debug_level = 0;

            app_hours = 24;
            app_level = 1;
            app_sample_rate = 50;

            sys_hours = 24;
            sys_level = 1;
            sys_sample_rate = 50;

            ServiceSampleRate = 10;
            HDSampleRate = 10;
            CPUSampleRate = 1;
            RAMSampleRate = 10;
            ProcessSampleRate = 50;
            SessionSampleRate = 10;

            Interval = 6000;
        }
    }

    public class ServerData
    {
        [DllImport("kernel32.dll")]
        extern static ulong GetTickCount64();
        private int Tick;
        private Logging evtlog;
        public ulong uptime;

        private CPUData cpudata;
        private RamData ramdata;
        private ProcessList allProcesses;
        private EventList syslog;
        private EventList applog;
        private ServiceList Services;
        private HDList hdlist;
        private JSONItemList log;
        private SessionList sessions;
        private string version;

        private ServerDataConfig config;

        public ServerData(ServerDataConfig s)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            Tick = 0;
            evtlog = new Logging("Samana Service Data", 0);
            ComputerInfo c = new ComputerInfo();
            config = s;

            cpudata = new CPUData(config);
            allProcesses = new ProcessList(config);
            syslog = new EventList(config, EventPath.System);
            applog = new EventList(config, EventPath.Application);
            hdlist = new HDList(config);
            Services = new ServiceList(config);
            log = new JSONItemList();
            ramdata = new RamData(c.TotalPhysicalMemory / 1024 / 1024, config);
            sessions = new SessionList(config);
            Poll();
        }

        public void Poll()
        {
            cpudata.Poll(Tick);
            ramdata.Poll(Tick);
            hdlist.Poll(Tick);
            Services.Poll(Tick);
            syslog.Poll(Tick);
            applog.Poll(Tick);
            allProcesses.Poll(Tick);
            uptime = GetTickCount64();

            Tick++;
        }

        private void PollProcess()
        {
            if (config.ProcessSampleRate == 0 || Tick % config.ProcessSampleRate != 0) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            allProcesses.Clear();
            Process[] pl = Process.GetProcesses();
            for (int i = 0; i < pl.Length; i++)
            {
                if (pl[i].Id == 0) continue; // Skip Idle process
                allProcesses.Add(new ProcessItem(pl[i]));
            }
            sw.Stop();
            allProcesses.ticks = sw.ElapsedMilliseconds;
        }

        public string get(HTTPRequest r)
        {
            string body = "Request method=" + r.method;
            int status = 400;
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            string[] path_query = r.url.Split('?');
            string path = path_query[0];


            try
            {
                if (r.method != "GET") throw new SamanaException(405, "Method not Allowed");
                if (path_query.Length > 2) throw new SamanaException(500, "Bad Query");

                if (path_query.Length > 1)
                {
                    string[] args = path_query[1].Split('&');
                    for (int i = 0; i < args.Length; i++)
                    {
                        string[] t = args[i].Split('=');
                        if (t.Length == 1)
                        {
                            arguments.Add(t[0], "1");
                        }
                        else if (t.Length == 2)
                        {
                            arguments.Add(t[0], t[1]);
                        }
                        else
                        {
                            throw new SamanaException(500, "Bad Query");
                        }
                    }
                }
                if (path == "/cpu")
                {
                    body = cpudata.json();
                    status = 200;

                }
                else if (path == "/ram")
                {
                    body = ramdata.json();
                    status = 200;

                }
                else if (path == "/uptime")
                {
                    body = uptime.ToString();
                    status = 200;

                }
                else if (path == "/syslog")
                {
                    body = syslog.json();
                    status = 200;

                }
                else if (path == "/applog")
                {
                    body = applog.json();
                    status = 200;

                }
                else if (path == "/services")
                {
                    body = Services.json();
                    status = 200;

                }
                else if (path == "/hddrives")
                {
                    body = hdlist.json();
                    status = 200;

                }
                else if (path == "/processes")
                {
                    body = allProcesses.json();
                    status = 200;
                }
                else if (path == "/log")
                {
                    try
                    {
                        int e = -1;
                        int h = 2;
                        int l = 3;
                        string logname;

                        if (!arguments.ContainsKey("logname"))
                            throw new Exception("Missing log name");

                        logname = System.Uri.UnescapeDataString(arguments["logname"]);
                        if (arguments.ContainsKey("eventId"))
                            if (!int.TryParse(arguments["eventId"], out e))
                                e = -1;

                        if (arguments.ContainsKey("hours"))
                            if (!int.TryParse(arguments["hours"], out h))
                                h = 2;

                        if (arguments.ContainsKey("level"))
                            if (!int.TryParse(arguments["level"], out l))
                                l = 3;

                        EventList evtlist = new EventList(logname, l, h, 1, config.debug_level);
                        evtlist.Poll(1);

                        body = evtlist.json();
                        status = 200;
                    }
                    catch (Exception e)
                    {
                        evtlog.error(e.Message + e.StackTrace, 2);
                        throw new SamanaException(500, "Bad Query");
                    }
                }
                else if (path == "/sessions")
                {
                    sessions.Poll();
                    body = sessions.json();
                    status = 200;
                }
                else if(path == "/version")
                {
                    body = "\"" + this.version + "\"";
                    status = 200;
                }
                else
                {
                    throw new SamanaException(404, "Not Found");
                }
            }
            catch (SamanaException e)
            {
                status = e.status;
                body = e.body;
            }

            HTTPResponse res = new HTTPResponse(status, body);
            return res.ToString();
        }

        public void RemoveSession(int id)
        {
            sessions.Remove(id);
        }

        public void AddSession(int id)
        {
            sessions.Add(id);
        }

        public void UpdateSession(int id)
        {
            sessions.Update(id);
        }
    }
}
