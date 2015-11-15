using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using FlowTomator.Common;

namespace FlowTomator.Engine
{
    class Program
    {
        public static Dictionary<string, string> Options { get; private set; }
        public static List<string> Parameters { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Console.Title = "FlowTomator";

            Options = args.Where(a => a.StartsWith("/"))
                          .Select(a => a.TrimStart('/'))
                          .Select(a => new { Parameter = a.Trim(), Separator = a.Trim().IndexOf(':') })
                          .ToDictionary(a => a.Separator == -1 ? a.Parameter : a.Parameter.Substring(0, a.Separator).ToLower(), a => a.Separator == -1 ? null : a.Parameter.Substring(a.Separator + 1));
            Parameters = args.Where(a => !a.StartsWith("/"))
                             .ToList();

            if (Options.ContainsKey("log"))
            {
                LogVerbosity verbosity;

                if (!Enum.TryParse(Options["log"], out verbosity))
                {
                    Log.Error("The specified log verbosity does not exist");
                    Exit(-1);
                }

                Log.Verbosity = verbosity;
            }

            string flowPath = Parameters.FirstOrDefault();
            if (flowPath == null)
            {
                Log.Error("You must specify a flow to evaluate");
                Exit(-1);
            }

            FileInfo flowInfo = new FileInfo(flowPath);
            if (!flowInfo.Exists)
            {
                Log.Error("Could not find the specified flow");
                Exit(-1);
            }

            Console.Title = "FlowTomator - " + flowInfo.Name;

            Flow flow = null;

            try
            {
                switch (flowInfo.Extension)
                {
                    case ".xflow": flow = XFlow.Load(XDocument.Load(flowInfo.FullName)); break;
                }
            }
            catch { }

            if (flow == null)
            {
                Log.Error("Could not load the specified flow");
                return;
            }

            flow.Reset();
            NodeStep nodeStep = flow.Evaluate();

            Exit((int)nodeStep.Result);
        }

        public static void Exit(int code)
        {
            if (Debugger.IsAttached || Options.ContainsKey("pause"))
            {
                Console.WriteLine();
                Console.Write("Press any key to exit ...");
                Console.ReadKey(true);
            }

            Environment.Exit(code);
        }
    }
}