using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;

namespace SamanaMonitor
{
    public partial class SamanaMonitor : ServiceBase
    {
        Logging log;
        System.Timers.Timer timer;
        ServerData sd;
        private Thread listenerThread;
        AsynchronousSocketListener l;
        int port;
        int debug;
        int Interval;
        bool running;

        public SamanaMonitor()
        {
            debug = 0;
            port = 0;
            InitializeComponent();
            log = new Logging("Samana Service Handler", 0);
            running = false;
            /*
            CanHandleSessionChangeEvent = true;
            */
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                LoadConfig();
                timer = new System.Timers.Timer();
                timer.Interval = Interval; 
                timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
                l = new AsynchronousSocketListener(sd, port, debug);
                listenerThread = new Thread(new ThreadStart(l.Start));
                listenerThread.Start();
                timer.Start();
                log.info("Samana Monitor Started.", 1);
                running = true;
            }
            catch (Exception e)
            {
                log.error(e.Message + "\r\n" + e.StackTrace, 2);
                Stop();
            }
        }

        protected void LoadConfig()
        {
            ServerDataConfig sconfig = new ServerDataConfig();
            var k = Registry.LocalMachine.OpenSubKey("Software\\Samana Group\\SamanaMonitor");
            if (k == null)
                throw new Exception("Unable to load configuration from registry.");

            port     = (int)k.GetValue("Port", 11000);
            debug    = (int)k.GetValue("Debug", 0);
            sconfig.debug_level = debug;
            Interval = (int)k.GetValue("Interval", 6000); // poller interval default 6s
            sconfig.Interval = Interval;

            // extract data every 5 minutes (6*50=300)
            sconfig.app_sample_rate = (int)k.GetValue("AppLogSample", 50);
            sconfig.app_hours = (int)k.GetValue("AppLogHours", 2);
            sconfig.app_level = (int)k.GetValue("AppLogLevel", (int)EventLevel.Warning);

            // extract data every 5 minutes (6*50=300)
            sconfig.sys_sample_rate = (int)k.GetValue("SysLogSample", 50);
            sconfig.sys_hours = (int)k.GetValue("SysLogHours", 2);
            sconfig.sys_level = (int)k.GetValue("SysLogLevel", (int)EventLevel.Warning);

            // extract data every 1 minute (6*10=60)
            sconfig.ServiceSampleRate = (int)k.GetValue("ServiceSampleRate", 10);

            // extract data every 1 minute (6*10=60)
            sconfig.HDSampleRate =      (int)k.GetValue("HDSampleRate", 10);

            // extract data every 6 seconds (6*1=6)
            sconfig.CPUSampleRate =     (int)k.GetValue("CPUSampleRate", 1);

            // extract data every 1 minute (6*10=60)
            sconfig.RAMSampleRate =     (int)k.GetValue("RAMSampleRate", 10);

            // extract data every 5 minutes (6*50=300)
            sconfig.ProcessSampleRate = (int)k.GetValue("ProcessSampleRate", 50);

            // extract data every 1 minute (6*10=60)
            sconfig.SessionSampleRate = (int)k.GetValue("SessionSampleRate", 10);

            sd = new ServerData(sconfig);
            log.debug(1, "Configuration Loaded", 104);
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            log.debug(1, "Pollig for information from the System", 109);

            if (!listenerThread.IsAlive)
            {
                Stop();
            }
            try
            {
                sd.Poll();
            }
            catch (Exception e)
            {
                log.error(e.Message + "\r\n" + e.StackTrace, 5);
                if(l != null)
                {
                    l.Stop();
                }
            }
        }

        protected override void OnStop()
        {
            if (running)
                log.info("Samana Monitor Stopped.", 6);
            if (l != null)
            {
                l.Stop();
            }
        }

        /*
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            if(debug > 0)
            {
                eventLog1.WriteEntry("Session " + changeDescription.SessionId + " changed state in RDP. " + changeDescription.Reason.ToString(),
                    EventLogEntryType.Information,
                    (int)changeDescription.Reason);
            }

            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
                    sd.AddSession(changeDescription.SessionId);
                    break;
                case SessionChangeReason.SessionLogoff:
                    sd.RemoveSession(changeDescription.SessionId);
                    break;
                case SessionChangeReason.RemoteConnect:
                case SessionChangeReason.RemoteDisconnect:
                case SessionChangeReason.SessionLock:
                case SessionChangeReason.SessionUnlock:
                    sd.UpdateSession(changeDescription.SessionId);
                    break;
            }
            base.OnSessionChange(changeDescription);
        }
        */
    }
}
