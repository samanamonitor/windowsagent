﻿using System.ServiceProcess;

namespace SamanaMonitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SamanaMonitor()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
