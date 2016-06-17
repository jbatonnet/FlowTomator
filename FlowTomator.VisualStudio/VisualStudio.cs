using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

using EnvDTE80;

namespace FlowTomator.VisualStudio
{
    public enum VisualStudioVersion
    {
        Unknown,

        VS2010,
        VS2012,
        VS2013,
        VS2015,
    }

    public class VisualStudio
    {
        [DllImport("user32.dll")]
        private extern static bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        public static IEnumerable<VisualStudio> Instances
        {
            get
            {
                return instances.AsReadOnly();
            }
        }
        private static List<VisualStudio> instances = new List<VisualStudio>();
        
        public static void RefreshInstances()
        {
            Dictionary<int, VisualStudio> oldInstances = instances.ToDictionary(i => i.Process.Id, i => i);
            instances.Clear();

            IRunningObjectTable rot;
            IEnumMoniker enumMoniker;

            int result = GetRunningObjectTable(0, out rot);
            if (result != 0)
                return;

            rot.EnumRunning(out enumMoniker);

            IntPtr fetched = IntPtr.Zero;
            IMoniker[] moniker = new IMoniker[1];

            while (enumMoniker.Next(1, moniker, fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);

                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);

                if (displayName.StartsWith("!VisualStudio.DTE.:"))
                {
                    string processIdString = displayName.Contains(":") ? displayName.Substring(displayName.IndexOf(":")) : displayName;

                    int processId = 0;
                    if (int.TryParse(processIdString, out processId))
                    {
                        VisualStudio instance;

                        if (oldInstances.TryGetValue(processId, out instance))
                        {
                            oldInstances.Remove(processId);
                            instances.Add(instance);
                        }
                        else
                        {
                            object dte;
                            rot.GetObject(moniker[0], out dte);

                            Process process = Process.GetProcessById(processId);
                            if (dte != null && process != null)
                                instances.Add(new VisualStudio(process, (DTE2)dte));
                        }
                    }
                }
            }
        }

        public static VisualStudio Start(FileInfo solution)
        {
            Process process = Process.Start(solution.FullName);
            process.WaitForInputIdle();

            RefreshInstances();

            return instances.FirstOrDefault(i => i.Process.Id == process.Id);
        }

        public Process Process { get; }
        public DTE2 DTE { get; }
        public VisualStudioVersion Version { get; }

        private VisualStudio(Process process, DTE2 dte)
        {
            Process = process;
            DTE = dte;
        }
    }
}