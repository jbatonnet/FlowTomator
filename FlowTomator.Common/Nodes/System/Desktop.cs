using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    // Solution from
    // http://maruf-dotnetdeveloper.blogspot.fr/2012/08/c-refreshing-system-tray-icon.html

    [Node("CleanTrayIcons", "System", "Clean the ghost icons in the tray bar")]
    public class CleanTrayIconsTask : Task
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        public override NodeResult Run()
        {
            IntPtr systemTrayContainerHandle = FindWindow("Shell_TrayWnd", null);
            IntPtr systemTrayHandle = FindWindowEx(systemTrayContainerHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            IntPtr sysPagerHandle = FindWindowEx(systemTrayHandle, IntPtr.Zero, "SysPager", null);
            IntPtr notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "Notification Area");

            if (notificationAreaHandle == IntPtr.Zero)
            {
                notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "User Promoted Notification Area");
                if (notificationAreaHandle == IntPtr.Zero)
                    notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "Zone de notification utilisateur promue");

                IntPtr notifyIconOverflowWindowHandle = FindWindow("NotifyIconOverflowWindow", null);
                IntPtr overflowNotificationAreaHandle = FindWindowEx(notifyIconOverflowWindowHandle, IntPtr.Zero, "ToolbarWindow32", "Overflow Notification Area");
                if (overflowNotificationAreaHandle == IntPtr.Zero)
                    overflowNotificationAreaHandle = FindWindowEx(notifyIconOverflowWindowHandle, IntPtr.Zero, "ToolbarWindow32", "Zone de notification de dépassement");

                RefreshTrayArea(overflowNotificationAreaHandle);
            }

            RefreshTrayArea(notificationAreaHandle);

            return NodeResult.Success;
        }

        private static void RefreshTrayArea(IntPtr windowHandle)
        {
            const uint WM_MOUSEMOVE = 0x0200;

            RECT rect;
            GetClientRect(windowHandle, out rect);

            for (var x = 0; x < rect.right; x += 12)
                for (var y = 0; y < rect.bottom; y += 12)
                    SendMessage(windowHandle, WM_MOUSEMOVE, 0, (y << 16) + x);
        }
    }
}