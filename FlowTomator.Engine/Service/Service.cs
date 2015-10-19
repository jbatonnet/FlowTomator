using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Engine
{
    public class FlowTomatorService : ServiceBase
    {
        public FlowTomatorService()
        {
            ServiceName = "FlowTomator";

            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            CanStop = true;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
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