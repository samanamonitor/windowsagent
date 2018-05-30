using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

public enum WTS_CONNECTSTATE_CLASS
{
    WTSActive,
    WTSConnected,
    WTSConnectQuery,
    WTSShadow,
    WTSDisconnected,
    WTSIdle,
    WTSListen,
    WTSReset,
    WTSDown,
    WTSInit
}

[StructLayout(LayoutKind.Sequential)]
public struct WTSINFO
{
    public WTS_CONNECTSTATE_CLASS State;
    public Int32 SessionId;
    public Int32 IncomingBytes;
    public Int32 OutgoingBytes;
    public Int32 IncomingFrames;
    public Int32 OutgoingFrames;
    public Int32 IncomingCompressedBytes;
    public Int32 OutgoingCompressedBytes;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string WinStationName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
    public string Domain;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
    public string UserName;

    public Int64 ConnectTime;
    public Int64 DisconnectTime;
    public Int64 LastInputTime;
    public Int64 LogonTime;
    public Int64 CurrentTime;
}

namespace SamanaMonitor
{
    public class SessionList : JSONItemList
    {
        ServerDataConfig config;
        private EventLog eventLog1;

        [DllImport("wtsapi32.dll")]
        static extern Int32 WTSEnumerateSessions(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] Int32 Reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 Version,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("Wtsapi32.dll")]
        static extern bool WTSQuerySessionInformation(
            System.IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
            WTSSessionInfoEx,
            WTSConfigInfo,
            WTSValidationInfo,
            WTSSessionAddressV4,
            WTSIsRemoteSession
        }

        public SessionList(ServerDataConfig c)
        {
            config = c;
            eventLog1 = new EventLog();
            eventLog1.Source = "Samana Service Session query";
            eventLog1.Log = "SamanaMonitorLog";
        }

        public void Poll()
        {
            IntPtr WTS_CURRENT_SERVER_HANDLE = (IntPtr)null;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                IntPtr SessionInfoPtr = IntPtr.Zero;
                Int32 sessionCount = 0;
                Int32 retVal = WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref SessionInfoPtr, ref sessionCount);
                Int64 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                IntPtr currentSession = SessionInfoPtr;

                if (retVal != 0)
                {
                    Dictionary<int, int> cs = new Dictionary<int, int>();
                    for (int i = 0; i < this.Count; i++)
                    {
                        cs[this[i].id] = i;
                    }
                    for (int i = 0; i < sessionCount; i++)
                    {
                        WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure(currentSession, typeof(WTS_SESSION_INFO));
                        currentSession = new IntPtr(currentSession.ToInt64() + dataSize);
                        if (si.SessionID == 0 || si.SessionID == 65536) continue;

                        if (!cs.ContainsKey(si.SessionID))
                        {
                            Add(si.SessionID);
                        }
                        else
                        {
                            cs.Remove(si.SessionID);
                        }
                    }
                    foreach (KeyValuePair<int, int> entry in cs)
                    {
                        this.Remove(this[entry.Value]);
                    }

                    WTSFreeMemory(SessionInfoPtr);
                }
            }
            catch (Exception e)
            {
                eventLog1.WriteEntry(e.Message + e.StackTrace, EventLogEntryType.Error, 1);
            }
            sw.Stop();
            ticks = sw.ElapsedMilliseconds;

        }

        public void Remove(int id)
        {
            for (int i = 0; i < Count; i++)
            {
                if (id == this[i].id)
                {
                    Remove((SessionItem)this[i]);
                    break;
                }
            }
        }

        public void Add(int id)
        {
            IntPtr WTS_CURRENT_SERVER_HANDLE = (IntPtr)null;
            IntPtr sessioninfo = IntPtr.Zero;
            uint bytes = 0;

            WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, id, WTS_INFO_CLASS.WTSSessionInfo, out sessioninfo, out bytes);
            try
            {
                SessionItem si = new SessionItem((WTSINFO)Marshal.PtrToStructure(sessioninfo, typeof(WTSINFO)));
                Add(si);
            }
            catch (Exception e)
            {
                eventLog1.WriteEntry(e.Message + e.StackTrace, EventLogEntryType.Error, 1);
            }
            WTSFreeMemory(sessioninfo);
        }

        public void Update(int id)
        {
            int i;
            for (i = 0; i < Count; i++)
            {
                if (this[i].id == id)
                {
                    break;
                }
            }
            if (i == Count)
            {
                eventLog1.WriteEntry("Session " + id + " not in list", EventLogEntryType.Information, 8);
                return;
            }
            IntPtr WTS_CURRENT_SERVER_HANDLE = (IntPtr)null;
            IntPtr sessioninfo = IntPtr.Zero;
            uint bytes = 0;

            WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, id, WTS_INFO_CLASS.WTSSessionInfo, out sessioninfo, out bytes);

            ((SessionItem)this[i]).UpdateItem((WTSINFO)Marshal.PtrToStructure(sessioninfo, typeof(WTSINFO)));

            WTSFreeMemory(sessioninfo);
        }

    }

    public class SessionItem : JSONItem
    {
        public string username;
        public string domainname;
        public string stationname;
        public WTS_CONNECTSTATE_CLASS connectstate;
        public DateTime ConnectTime;
        public DateTime DisconnectTime;
        public DateTime LastInputTime;
        public DateTime LogonTime;
        public DateTime CurrentTime;
        public TimeSpan IdleTime;
        public TimeSpan SessionTime;
        public UInt64 LogonID;
        public LoginData ld;

        public SessionItem(int i, string u, string d, string s, WTS_CONNECTSTATE_CLASS c)
        {
            id = i;
            username = u;
            domainname = d;
            stationname = s;
            connectstate = c;
        }

        public SessionItem(WTSINFO si)
        {
            id = si.SessionId;
            username = si.UserName;
            domainname = si.Domain;
            stationname = si.WinStationName;
            connectstate = si.State;
            ConnectTime = new DateTime(si.ConnectTime).AddYears(1600);
            DisconnectTime = new DateTime(si.DisconnectTime).AddYears(1600);
            LastInputTime = new DateTime(si.LastInputTime).AddYears(1600);
            LogonTime = new DateTime(si.LogonTime).AddYears(1600);
            CurrentTime = new DateTime(si.CurrentTime).AddYears(1600);
            IdleTime = CurrentTime - LastInputTime;
            SessionTime = CurrentTime - LogonTime;

            ld = new LoginData(this);
            ld.ProcessLogon();
        }

        public void UpdateItem(int i, string u, string d, string s, WTS_CONNECTSTATE_CLASS c)
        {
            id = i;
            username = u;
            domainname = d;
            stationname = s;
            connectstate = c;
        }

        public void UpdateItem(WTSINFO si)
        {
            username = si.UserName;
            domainname = si.Domain;
            stationname = si.WinStationName;
            connectstate = si.State;
            ConnectTime = new DateTime(si.ConnectTime).AddYears(1600);
            DisconnectTime = new DateTime(si.DisconnectTime).AddYears(1600);
            LastInputTime = new DateTime(si.LastInputTime).AddYears(1600);
            LogonTime = new DateTime(si.LogonTime).AddYears(1600);
            CurrentTime = new DateTime(si.CurrentTime).AddYears(1600);
            IdleTime = CurrentTime - LastInputTime;
            SessionTime = CurrentTime - LogonTime;
        }
        public override string json()
        {
            string outstring = "{ "
                + "\"sessionId\": " + id + ", "
                + "\"username\": \"" + username + "\", "
                + "\"domain\": \"" + domainname + "\", "
                + "\"stationname\": \"" + stationname + "\", "
                + "\"connecttime\": \"" + ConnectTime + "\", "
                + "\"disconnecttime\": \"" + DisconnectTime + "\", "
                + "\"lastinputtime\": \"" + LastInputTime + "\", "
                + "\"logontime\": \"" + LogonTime.ToString("s") + "Z" + "\", "
                + "\"currenttime\": \"" + CurrentTime + "\", "
                + "\"idletime\": \"" + IdleTime + "\", "
                + "\"sessiontime\": \"" + SessionTime + "\", "
                + "\"state\": \"" + connectstate.ToString().Substring(3) + "\", ";
            if (ld != null)
            {
                outstring += "\"logindata\": " + ld.json() + ", ";
            }
            outstring += "\"ticks\": " + ticks
                + " }";
            return outstring;
        }
    }
}
