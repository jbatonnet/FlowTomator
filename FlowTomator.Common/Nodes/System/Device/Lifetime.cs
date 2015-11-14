using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace FlowTomator.Common
{
    [Node("ShutdownDevice", "System", "Shutdown the current device")]
    public class ShutdownDevice : Task
    {
        public override NodeResult Run()
        {
            throw new NotImplementedException();
        }
    }

    [Node("DeviceBoot", "System", "Triggers when the system has been booted")]
    public class DeviceBootEvent : Event
    {
        public override NodeResult Check()
        {
            int tickCount = Environment.TickCount;
            
            return NodeResult.Success;
        }
    }

    [Node("DeviceShutdown", "System", "Triggers when the system is shutting down")]
    public class DeviceShutdownEvent : Event
    {
        public override NodeResult Check()
        {
            throw new NotImplementedException();
        }
    }
}