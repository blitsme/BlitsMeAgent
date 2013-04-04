﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Win32;
using BlitsMe.Service.ServiceHost;
using log4net;
using log4net.Config;

namespace BlitsMe.Service
{
    public partial class BMService : ServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BMService));
        private readonly WebClient _webClient;
        private readonly Timer _updateCheck;
#if DEBUG
        private String updateServer = "s1.i.dev.blits.me";
        private const int _updateCheckInterval = 120;
#else
        private String updateServer = "s1.i.blits.me";
        private const int _updateCheckInterval = 3600;
#endif
        // FIXME: Move this to a global config file at some point
        private const string tvncServiceName = "BlitsMeSupportService";
        private const int tvnTimeoutMS = 30000;

        public List<String> Servers;
        private System.ServiceModel.ServiceHost serviceHost;
        public BMService()
        {
            InitializeComponent();
            XmlConfigurator.Configure();
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Service.log4net.xml"));
            Logger.Info("BlitsMeService Starting Up [" + System.Environment.UserName + "]");
#if DEBUG
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.Debug("Embedded Resource : " + manifestResourceName);
            }
#endif
            // Check for update on startup
            _webClient = new WebClient();
            CheckForNewVersion();
            // check for updates every interval
            _updateCheck = new Timer(_updateCheckInterval*1000);
            _updateCheck.Elapsed += delegate { CheckForNewVersion(); };
            _updateCheck.Start();
        }

        private void CheckForNewVersion()
        {
            try
            {
                Version assemblyVersion = new Version(FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/BlitsMe.Agent.exe").FileVersion);
                String versionInfomation = _webClient.DownloadString("http://" + updateServer + "/updates/update.txt?ver=" + assemblyVersion);
                String[] versionParts = versionInfomation.Split('\n')[0].Split(':');
                Version updateVersion = new Version(versionParts[0]);
                if (assemblyVersion.CompareTo(updateVersion) < 0)
                {
                    Logger.Debug("Upgrade Available : " + assemblyVersion + " => " + updateVersion);
                    try
                    {
                        
                        Logger.Info("Downloading update " + versionParts[1]);
                        String fileLocation = Path.GetTempPath() + versionParts[1];
                        _webClient.DownloadFile("http://" + updateServer + "/updates/" + versionParts[1], fileLocation);
                        Logger.Info("Downloaded update " + versionParts[1]);
                        String logfile = Path.GetTempPath() + "BlitsMeInstall.log";
                        Logger.Info("Executing " + fileLocation + ", log file is " + logfile);
                        Process.Start(fileLocation, "/qn /lvx " + logfile);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to download update : " + e.Message, e);
                    }
                } else
                {
                    Logger.Debug("No update available, current version " + assemblyVersion + ", available version " + updateVersion + ", checking again in " + (_updateCheckInterval/60) + " minutes.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to check for update : " + e.Message, e);
            }
        }

        protected override void OnStart(string[] args)
        {
            initServers();
            // do we need this connection?
            //connection = new CloudConnection(servers);
            serviceHost = new System.ServiceModel.ServiceHost(new BlitsMeService(this), new Uri("net.pipe://localhost/BlitsMeService"));
            serviceHost.Open();
        }

        private void initServers()
        {
            if (Servers == null || Servers.Count == 0)
            {
                try
                {
                    Servers = getServerIPs();
                    saveServerIPs(Servers);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get the server IP's : " + e.Message,e);
                }
            }
        }

        public List<String> getServerIPs()
        {
            RegistryKey bmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(BLMRegistry.root);
            String ipKey = (String)bmKey.GetValue(BLMRegistry.serverIPsKey);
            return new List<String>(ipKey.Split(','));
        }

        public void saveServerIPs(List<String> newIPs)
        {
            // Lets add some
            try
            {
                RegistryKey ips = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(BLMRegistry.root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                ips.SetValue(BLMRegistry.serverIPsKey, String.Join(",", newIPs.ToArray()));
            }
            catch (Exception e2)
            {
                // TODO log something to event log
                Logger.Error("Failed to determine server IP's from the registry [" + e2.GetType() + "] : " + e2.Message,e2);
            }
        }

        public bool tvncStartService()
        {
            ServiceController service = new ServiceController(tvncServiceName);

            try
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(tvnTimeoutMS));
                }
            }
            catch (System.ServiceProcess.TimeoutException e)
            {
                Logger.Error("TightVNC service failed to start in a reasonable time : " + e.Message,e);
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("TightVNC service failed to start : " + e.Message, e);
                return false;
            }

            return true;
        }


        protected override void OnStop()
        {
            serviceHost.Close();
            Logger.Info("BlitsMe Service Shutting Down");
        }

    }
}
