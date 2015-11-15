using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace FlowTomator.Common
{
    public enum ShutdownSimulationKind
    {
        Disabled,
        PassThrough,
        FakeEvent
    }

    [Node("ShutdownDevice", "System", "Shutdown the current device. The following nodes are not guaranteed to be executed, especially if you specify the force parameter")]
    public class ShutdownDevice : Task
    {
#if DEBUG
        internal static EventWaitHandle FakeShutdown
        {
            get
            {
                const string eventName = @"Global\FlowTomator.FakeShutdown";
                EventWaitHandle fakeShutdownEvent = null;

                if (!EventWaitHandle.TryOpenExisting(eventName, out fakeShutdownEvent))
                {
                    SecurityIdentifier users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    EventWaitHandleAccessRule rule = new EventWaitHandleAccessRule(users, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify, AccessControlType.Allow);
                    EventWaitHandleSecurity security = new EventWaitHandleSecurity();
                    security.AddAccessRule(rule);

                    bool created;
                    fakeShutdownEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName, out created, security);
                }

                return fakeShutdownEvent;
            }
        }
#endif

        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return force;

#if DEBUG
                yield return simulation;
#endif
            }
        }

        private Variable<bool> force = new Variable<bool>("Force", false, "Choose whether to force device shutdown. It will kill all running applications");
#if DEBUG
        private Variable<ShutdownSimulationKind> simulation = new Variable<ShutdownSimulationKind>("Simulation", ShutdownSimulationKind.FakeEvent, "The simulation mode used for debugging purpose");
#endif

        public override NodeResult Run()
        {
            if (simulation.Value == ShutdownSimulationKind.PassThrough)
                return NodeResult.Success;

            if (simulation.Value == ShutdownSimulationKind.FakeEvent)
            {
                ShutdownDevice.FakeShutdown.Set();
                return NodeResult.Success;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo("shutdown", (force.Value ? "/f " : "") + "/s /t 0");
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            Process.Start(processStartInfo);

            return NodeResult.Success;
        }
    }

    [Node("DeviceBooted", "System", "Detects if the current device just booted")]
    public class DeviceBootedEvent : BinaryChoice
    {
#if DEBUG
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return simulate;
            }
        }

        private Variable<bool> simulate = new Variable<bool>("Simulate", false, "Choose whether to simulate device boot for debugging purpose");
#endif

        public override NodeStep Evaluate()
        {
            if (simulate.Value)
                return new NodeStep(NodeResult.Success, TrueSlot);

            int tickCount = Environment.TickCount;
            return new NodeStep(NodeResult.Success, tickCount < 50000 ? TrueSlot : FalseSlot);
        }
    }

    [Node("DeviceShutdown", "System", "Triggers when the current device is shutting down")]
    public class DeviceShutdownEvent : Event
    {
#if DEBUG
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return simulation;
            }
        }

        private Variable<ShutdownSimulationKind> simulation = new Variable<ShutdownSimulationKind>("Simulate", ShutdownSimulationKind.FakeEvent, "The simulation mode user for debugging purpose");
#endif

        public override NodeResult Check()
        {
            if (simulation.Value == ShutdownSimulationKind.PassThrough)
                return NodeResult.Success;

            if (simulation.Value == ShutdownSimulationKind.FakeEvent)
            {
                ShutdownDevice.FakeShutdown.WaitOne();
                return NodeResult.Success;
            }

            throw new NotImplementedException();
        }
    }
}