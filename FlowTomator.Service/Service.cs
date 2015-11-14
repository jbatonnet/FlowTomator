using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Service
{
    [RunInstaller(true)]
    public sealed class FlowTomatorServiceProcessInstaller : ServiceProcessInstaller
    {
        public FlowTomatorServiceProcessInstaller()
        {
            Account = ServiceAccount.User;
            Username = @".\" + Environment.UserName;
        }
    }

    [RunInstaller(true)]
    public sealed class FlowTomatorServiceInstaller : ServiceInstaller
    {
        public static bool Installed
        {
            get
            {
                ServiceController service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == Program.ServiceName);
                return service != null;
            }
        }

        public FlowTomatorServiceInstaller()
        {
            Description = "FlowTomator Service";
            DisplayName = Program.ServiceName;
            ServiceName = Program.ServiceName;
            StartType = ServiceStartMode.Automatic;
        }

        public static void Install()
        {
            if (Installed)
                return;

            IDictionary saveState = new Hashtable();

            using (AssemblyInstaller installer = new AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[0]))
            {
                installer.UseNewContext = true;

                try
                {
                    installer.Install(saveState);
                    installer.Commit(saveState);
                }
                catch
                {
                    try
                    {
                        installer.Rollback(saveState);
                    }
                    catch { }

                    throw;
                }
            }
        }
        public static void Uninstall()
        {
            if (!Installed)
                return;

            IDictionary saveState = new Hashtable();

            using (AssemblyInstaller installer = new AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[0]))
            {
                installer.UseNewContext = true;
                installer.Uninstall(saveState);
            }
        }
    }

    [Serializable]
    public class FlowTomatorNotification
    {
        public LogVerbosity Importance { get; private set; }
        public string Message { get; private set; }

        public FlowTomatorNotification(LogVerbosity importance, string message)
        {
            Importance = importance;
            Message = message;
        }
    }
    public delegate void FlowTomatorNotificationHandler(FlowTomatorNotification notification);

    public class LogTextWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.Default;
            }
        }
        public event Action<string> NewLine;

        string buffer = "";

        public override void Write(char[] data, int index, int count)
        {
            buffer += new string(data, index, count);

            int newLine = buffer.IndexOfAny(new char[] { '\r', '\n' });
            if (newLine >= 0)
            {
                string line = buffer.Substring(0, newLine);

                if (NewLine != null)
                    NewLine(line);

                buffer = buffer.Substring(newLine + 1).TrimStart('\r', '\n');
            }
        }
    }

    public class FlowTomatorService : ServiceBase
    {
        public Dictionary<string, string> Parameters { get; private set; }
        public ObservableCollection<FlowEnvironment> Flows { get; } = new ObservableCollection<FlowEnvironment>();
        public event FlowTomatorNotificationHandler Notification;
        public FileInfo LogFile { get; private set; }

        private ObjRef serviceRef;

        public FlowTomatorService()
        {
            ServiceName = "FlowTomator";

            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            CanStop = true;
        }

        public FlowEnvironment Load(string path)
        {
            FlowEnvironment flow = null;

            try
            {
                flow = FlowEnvironment.Load(path);
            }
            catch (Exception e)
            {
                OnNotification(new FlowTomatorNotification(LogVerbosity.Error, "Failed to load the specified flow : " + path + ". " + e.Message));
            }

            if (flow == null)
                return null;

            Flows.Add(flow);
            return flow;
        }

        private void TextWriter_Updated(string message)
        {
            if (LogFile == null)
                return;

            using (StreamWriter writer = LogFile.AppendText())
                writer.WriteLine("[{0}] {1}", DateTime.Now.ToLongTimeString(), message);
        }
        private void OnNotification(FlowTomatorNotification notification)
        {
            Log.Error(notification.Message);

            if (Notification == null)
                return;

            Delegate[] invocations = Notification.GetInvocationList();
            foreach (Delegate invocation in invocations)
            {
                FlowTomatorNotificationHandler callback = (FlowTomatorNotificationHandler)invocation;

                try
                {
                    callback(notification);
                }
                catch (Exception e)
                {
                    Log.Error("Unable to invoke a notification callback. " + e.Message);
                    Notification -= callback;
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            Parameters = args.Where(p => p.StartsWith("/"))
                             .Select(p => p.TrimStart('/'))
                             .Select(p => new { Parameter = p.Trim(), Separator = p.Trim().IndexOf(':') })
                             .ToDictionary(p => p.Separator == -1 ? p.Parameter.ToLower() : p.Parameter.Substring(0, p.Separator).ToLower(), p => p.Separator == -1 ? null : p.Parameter.Substring(p.Separator + 1));

            // Handle parameters
            if (Parameters.ContainsKey("log"))
                LogFile = new FileInfo(Parameters["log"]);
            else
                LogFile = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FlowTomator.Service.log"));

            // Redirect logging
            LogTextWriter textWriter = new LogTextWriter();
            textWriter.NewLine += TextWriter_Updated;
            Console.SetOut(textWriter);
            Console.SetError(textWriter);

            // Start service
            Log.Info(Environment.NewLine);
            Log.Info("Starting FlowTomator service");
            base.OnStart(args);

            // Share service via .NET remoting
            Log.Debug("Enabling remote monitoring");

            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = TypeFilterLevel.Full;

            IChannel channel = new IpcServerChannel("FlowTomator", "FlowTomator.Service", serverProvider);
            ChannelServices.RegisterChannel(channel);

            serviceRef = RemotingServices.Marshal(this, nameof(FlowTomatorService));

            Log.Info("Service started");
        }
        protected override void OnStop()
        {
            Log.Info("Stopping FlowTomator service");
            base.OnStop();
        }
        protected override void OnPause()
        {
            base.OnPause();
        }
        protected override void OnContinue()
        {
            base.OnContinue();
        }
        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);
        }
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }
    }
}