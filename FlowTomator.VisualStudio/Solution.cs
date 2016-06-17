using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FlowTomator.Common;

namespace FlowTomator.VisualStudio
{
    [Node("Open solution", "Visual Studio", "Open the specified visual studio solution")]
    public class OpenSolution : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return solution;
            }
        }

        private Variable<FileInfo> solution = new Variable<FileInfo>("Solution", null, "Solution to open");

        public override NodeResult Run()
        {
            if (solution.Value == null)
            {
                Log.Error("Please specify a solution to open");
                return NodeResult.Fail;
            }

            VisualStudio.Start(solution.Value);
            return NodeResult.Success;
        }
    }

    public class CloseSolution : Task
    {
        private Variable<string> pattern = new Variable<string>("Pattern", "*", "Pattern to match solution name");

        public override NodeResult Run()
        {
            throw new NotImplementedException();
        }
    }
}