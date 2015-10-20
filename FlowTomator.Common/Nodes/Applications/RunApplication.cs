using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common.Nodes
{
    [Node("RunApp", "Applications", "Run the specified application")]
    public class RunApplication : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return path;
                yield return arguments;
                yield return wait;
            }
        }

        private Variable<FileInfo> path = new Variable<FileInfo>("Path", null, "The path of the application to launch");
        private Variable<string> arguments = new Variable<string>("Args", null, "The arguments to pass to the application");
        private Variable<bool> wait = new Variable<bool>("Wait", false, "Choose wether to wait for the application to exit or not");

        public override NodeResult Run()
        {
            if (path.Value == null || !path.Value.Exists)
                return NodeResult.Fail;

            Process process = Process.Start(path.Value.FullName, arguments.Value);

            if (wait.Value)
                process.WaitForExit();

            return NodeResult.Success;
        }
    }

    [Node("KillApp", "Applications", "Kill the specified application")]
    public class KillApplication : Task
    {
        public override NodeResult Run()
        {
            return NodeResult.Fail;
        }
    }

    public enum ApplicationPatternType
    {
        FullPath,
        FileName,
        WindowTitle
    }

    [Node("IsAppRunning", "Applications", "Check if the specified application is running")]
    public class IsApplicationRunning : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return type;
                yield return pattern;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return process;
            }
        }

        private Variable<ApplicationPatternType> type = new Variable<ApplicationPatternType>("Type", ApplicationPatternType.FileName, "The application property to check");
        private Variable<string> pattern = new Variable<string>("Pattern", null, "The pattern to check");
        private Variable<Process> process = new Variable<Process>("Process", null, "The first process that matched the specified properties");

        public override NodeStep Evaluate()
        {
            if (string.IsNullOrWhiteSpace(pattern.Value))
                return new NodeStep(NodeResult.Fail, null);

            Func<Process, bool> filter = null;

            switch (type.Value)
            {
                case ApplicationPatternType.FullPath: filter = p => p.StartInfo.FileName.ToLower() == pattern.Value.ToLower(); break;
                case ApplicationPatternType.FileName: filter = p => p.ProcessName.ToLower().StartsWith(pattern.Value.ToLower()); break;
                case ApplicationPatternType.WindowTitle: filter = p => p.MainWindowTitle.ToLower() == pattern.Value.ToLower(); break;
            }

            if (filter == null)
                return new NodeStep(NodeResult.Fail, null);

            Process[] processes = Process.GetProcesses().Where(filter).ToArray();

            if (processes.Length > 0)
            {
                process.Value = processes[0];
                return new NodeStep(NodeResult.Success, TrueSlot);
            }
            else
                return new NodeStep(NodeResult.Success, FalseSlot);
        }
    }

    [Node("AppRan", "Applications", "Triggers when the described application starts")]
    public class ApplicationRunEvent : Event
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return path;
                yield return file;
            }
        }

        private Variable<string> path = new Variable<string>("Path");
        private Variable<string> file = new Variable<string>("File");

        public override NodeResult Check()
        {
            DateTime end;

            if (timeout.Value == TimeSpan.MaxValue)
                end = DateTime.MaxValue;
            else
                end = DateTime.Now + timeout.Value;

            while (true)
            {
                DateTime now = DateTime.Now;

                if (now > end)
                    return NodeResult.Skip;

                IEnumerable<Process> processes = Process.GetProcesses();

                if (path.Value != null)
                    processes = processes.Where(p => p.StartInfo.FileName.ToLower() == path.Value.ToLower());
                if (file.Value != null)
                    processes = processes.Where(p => p.ProcessName.ToLower() == file.Value.ToLower());

                if (processes.Any())
                    return NodeResult.Success;

                Thread.Sleep(1000);
            }
        }
    }
}