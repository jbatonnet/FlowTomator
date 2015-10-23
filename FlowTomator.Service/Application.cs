using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using FlowTomator.Service.Properties;

namespace FlowTomator.Service
{
    public class FlowTomatorApplication : ApplicationContext
    {
        public NotifyIcon Icon { get; private set; }
        public ContextMenu Menu { get; private set; }
        public FlowTomatorService Service { get; private set; }

        private IChannel channel;
        private MenuItem startServiceButton, stopServiceButton;

        public FlowTomatorApplication()
        {
            InitializeComponent();
            InitializeRemoting();

            // Start service if needed
            if (Program.Parameters.ContainsKey("start"))
                Task.Run(() => StartService()).ContinueWith(t => ConnectToService(false));
            else
                Task.Run(() => ConnectToService(false));
        }

        private void InitializeComponent()
        {
            Icon = new NotifyIcon();
            Menu = new ContextMenu();

            Icon.Icon = Resources.Icon;
            Icon.Text = "FlowTomator";
            Icon.ContextMenu = Menu;
            Icon.Visible = true;

            Menu.Popup += Menu_Popup;
            startServiceButton = new MenuItem("Start", StartServiceButton_Click);
            stopServiceButton = new MenuItem("Stop", StopServiceButton_Click);

            startServiceButton.Enabled = true;
            stopServiceButton.Enabled = false;
        }
        private void InitializeRemoting()
        {
            BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = TypeFilterLevel.Full;

            Hashtable properties = new Hashtable();
            properties["name"] = "FlowTomator";
            properties["portName"] = "FlowTomator.Monitor";

            channel = new IpcChannel(properties, clientProvider, serverProvider);
            ChannelServices.RegisterChannel(channel);
        }

        private void Menu_Popup(object sender, EventArgs e)
        {
            Menu.MenuItems.Clear();

            if (Service != null)
            {
                FlowEnvironment[] flows = Service.Flows.ToArray();
                if (flows.Length > 0)
                {
                    foreach (FlowEnvironment flow in flows)
                    {
                        string path = flow.Path;
                        string file = Path.GetFileName(path);

                        Menu.MenuItems.Add(new MenuItem(file, new[]
                        {
                        new MenuItem("Edit", (a, b) => Process.Start(path)),
                        new MenuItem("Stop")
                    }));
                    }

                    Menu.MenuItems.Add("-");
                }
            }

            Menu.MenuItems.AddRange(new[]
            {
                new MenuItem("Load flow ...", LoadFlowButton_Click),
                new MenuItem("-"),
                new MenuItem("Service", new[]
                {
                    startServiceButton,
                    stopServiceButton
                }),
                new MenuItem("Exit", ExitButton_Click)
            });
        }
        
        private void LoadFlowButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "XFlow files (*.xflow)|*.xflow|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            try
            {
                FlowEnvironment flow = Service.Load(openFileDialog.FileName);
                flow.Start();
            }
            catch (Exception ex)
            {

            }
        }
        private void StartServiceButton_Click(object sender, EventArgs e)
        {
            if (Service != null)
                return;

            Task.Run(() => StartService())
                .ContinueWith(t => ConnectToService());
        }
        private void StopServiceButton_Click(object sender, EventArgs e)
        {
            Task.Run(() => StopService())
                .ContinueWith(t => Service = null);
        }
        private void ExitButton_Click(object sender, EventArgs e)
        {
            if (Program.Parameters.ContainsKey("start"))
                StopService();

            ExitThread();
        }

        private void StartService()
        {
            ServiceController service = ServiceController.GetServices().SingleOrDefault(s => s.ServiceName == Program.ServiceName);
            if (service == null)
            {
                Icon.ShowBalloonTip(5000, "FlowTomator", "Could not start FlowTomator service. It may not be installed", ToolTipIcon.Error);
                return;
            }

            service.Start();
            
            startServiceButton.Enabled = false;
            stopServiceButton.Enabled = true;

            Thread.Sleep(2000);
        }
        private void StopService()
        {
            ServiceController service = ServiceController.GetServices().SingleOrDefault(s => s.ServiceName == Program.ServiceName);
            if (service == null)
            {
                Icon.ShowBalloonTip(5000, "FlowTomator", "Could not start FlowTomator service. It may not be installed", ToolTipIcon.Error);
                return;
            }

            service.Stop();

            startServiceButton.Enabled = true;
            stopServiceButton.Enabled = false;
        }
        private void ConnectToService(bool canThrow = true)
        {
            string uri = string.Format("ipc://{0}/{1}", "FlowTomator.Service", nameof(FlowTomatorService));

            try
            {
                Service = (FlowTomatorService)Activator.GetObject(typeof(FlowTomatorService), uri);
                Service.ToString();

                Icon.ShowBalloonTip(5000, "FlowTomator", "Connected to FlowTomator service", ToolTipIcon.Info);
            }
            catch (Exception e)
            {
                Service = null;

                if (canThrow && Debugger.IsAttached)
                    Debugger.Break();
                else
                    Icon.ShowBalloonTip(5000, "FlowTomator", "Could not connect to FlowTomator service. " + e.Message, ToolTipIcon.Error);
            }
        }
    }
}