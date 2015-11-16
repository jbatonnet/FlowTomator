using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace FlowTomator.Common
{
    public static class DeviceEvents
    {
        public static event Action DeviceLocked;
        public static event Action DeviceUnlocked;

        static DeviceEvents()
        {
            if (Environment.UserInteractive)
            {
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            }
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock: OnDeviceLocked(); break;
                case SessionSwitchReason.SessionUnlock: OnDeviceUnlocked(); break;
            }
        }

        public static void OnDeviceLocked()
        {
            if (DeviceLocked != null)
                DeviceLocked();
        }
        public static void OnDeviceUnlocked()
        {
            if (DeviceUnlocked != null)
                DeviceUnlocked();
        }
    }
}