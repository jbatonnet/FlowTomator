using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace FlowTomator.Common
{
    [Node("SessionLock", "System", "Triggers when the system session is locked")]
    public class SessionLockEvent : Event
    {
        private AutoResetEvent sessionLockEvent = new AutoResetEvent(false);

        public SessionLockEvent()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        public override NodeResult Check()
        {
            DateTime start = DateTime.Now;

            while (!sessionLockEvent.WaitOne(500))
            {
                if (timeout.Value == TimeSpan.MaxValue)
                    continue;

                if (DateTime.Now > start + timeout.Value)
                    return NodeResult.Skip;
            }

            return NodeResult.Success;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
                sessionLockEvent.Set();
        }
    }

    [Node("SessionUnlock", "System", "Triggers when the system session is unlocked")]
    public class SessionUnlockEvent : Event
    {
        private AutoResetEvent sessionUnlockEvent = new AutoResetEvent(false);

        public SessionUnlockEvent()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        public override NodeResult Check()
        {
            DateTime start = DateTime.Now;

            while (!sessionUnlockEvent.WaitOne(500))
            {
                if (timeout.Value == TimeSpan.MaxValue)
                    continue;

                if (DateTime.Now > start + timeout.Value)
                    return NodeResult.Skip;
            }

            return NodeResult.Success;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
                sessionUnlockEvent.Set();
        }
    }

    [Node("IsSessionLocked", "System", "Detects wether the system session is locked or unlocked")]
    public class IsSessionLocked : BinaryChoice
    {
        private bool sessionLocked = false;

        public IsSessionLocked()
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