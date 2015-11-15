using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Service
{
    public class Program
    {
        public const string ServiceName = "FlowTomator";
        public static Dictionary<string, string> Parameters { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive || Environment.OSVersion.Platform == PlatformID.Unix)
            {
                ServiceBase.Run(new FlowTomatorService());
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Parameters = Environment.GetCommandLineArgs()
                                    .Where(p => p.StartsWith("/"))
                                    .Select(p => p.TrimStart('/'))
                                    .Select(p => new { Parameter = p.Trim(), Separator = p.Trim().IndexOf(':') })
                                    .ToDictionary(p => p.Separator == -1 ? p.Parameter.ToLower() : p.Parameter.Substring(0, p.Separator).ToLower(), p => p.Separator == -1 ? null : p.Parameter.Substring(p.Separator + 1));

            // Quick flag to stop the service
            if (Parameters.ContainsKey("stop"))
            {
                ServiceController service = ServiceController.GetServices().SingleOrDefault(s => s.ServiceName == Program.ServiceName);
                if (service == null)
                    return;

                if (service.Status != ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.StopPending)
                    service.Stop();

                return;
            }

            // Install service if needed
            if (Parameters.ContainsKey("reinstall"))
            {
                if (FlowTomatorService.Installed)
                {
                    try
                    {
                        FlowTomatorService.Uninstall();
                    }
                    catch (Exception e)
                    {
                        if (Debugger.IsAttached)
                            throw;
                        else
                            MessageBox.Show("Could not uninstall FlowTomator service. " + e.Message);
                    }
                    
                    Thread.Sleep(1000);
                }

                try
                {
                    FlowTomatorService.Install();
                }
                catch (Exception e)
                {
                    if (Debugger.IsAttached)
                        throw;
                    else
                        MessageBox.Show("Could not install FlowTomator service. " + e.Message);
                }
            }
            else if (Parameters.ContainsKey("install"))
            {
                if (!FlowTomatorService.Installed)
                {
                    try
                    {
                        FlowTomatorService.Install();
                    }
                    catch (Exception e)
                    {
                        if (Debugger.IsAttached)
                            throw;
                        else
                            MessageBox.Show("Could not install FlowTomator service. " + e.Message);
                    }
                }
            }

            // Start FlowTomator service UI
            Application.Run(new FlowTomatorApplication());
        }
    }
}