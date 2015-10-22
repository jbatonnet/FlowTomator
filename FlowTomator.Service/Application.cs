using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FlowTomator.Service.Properties;

namespace FlowTomator.Service
{
    public class FlowTomatorApplication : ApplicationContext
    {
        public NotifyIcon Icon { get; private set; }
        public ContextMenu Menu { get; private set; }

        private BackgroundWorker serviceStarter = new BackgroundWorker();

        public FlowTomatorApplication()
        {
            InitializeComponent();

            // Start service if needed
            if (Program.Parameters.ContainsKey("start"))
                Task.Run((Action)StartService);
        }

        private void InitializeComponent()
        {
            Menu = new ContextMenu();
            Icon = new NotifyIcon();

            Menu.Popup += Menu_Popup;
            //Menu.MenuItems.Add("Show", new EventHandler((o, e) => Icon_MouseClick(o, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0))));
            //Menu.MenuItems.Add("Extend", new EventHandler(Extend_Click));
            //Menu.MenuItems.Add("Exit", new EventHandler(Exit_Click));

            Icon.Icon = Resources.Icon;
            Icon.Text = "FlowTomator";
            Icon.ContextMenu = Menu;
            //Icon.MouseClick += Icon_MouseClick;
            Icon.Visible = true;
        }

        private void Menu_Popup(object sender, EventArgs e)
        {
            
        }

        private void StartService()
        {
            if (!ServiceManager.ServiceIsInstalled(Program.ServiceName))
            {
                MessageBox.Show("FlowTomator service is not installed yet. Please install it first", "FlowTomator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ServiceManager.StartService(Program.ServiceName);
        }
    }
}