using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Service
{
    public class FlowTomatorService : ServiceBase
    {
        public ObservableCollection<FlowEnvironment> Flows { get; } = new ObservableCollection<FlowEnvironment>();

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
            FlowEnvironment flow = FlowEnvironment.Load(path);
            if (flow == null)
                return null;

            Flows.Add(flow);
            return flow;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            // Share service via .NET remoting
            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = TypeFilterLevel.Full;

            IChannel channel = new IpcServerChannel("FlowTomator", "FlowTomator.Service", serverProvider);
            ChannelServices.RegisterChannel(channel);

            serviceRef = RemotingServices.Marshal(this, nameof(FlowTomatorService));
        }
        protected override void OnStop()
        {
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