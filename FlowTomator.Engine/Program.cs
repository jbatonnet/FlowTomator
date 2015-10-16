using System;
using System.Collections.Generic;
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
        static void Main(string[] args)
        {
            Dictionary<string, string> parameters = Environment.GetCommandLineArgs()
                                                               .Where(p => p.StartsWith("/"))
                                                               .Select(p => p.TrimStart('/'))
                                                               .Select(p => new { Parameter = p.Trim(), Separator = p.Trim().IndexOf(':') })
                                                               .ToDictionary(p => p.Separator == -1 ? p.Parameter : p.Parameter.Substring(0, p.Separator).ToLower(), p => p.Separator == -1 ? null : p.Parameter.Substring(p.Separator + 1));

            if (parameters.ContainsKey("log"))
            {
                LogVerbosity verbosity;

                if (!Enum.TryParse(parameters["log"], out verbosity))
                {
                    Log.Error("The specified log verbosity does not exist");
                    return;
                }

                Log.Verbosity = verbosity;
            }

            string flowPath = Environment.GetCommandLineArgs().Skip(1).LastOrDefault();
            if (flowPath == null)
            {
                Log.Error("You must specify a flow to evaluate");
                return;
            }

            FileInfo flowInfo = new FileInfo(flowPath);
            if (!flowInfo.Exists)
            {
                Log.Error("Could not find the specified flow");
                return;
            }
            
            Flow flow = null;

            try
            {
                switch (flowInfo.Extension)
                {
                    case ".xml": flow = XFlow.Load(XDocument.Load(flowInfo.FullName)); break;
                }
            }
            catch { }

            if (flow == null)
            {
                Log.Error("Could not load the specified flow");
                return;
            }

            Application.EnableVisualStyles();

            flow.Reset();
            NodeStep nodeStep = flow.Evaluate();

            Environment.Exit((int)nodeStep.Result);
        }
    }
}