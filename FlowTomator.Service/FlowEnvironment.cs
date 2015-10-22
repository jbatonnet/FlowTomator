using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FlowTomator.Common;

namespace FlowTomator.Service
{
    public class FlowEnvironment : MarshalByRefObject, IDisposable
    {
        public AppDomain Domain { get; private set; }
        public Flow Flow { get; internal set; }

        public FlowEnvironment()
        {
            // Create a new domain
            string domainName = string.Format("FlowEnvironment.0x{0:X8}", GetHashCode());
            Domain = AppDomain.CreateDomain(domainName);

            // Preload common assemblies
            Domain.DoCallBack(PreloadAssemblies);
        }
        ~FlowEnvironment()
        {
            Dispose();
        }

        public static FlowEnvironment Load(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
                return null;

            Flow flow = null;

            try
            {
                switch (fileInfo.Extension)
                {
                    case ".xflow":
                        XDocument document = XDocument.Load(fileInfo.FullName);
                        flow = XFlow.Load(document);
                        break;
                }

            }
            catch
            {
                return null;
            }

            if (flow == null)
                return null;

            return new FlowEnvironment()
            {
                Flow = flow
            };
        }

        public void Start()
        {
            Flow.Reset();
            Flow.Evaluate();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            AppDomain.Unload(Domain);
        }

        private static void PreloadAssemblies()
        {
            string[] assemblyNames = new[]
            {
                "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",

                "FlowTomator.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
            };

            foreach (string assemblyName in assemblyNames)
                Assembly.Load(new AssemblyName(assemblyName));
        }
    }
}