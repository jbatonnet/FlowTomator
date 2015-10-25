using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class WebRequest : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return address;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return result;
            }
        }

        private Variable<string> address = new Variable<string>("Address", null, "Address to query");
        private Variable<string> result = new Variable<string>("Result");

        private WebClient webClient = new WebClient();

        public override NodeResult Run()
        {
            try
            {
                result.Value = webClient.DownloadString(address.Value);
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}