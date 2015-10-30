using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class DataSet
    {
        public string Name { get; set; }
        public string[] Columns { get; set; }
        public List<object[]> Rows { get; set; } = new List<object[]>();

        public DataSet(string name, params string[] columns)
        {
            Name = name;
            Columns = columns;
        }
    }
}