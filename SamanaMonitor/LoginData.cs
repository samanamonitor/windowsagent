using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;

namespace SamanaMonitor
{
    public class LoginData : JSONItem
    {
        public string UserName;
        public string UserDomain;
        public UInt64 LogonId;
        private DateTime LogonTime;
        private UInt64 WinlogonPid;
        private SecurityIdentifier UserSID;
        private bool npTime;
        private DateTime npStart;
        private DateTime npEnd;
        private bool upmTime;
        private DateTime upmStart;
        private DateTime upmEnd;
        private bool userProfileTime;
        private DateTime userProfileStart;
        private DateTime userProfileEnd;
        private bool gpTime;
        private DateTime gpStart;
        private DateTime gpEnd;
        private bool gpScriptTime;
        private DateTime gpScriptStart;
        private DateTime gpScriptEnd;
        private bool uiTime;
        private DateTime uiStart;
        private DateTime uiEnd;

        public LoginData(SessionItem si)
        {
            UserName = si.username;
            UserDomain = si.domainname;
            string timestart = si.LogonTime.AddSeconds(-10).ToString("s") + "Z";
            string timeend = si.LogonTime.AddSeconds(10).ToString("s") + "Z";

            string queryString =
                "<QueryList>" +
                    "  <Query Id=\"0\" Path=\"Security\">\n" +
                    "    <Select Path=\"Security\">\n" +
                    "        *[System[(EventID=4624) and  TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                    "        and *[EventData[Data[@Name = 'TargetUserName'] and (Data = \"" + si.username + "\")]]\n" +
                    "        and *[EventData[Data[@Name = 'TargetDomainName'] and(Data = \"" + si.domainname + "\")]]\n" +
                    "        and *[EventData[Data[@Name = 'LogonType'] and(Data = \"2\" or Data = \"10\" or Data = \"11\")]]\n" +
                    "        and *[EventData[Data[@Name = 'ProcessName'] and(Data = \"C:\\Windows\\System32\\winlogon.exe\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";

            EventLogQuery eventsQuery = new EventLogQuery("Security", PathType.LogName, queryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);

            EventRecord e = logReader.ReadEvent();
            LogonTime = ((DateTime)e.TimeCreated).ToUniversalTime();
            WinlogonPid = (UInt64)e.Properties[16].Value;
            LogonId = (UInt64)e.Properties[7].Value;
            UserSID = (SecurityIdentifier)e.Properties[4].Value;
        }

        public void ProcessLogon()
        {
            npTime = NetworkProviders();
            upmTime = CitrixUPM();
            userProfileTime = UserProfile();
            gpTime = GroupPolicy();
            gpScriptTime = GroupPolicyScript();
            uiTime = UserInit();
        }

        private bool NetworkProviders()
        {
            UInt64 Pid;
            string timestart = LogonTime.ToString("s");
            string StartQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Security\">\n" +
                    "    <Select Path=\"Security\">\n" +
                    "        *[System[(EventID=4688) and  TimeCreated[@SystemTime &gt; '" + timestart + "']]]\n" +
                    "        and *[EventData[Data[@Name = 'ProcessId'] and (Data = \"0x" + WinlogonPid.ToString("X") + "\")]]\n" +
                    "        and *[EventData[Data[@Name = 'NewProcessName'] and(Data = \"C:\\Windows\\System32\\mpnotify.exe\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";

            EventLogQuery eventsQuery = new EventLogQuery("Security", PathType.FilePath, StartQueryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);

            EventRecord e = logReader.ReadEvent();
            if (e == null) return false;

            npStart = (DateTime)e.TimeCreated;
            Pid = (UInt64)e.Properties[4].Value;
            string EndQueryString =
            "<QueryList>\n" +
                "  <Query Id=\"0\" Path=\"Security\">\n" +
                "    <Select Path=\"Security\">\n" +
                "        *[System[(EventID=4689) and  TimeCreated[@SystemTime &gt; '" + npStart.ToString("s") + "']]]\n" +
                "        and *[EventData[Data[@Name = 'ProcessId'] and (Data = \"0x" + Pid.ToString("X") + "\")]]\n" +
                "        and *[EventData[Data[@Name = 'ProcessName'] and(Data = \"C:\\Windows\\System32\\mpnotify.exe\")]]\n" +
                "    </Select>\n" +
                "  </Query>\n" +
                "</QueryList>\n";
            eventsQuery = new EventLogQuery("Security", PathType.FilePath, EndQueryString);
            logReader = new EventLogReader(eventsQuery);
            e = logReader.ReadEvent();
            if (e == null) return false;

            npEnd = (DateTime)e.TimeCreated;
            return true;
        }

        private bool CitrixUPM()
        {
            if (!EventLog.SourceExists("Citrix Profile management")) return false;

            string timestart = LogonTime.ToString("s");
            string timeend = LogonTime.AddHours(1).ToString("s");
            string StartQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Application\">\n" +
                    "    <Select Path=\"Application\">\n" +
                    "        *[System[(EventID=10) " +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']" +
                    "            and Provider[@Name='Citrix Profile management']]]\n" +
                    "        and *[EventData[Data and (Data = \"" + UserName + "\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            EventLogQuery eventsQuery = new EventLogQuery("Citrix Profile management", PathType.FilePath, StartQueryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);
            EventRecord e = logReader.ReadEvent();
            if (e == null) return false;

            upmStart = (DateTime)e.TimeCreated;
            string EndQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-User Profile Service/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-User Profile Service/Operational\">\n" +
                    "        *[System[(EventID=1) " +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']" +
                    "            and Security[@UserID='" + UserSID.ToString() + "']]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            eventsQuery = new EventLogQuery("Microsoft-Windows-User Profile Service/Operational", PathType.FilePath, EndQueryString);
            logReader = new EventLogReader(eventsQuery);
            e = logReader.ReadEvent();
            if (e == null) return false;

            upmEnd = (DateTime)e.TimeCreated;
            return true;
        }

        private bool UserProfile()
        {
            string timestart = LogonTime.ToString("s");
            string timeend = LogonTime.AddHours(1).ToString("s");
            string StartQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-User Profile Service/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-User Profile Service/Operational\">\n" +
                    "        *[System[(EventID=1)\n" +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']\n" +
                    "            and Security[@UserID='" + UserSID.ToString() + "']]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            EventLogQuery eventsQuery = new EventLogQuery("Microsoft-Windows-User Profile Service/Operational", PathType.FilePath, StartQueryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);
            EventRecord e = logReader.ReadEvent();
            if (e == null) return false;

            userProfileStart = (DateTime)e.TimeCreated;

            string EndQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-User Profile Service/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-User Profile Service/Operational\">\n" +
                    "        *[System[(EventID=2)\n" +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']\n" +
                    "            and Security[@UserID='" + UserSID.ToString() + "']]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            eventsQuery = new EventLogQuery("Microsoft-Windows-User Profile Service/Operational", PathType.FilePath, EndQueryString);
            logReader = new EventLogReader(eventsQuery);
            e = logReader.ReadEvent();
            if (e == null) return false;

            userProfileEnd = (DateTime)e.TimeCreated;
            return true;
        }

        private bool GroupPolicy()
        {
            string timestart = LogonTime.ToString("s");
            string timeend = LogonTime.AddHours(1).ToString("s");
            string StartQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "        *[System[(EventID=4001)\n" +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                    "        and *[EventData[Data[@Name='PrincipalSamName'] and (Data=\"" + UserDomain + "\\" + UserName + "\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            EventLogQuery eventsQuery = new EventLogQuery("Microsoft-Windows-GroupPolicy/Operational", PathType.FilePath, StartQueryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);
            EventRecord e = logReader.ReadEvent();
            if (e == null) return false;

            gpStart = (DateTime)e.TimeCreated;

            string EndQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "        *[System[(EventID=8001) " +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                    "        and *[EventData[Data[@Name='PrincipalSamName'] and (Data=\"" + UserDomain + "\\" + UserName + "\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            eventsQuery = new EventLogQuery("Microsoft-Windows-User Profile Service/Operational", PathType.FilePath, EndQueryString);
            logReader = new EventLogReader(eventsQuery);
            e = logReader.ReadEvent();
            if (e == null) return false;

            gpEnd = (DateTime)e.TimeCreated;
            return true;
        }

        private bool GroupPolicyScript()
        {
            string timestart = LogonTime.ToString("s");
            string timeend = LogonTime.AddHours(1).ToString("s");
            string StartQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "        *[System[(EventID=4018) " +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                    "        and *[EventData[Data[@Name='PrincipalSamName'] and (Data=\"" + UserDomain + "\\" + UserName + "\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            EventLogQuery eventsQuery = new EventLogQuery("Microsoft-Windows-GroupPolicy/Operational", PathType.FilePath, StartQueryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);
            EventRecord e = logReader.ReadEvent();
            if (e == null) return false;

            gpScriptStart = (DateTime)e.TimeCreated;

            string EndQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "    <Select Path=\"Microsoft-Windows-GroupPolicy/Operational\">\n" +
                    "        *[System[(EventID=5018)\n" +
                    "            and TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                    "        and *[EventData[Data[@Name='PrincipalSamName'] and (Data=\"" + UserDomain + "\\" + UserName + "\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            eventsQuery = new EventLogQuery("Microsoft-Windows-User Profile Service/Operational", PathType.FilePath, EndQueryString);
            logReader = new EventLogReader(eventsQuery);
            e = logReader.ReadEvent();
            if (e == null) return false;

            gpScriptEnd = (DateTime)e.TimeCreated;
            return true;
        }

        private bool UserInit()
        {
            string timestart = LogonTime.ToString("s");
            string timeend = LogonTime.AddHours(1).ToString("s");
            UInt64 uiProcessId;
            string StartQueryString =
                "<QueryList>\n" +
                "  <Query Id=\"0\" Path=\"Security\">\n" +
                "    <Select Path=\"Security\">\n" +
                "        *[System[(EventID=4688) and  TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                "        and *[EventData[Data[@Name = 'ProcessId'] and (Data = \"0x" + WinlogonPid.ToString("X") + "\")]]\n" +
                "        and *[EventData[Data[@Name = 'NewProcessName'] and(Data = \"C:\\Windows\\System32\\userinit.exe\")]]\n" +
                "    </Select>\n" +
                "  </Query>\n" +
                "</QueryList>\n";

            EventLogQuery eventsQuery = new EventLogQuery("Security", PathType.FilePath, StartQueryString);
            EventLogReader logReader = new EventLogReader(eventsQuery);

            EventRecord e = logReader.ReadEvent();
            if (e == null) return false;

            uiStart = (DateTime)e.TimeCreated;
            uiProcessId = (UInt64)e.Properties[4].Value;
            string EndQueryString =
                "<QueryList>\n" +
                    "  <Query Id=\"0\" Path=\"Security\">\n" +
                    "    <Select Path=\"Security\">\n" +
                    "        *[System[(EventID=4688) and  TimeCreated[@SystemTime &gt; '" + timestart + "' and @SystemTime &lt; '" + timeend + "']]]\n" +
                    "        and *[EventData[Data[@Name = 'ProcessId'] and (Data = \"0x" + uiProcessId.ToString("X") + "\")]]\n" +
                    "        and *[EventData[Data[@Name = 'NewProcessName'] and\n" +
                    "            (Data = \"C:\\Program Files (x86)\\Citrix\\system32\\icast.exe\")\n" +
                    "            or (Data = \"C:\\Windows\\explorer.exe\")]]\n" +
                    "    </Select>\n" +
                    "  </Query>\n" +
                    "</QueryList>";
            eventsQuery = new EventLogQuery("Security", PathType.FilePath, EndQueryString);
            logReader = new EventLogReader(eventsQuery);
            e = logReader.ReadEvent();

            if (e == null) return false;

            uiEnd = (DateTime)e.TimeCreated;
            return true;
        }

        public override string json()
        {
            string outstring = "{ "
                + "\"LogonID\": " + id + ", "
                + "\"LogonTime\": \"" + LogonTime.ToString("s") + "Z\", "
                + "\"WinlogonPid\": " + WinlogonPid + ", "
                + "\"UserSID\": \"" + UserSID.ToString() + "\", ";
            if (npTime) outstring += "\"networkprovider\": " + (npEnd - npStart).Milliseconds + ", ";
            if (upmTime) outstring += "\"CitrixUPM\": " + (upmEnd - upmStart).Milliseconds + ", ";
            if (userProfileTime) outstring += "\"UserProfile\": " + (userProfileEnd - userProfileStart).Milliseconds + ", ";
            if (gpTime) outstring += "\"GroupPolicy\": " + (gpEnd - gpStart).Milliseconds + ", ";
            if (gpScriptTime) outstring += "\"GroupPolicyScript\": " + (gpScriptEnd - gpScriptStart).Milliseconds + ", ";
            if (uiTime) outstring += "\"UserInit\": " + (uiEnd - uiStart).Milliseconds + ", ";
            outstring += "\"ticks\": " + ticks
                + "} ";
            return outstring;
        }
    }
}
