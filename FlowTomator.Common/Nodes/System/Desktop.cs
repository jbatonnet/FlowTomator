using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class ObjectPinner : IDisposable
    {
        public IntPtr Pointer { get; private set; }

        private GCHandle handle;
        private bool disposed;

        public ObjectPinner(object obj)
        {
            handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            Pointer = handle.AddrOfPinnedObject();
        }

        ~ObjectPinner()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                handle.Free();
                Pointer = IntPtr.Zero;
            }
        }
    }
    public class NativeProcess : IDisposable
    {
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint procId);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr OpenProcess(uint access, bool inheritHandle, uint procID);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, int address, int size, uint allocationType, uint protection);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr address, int size, uint freeType);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr otherAddress, IntPtr localAddress, int size, ref uint bytesWritten);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr otherAddress, IntPtr localAddress, int size, ref uint bytesRead);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr otherAddress, StringBuilder localAddress, int size, ref uint bytesRead);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool GetProcessImageFileName(IntPtr hProcess, StringBuilder fileName, int fileNameSize);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetLastError();

        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x0004;

        private IntPtr hProcess;
        private uint ownerProcessID;
        private ArrayList allocations = new ArrayList();

        public NativeProcess(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, ref ownerProcessID);
            hProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION, false, ownerProcessID);
        }

        public void Dispose()
        {
            if (hProcess != IntPtr.Zero)
            {
                foreach (IntPtr ptr in allocations)
                {
                    VirtualFreeEx(hProcess, ptr, 0, MEM_RELEASE);
                }
                CloseHandle(hProcess);
            }
        }

        public string GetImageFileName()
        {
            StringBuilder sb = new StringBuilder(1024);

            bool result = GetProcessImageFileName(hProcess, sb, sb.Capacity - 1);
            if (!result)
                return null;

            return sb.ToString();
        }
        public IntPtr Allocate(object managedObject)
        {
            int size = Marshal.SizeOf(managedObject);

            IntPtr pointer = VirtualAllocEx(hProcess, 0, size, MEM_COMMIT, PAGE_READWRITE);
            if (pointer != IntPtr.Zero)
                allocations.Add(pointer);

            return pointer;
        }
        public void Read(object obj, IntPtr pointer)
        {
            using (ObjectPinner pin = new ObjectPinner(obj))
            {
                uint bytes = 0;
                int size = Marshal.SizeOf(obj);

                if (!ReadProcessMemory(hProcess, pointer, pin.Pointer, size, ref bytes))
                {
                    int error = GetLastError();
                    throw new ApplicationException("Read failed; error=" + error + "; bytes=" + bytes);
                }
            }
        }
        public string ReadString(int size, IntPtr pointer)
        {
            StringBuilder sb = new StringBuilder(size);

            uint bytes = 0;
            if (!ReadProcessMemory(hProcess, pointer, sb, size, ref bytes))
            {
                int error = GetLastError();
                throw new ApplicationException("Read failed; error=" + error + "; bytes=" + bytes);
            }

            return sb.ToString();
        }
        public void Write(object obj, int size, IntPtr pointer)
        {
            using (ObjectPinner pin = new ObjectPinner(obj))
            {
                uint bytes = 0;

                if (!WriteProcessMemory(hProcess, pointer, pin.Pointer, size, ref bytes))
                {
                    int error = GetLastError();
                    throw new ApplicationException("Write failed; error=" + error + "; bytes=" + bytes);
                }
            }
        }
    }
    
    [Node("CleanTrayIcons", "System", "Clean the ghost icons in the tray bar")]
    public class CleanTrayIconsTask : Task
    {
        #region Win32 API

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("user32.dll", EntryPoint = "SendMessageA", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SendMessage(IntPtr Hdc, uint Msg_Const, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "FindWindowA", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.dll", EntryPoint = "FindWindowExA", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public UIntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class ToolBarButton32
        {
            public uint iBitmap;        //  0
            public uint idCommand;      //  4
            public byte fsState;        //  8
            public byte fsStyle;        //  9
            private byte bReserved0;    // 10: 2 padding bytes added so IntPtr is at multiple of 4
            private byte bReserved1;    // 11
            public IntPtr dwData;       // 12: points to tray data
            public uint iString;        // 16
        }

        [StructLayout(LayoutKind.Sequential)]
        public class ToolBarButton64
        {
            public uint iBitmap;        //  0
            public uint idCommand;      //  4
            public byte fsState;        //  8
            public byte fsStyle;        //  9
            private byte bReserved0;    // 10: 6 padding bytes added so IntPtr is at multiple of 8
            private byte bReserved1;    // 11
            private byte bReserved2;    // 12
            private byte bReserved3;    // 13
            private byte bReserved4;    // 14
            private byte bReserved5;    // 15
            public IntPtr dwData;       // 16: points to tray data
            public uint iString;        // 24
        }

        [StructLayout(LayoutKind.Sequential)]
        public class TrayData
        {
            public IntPtr hWnd;             //  0
            public uint uID;                //  4 or  8
            public uint uCallbackMessage;   //  8 or 12
            private uint reserved0;         // 12 or 16
            private uint reserved1;         // 16 or 20
            public IntPtr hIcon;            // 20 or 24
        }

        private const uint TB_BUTTONCOUNT = 0x0418;  // WM_USER + 24
        private const uint TB_GETBUTTON = 0x0417;    // WM_USER + 23
        private const uint TB_DELETEBUTTON = 0x0416; // WM_USER + 22
        private const int PROCESSOR_ARCHITECTURE_AMD64 = 9; // x64 (AMD or Intel)

        #endregion

        private static object mutex = new object(); // concurrency protection

        public override NodeResult Run()
        {
            bool is64bitWindows = false;

            try
            {
                SYSTEM_INFO si;
                GetSystemInfo(out si);

                if (si.processorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
                    is64bitWindows = true;
            }
            catch { }
            
            ToolBarButton64 tbb64 = new ToolBarButton64();
            ToolBarButton32 tbb32 = new ToolBarButton32();
            TrayData td = new TrayData();

            // for safety reasons we perform two passes:
            // pass1 = search for my own NotifyIcon
            // pass2 = search phantom icons and remove them
            //         pass2 doesnt happen if pass1 fails
            lock (mutex)
            {
                for (int pass = 1; pass <= 2; pass++)
                {
                    for (int kind = 0; kind < 2; kind++)
                    {
                        IntPtr hWnd = IntPtr.Zero;

                        if (kind == 0)
                        {
                            // get the regular icon collection that exists on all Windows versions
                            FindNestedWindow(ref hWnd, "Shell_TrayWnd");
                            FindNestedWindow(ref hWnd, "TrayNotifyWnd");
                            FindNestedWindow(ref hWnd, "SysPager");
                            FindNestedWindow(ref hWnd, "ToolbarWindow32");
                        }
                        else
                        {
                            // get the hidden icon collection that exists since Windows 7
                            try
                            {
                                FindNestedWindow(ref hWnd, "NotifyIconOverflowWindow");
                                FindNestedWindow(ref hWnd, "ToolbarWindow32");
                            }
                            catch
                            {
                                // fail silently, as NotifyIconOverflowWindow did not exist prior to Win7
                                break;
                            }
                        }

                        // create an object so we can exchange data with other process
                        using (NativeProcess process = new NativeProcess(hWnd))
                        {
                            IntPtr remoteButtonPtr;

                            if (is64bitWindows)
                                remoteButtonPtr = process.Allocate(tbb64);
                            else
                                remoteButtonPtr = process.Allocate(tbb32);

                            process.Allocate(td);
                            uint itemCount = (uint)SendMessage(hWnd, TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);

                            uint removedCount = 0;
                            for (uint item = 0; item < itemCount; item++)
                            {
                                // index changes when previous items got removed !
                                uint item2 = item - removedCount;
                                uint SOK = (uint)SendMessage(hWnd, TB_GETBUTTON, new IntPtr(item2), remoteButtonPtr);
                                if (SOK != 1) throw new ApplicationException("TB_GETBUTTON failed");
                                if (is64bitWindows)
                                {
                                    process.Read(tbb64, remoteButtonPtr);
                                    process.Read(td, tbb64.dwData);
                                }
                                else
                                {
                                    process.Read(tbb32, remoteButtonPtr);
                                    process.Read(td, tbb32.dwData);
                                }
                                IntPtr hWnd2 = td.hWnd;
                                if (hWnd2 == IntPtr.Zero) throw new ApplicationException("Invalid window handle");
                                using (NativeProcess proc = new NativeProcess(hWnd2))
                                {
                                    string filename = proc.GetImageFileName();
                                    if (pass == 1 && filename != null)
                                    {
                                        filename = filename.ToLower();
                                        if (filename.EndsWith(".exe"))
                                        {
                                            break;
                                        }
                                    }
                                    // a phantom icon has no imagefilename
                                    if (pass == 2 && filename == null)
                                    {
                                        SOK = (uint)SendMessage(hWnd, TB_DELETEBUTTON,
                                            new IntPtr(item2), IntPtr.Zero);
                                        if (SOK != 1) throw new ApplicationException("TB_DELETEBUTTON failed");
                                        removedCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return NodeResult.Success;
        }

        private static void FindNestedWindow(ref IntPtr hWnd, string name)
        {
            if (hWnd == IntPtr.Zero)
            {
                hWnd = FindWindow(name, null);
            }
            else {
                hWnd = FindWindowEx(hWnd, IntPtr.Zero, name, null);
            }
            if (hWnd == IntPtr.Zero) throw new ApplicationException("Failed to locate window " + name);
        }
    }
}