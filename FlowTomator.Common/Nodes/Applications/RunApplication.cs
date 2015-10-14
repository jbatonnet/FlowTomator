using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common.Nodes
{
    public class LaunchApplication : Task
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

        private Variable<string> path = new Variable<string>("Path", "", "The path of the application to launch");
        private Variable<string> arguments = new Variable<string>("Arguments", "", "The arguments to pass to the application");
        private Variable<bool> wait = new Variable<bool>("Wait", false, "Choose wether to wait for the application to exit or not");

        public override NodeResult Run()
        {
            Process process = Process.Start(path.Value, arguments.Value);

            if (wait.Value)
                process.WaitForExit();

            return NodeResult.Success;
        }
    }

    public class ApplicationRunning : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return path;
                yield return file;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return process;
            }
        }

        private Variable<string> path = new Variable<string>("Path");
        private Variable<string> file = new Variable<string>("File");

        private Variable<Process> process = new Variable<Process>("Process");

        public override NodeStep Evaluate()
        {
            IEnumerable<Process> processes = Process.GetProcesses();

            if (path.Value != null)
                processes = processes.Where(p => p.StartInfo.FileName.ToLower() == path.Value.ToLower());
            if (file.Value != null)
                processes = processes.Where(p => p.ProcessName.ToLower() == file.Value.ToLower());

            if (processes.Any())
                return new NodeStep(NodeResult.Success, TrueSlot);
            else
                return new NodeStep(NodeResult.Success, FalseSlot);
        }
    }

    public class ApplicationLaunch : Event
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

            if (Timeout == TimeSpan.MaxValue)
                end = DateTime.MaxValue;
            else
                end = DateTime.Now + Timeout;

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