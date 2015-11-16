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
    [Node("LockDevice", "System", "Lock the current device")]
    public class LockDevice : Task
    {
        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        public override NodeResult Run()
        {
            if (LockWorkStation())
                return NodeResult.Success;
            else
                return NodeResult.Fail;
        }
    }

    [Node("DeviceLock", "System", "Triggers when the system session is locked")]
    public class DeviceLockEvent : Event
    {
        private ManualResetEvent deviceLockEvent = new ManualResetEvent(false);

        public DeviceLockEvent()
        {
            DeviceEvents.DeviceLocked += DeviceEvents_DeviceLocked;
        }

        public override NodeResult Check()
        {
            DateTime start = DateTime.Now;

            while (!deviceLockEvent.WaitOne(500))
            {
                if (timeout.Value == TimeSpan.MaxValue)
                    continue;

                if (DateTime.Now > start + timeout.Value)
                    return NodeResult.Skip;
            }

            deviceLockEvent.Reset();

            return NodeResult.Success;
        }

        private void DeviceEvents_DeviceLocked()
        {
            deviceLockEvent.Set();
        }
    }

    [Node("DeviceUnlock", "System", "Triggers when the system session is unlocked")]
    public class DeviceUnlockEvent : Event
    {
        private ManualResetEvent deviceUnlockEvent = new ManualResetEvent(false);

        public DeviceUnlockEvent()
        {
            DeviceEvents.DeviceUnlocked += DeviceEvents_DeviceUnlocked;
        }

        public override NodeResult Check()
        {
            DateTime start = DateTime.Now;

            while (!deviceUnlockEvent.WaitOne(500))
            {
                if (timeout.Value == TimeSpan.MaxValue)
                    continue;

                if (DateTime.Now > start + timeout.Value)
                    return NodeResult.Skip;
            }

            deviceUnlockEvent.Reset();

            return NodeResult.Success;
        }

        private void DeviceEvents_DeviceUnlocked()
        {
            deviceUnlockEvent.Set();
        }
    }

    [Node("IsDeviceLocked", "System", "Detects wether the system session is locked or unlocked")]
    public class IsDeviceLocked : BinaryChoice
    {
        private bool sessionLocked = false;

        public IsDeviceLocked()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        public override NodeStep Evaluate()
        {
            return new NodeStep(NodeResult.Success, sessionLocked ? TrueSlot : FalseSlot);
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
                sessionLocked = true;
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
                sessionLocked = false;
        }
    }
}