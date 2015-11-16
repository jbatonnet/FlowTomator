using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public static class Utils
    {
        public static EventWaitHandle GetOrCreateEvent(string name)
        {
            EventWaitHandle eventWaitHandle = null;

            if (!EventWaitHandle.TryOpenExisting(@"Global\" + name, out eventWaitHandle))
            {
                SecurityIdentifier users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                EventWaitHandleAccessRule rule = new EventWaitHandleAccessRule(users, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify, AccessControlType.Allow);
                EventWaitHandleSecurity security = new EventWaitHandleSecurity();
                security.AddAccessRule(rule);

                bool created;
                eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, @"Global\" + name, out created, security);
            }

            return eventWaitHandle;
        }
    }
}