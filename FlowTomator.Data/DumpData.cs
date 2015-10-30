using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FlowTomator.Common;

namespace FlowTomator.Data
{
    public enum DataFormat
    {
        Csv,
        //Sql,
        //BGeo,
        //Bson,
    }

    [Node(nameof(DumpData), "Data", "Dumps the specified data into the specified file")]
    public class DumpData : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return data;
                yield return file;
                yield return format;
            }
        }

        private Variable<DataSet> data = new Variable<DataSet>("Data", null, "The data to dump");
        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file where the data will be dumped");
        private Variable<DataFormat> format = new Variable<DataFormat>("Format", DataFormat.Csv, "The format used to dump the data");

        public override NodeResult Run()
        {
            if (data.Value == null || file.Value == null)
                return NodeResult.Fail;

            try
            {
                if (!file.Value.Exists)
                    File.Create(file.Value.FullName).Close();

                switch (format.Value)
                {
                    case DataFormat.Csv: return DumpCsv();
                }
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Fail;
        }

        private NodeResult DumpCsv()
        {
            using (StreamWriter writer = new StreamWriter(file.Value.FullName))
            {
                writer.WriteLine(string.Join(";", data.Value.Columns));

                foreach (object[] rows in data.Value.Rows)
                    writer.WriteLine(string.Join(";", rows));
            }

            return NodeResult.Success;
        }
    }
}