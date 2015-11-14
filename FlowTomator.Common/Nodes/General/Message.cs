using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common
{
    [Node("Message", "General", "Shows a message box with parameterable content")]
    public class Message : Node
    {
        [StructLayout(LayoutKind.Sequential)]
        struct WTS_SESSION_INFO
        {
            public int SessionId;
            public string pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSSendMessage(IntPtr hServer, int SessionId, string pTitle, int TitleLength, string pMessage, int MessageLength, MessageBoxButtons Style, int Timeout, out DialogResult pResponse, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern void WTSFreeMemory(IntPtr pMemory);

        public static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;
        public static int WTS_CURRENT_SESSION = -1;

        public static List<int> GetActiveSessions(IntPtr server)
        {
            List<int> ret = new List<int>();
            IntPtr ppSessionInfo = IntPtr.Zero;

            int count = 0;
            int retval = WTSEnumerateSessions(server, 0, 1, ref ppSessionInfo, ref count);
            int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

            long current = (int)ppSessionInfo;

            if (retval != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((IntPtr)current, typeof(WTS_SESSION_INFO));
                    current += dataSize;

                    if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                        ret.Add(si.SessionId);
                }

                WTSFreeMemory(ppSessionInfo);
            }

            return ret;
        }

        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return text;
                yield return title;
                yield return buttons;
                yield return icon;
            }
        }
        public override IEnumerable<Slot> Slots
        {
            get
            {
                switch (buttons.Value)
                {
                    case MessageBoxButtons.AbortRetryIgnore:
                        yield return abortSlot;
                        yield return retrySlot;
                        yield return ignoreSlot;
                        yield break;

                    case MessageBoxButtons.OK:
                        yield return okSlot;
                        yield break;

                    case MessageBoxButtons.OKCancel:
                        yield return okSlot;
                        yield return cancelSlot;
                        yield break;

                    case MessageBoxButtons.RetryCancel:
                        yield return retrySlot;
                        yield return cancelSlot;
                        yield break;

                    case MessageBoxButtons.YesNo:
                        yield return yesSlot;
                        yield return noSlot;
                        yield break;

                    case MessageBoxButtons.YesNoCancel:
                        yield return yesSlot;
                        yield return noSlot;
                        yield return cancelSlot;
                        yield break;
                }
            }
        }

        internal Variable<string> text = new Variable<string>("Text", "Text", "The text to display in this message box");
        internal Variable<string> title = new Variable<string>("Title", "Title", "The title of this message box");
        internal Variable<MessageBoxButtons> buttons = new Variable<MessageBoxButtons>("Buttons", MessageBoxButtons.OK, "The buttons to display in this message box");
        internal Variable<MessageBoxIcon> icon = new Variable<MessageBoxIcon>("Icon", MessageBoxIcon.None, "The icon to display in this message box");
        internal Variable<DialogResult> result = new Variable<DialogResult>("Result", DialogResult.OK, "The result of this message box");

        internal Slot okSlot = new Slot("OK");
        internal Slot yesSlot = new Slot("Yes");
        internal Slot noSlot = new Slot("No");
        internal Slot cancelSlot = new Slot("Cancel");
        internal Slot abortSlot = new Slot("Abort");
        internal Slot ignoreSlot = new Slot("Ignore");
        internal Slot retrySlot = new Slot("Retry");

        public override NodeStep Evaluate()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Log.Warning("Message node is not supported on this platform. The first slot will be chosen.");
                return new NodeStep(NodeResult.Success, Slots.FirstOrDefault(s => s.Nodes.Count > 0));
            }

            if (Environment.UserInteractive)
                result.Value = MessageBox.Show(text.Value, title.Value, buttons.Value, icon.Value);
            else
            {
                DialogResult result;

                int session = GetActiveSessions(WTS_CURRENT_SERVER_HANDLE).First();
                if (!WTSSendMessage(WTS_CURRENT_SERVER_HANDLE, session, title.Value, title.Value.Length, text.Value, text.Value.Length, buttons.Value, 0, out result, true))
                    return new NodeStep(NodeResult.Fail);

                this.result.Value = result;
            }

            switch (result.Value)
            {
                case DialogResult.Abort: return new NodeStep(NodeResult.Success, abortSlot);
                case DialogResult.Cancel: return new NodeStep(NodeResult.Success, cancelSlot);
                case DialogResult.Ignore: return new NodeStep(NodeResult.Success, ignoreSlot);
                case DialogResult.No: return new NodeStep(NodeResult.Success, noSlot);
                case DialogResult.OK: return new NodeStep(NodeResult.Success, okSlot);
                case DialogResult.Retry: return new NodeStep(NodeResult.Success, retrySlot);
                case DialogResult.Yes: return new NodeStep(NodeResult.Success, yesSlot);
            }

            return new NodeStep(NodeResult.Fail);
        }
    }
}