using System;
using System.Collections;
using System.ComponentModel;
using System.ServiceProcess;
using System.Security.Cryptography.X509Certificates;
using NetFwTypeLib;
using Microsoft.Win32;

namespace SamanaMonitor
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            int port = 11000;

            try
            {
                port = Convert.ToInt16(Context.Parameters["TCPPort"]);
                if(port > 65535 || port < 1024)
                {
                    port = 11000;
                }
            } catch
            {
                port = 11000;
            }
            InstallCert(StripDir(Context.Parameters["assemblypath"]));
            AddFirewallRule(port.ToString());
            InstallRegKeys(port);
            
            base.Install(stateSaver);
        }
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            /*
            ServiceController svc = new ServiceController("SamanaMonitor");
            if (svc != null)
            {
                svc.Start();
            }
            */
        }
        public override void Uninstall(IDictionary savedState)
        {
            DeleteFirewallRule();
            DeleteCert();
            DeleteRegKeys();
            base.Uninstall(savedState);
        }
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
            DeleteFirewallRule();
            DeleteCert();
            DeleteRegKeys();
        }
        private string StripDir(string fullPath)
        {
            string retValue = default(string);
            if (fullPath != null && fullPath != string.Empty && fullPath != default(string))
            {
                retValue = fullPath.Substring(0, fullPath.LastIndexOf(@"\"));
            }
            return retValue;
        }
        private void InstallRegKeys(int port)
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey("Software\\Samana Group\\SamanaMonitor");
            key.SetValue("Port", port, RegistryValueKind.DWord);
            key.SetValue("Interval", 6000, RegistryValueKind.DWord);
        }
        private void DeleteRegKeys()
        {
            try
            {
                Registry.LocalMachine.DeleteSubKey("Software\\Samana Group\\SamanaMonitor");
                Registry.LocalMachine.DeleteSubKey("Software\\Samana Group");
            }
            catch
            {
                return;
            }
        }
        private void InstallCert(string path)
        {
            X509Certificate2 cert = new X509Certificate2(path + "\\samanamonitor.pfx", "Samana81.",
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
            if (cert == null) return;

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
            } finally
            {
                store.Close();
            }
        }
        private void DeleteCert()
        {
            X509Store store = new X509Store(StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadWrite);

                X509Certificate2Collection certCollection = store.Certificates;

                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectName, "SamanaGroup", false);
                if (signingCert.Count == 0)
                    return;

                store.Remove(signingCert[0]);
            }
            finally
            {
                store.Close();
            }
        }
        private void AddFirewallRule(string p)
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Description = "Used to allow Samana Monitor access.";
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            firewallRule.LocalPorts = p;
            firewallRule.Name = "SamanaMonitor";

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            firewallPolicy.Rules.Add(firewallRule);
        }
        private void DeleteFirewallRule()
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Remove("SamanaMonitor");
        }
    }
}
