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
        public FileInfo File { get; private set; }

        private AppDomain domain;
        private Flow flow;
        private FileSystemWatcher watcher;

        public FlowEnvironment(string path)
        {
            File = new FileInfo(path);
            if (!File.Exists)
                throw new FileNotFoundException("Unable to find the specified file path");

            // Create a new domain
            string domainName = string.Format("FlowEnvironment.0x{0:X8}", GetHashCode());
            domain = AppDomain.CreateDomain(domainName);

            // Preload common assemblies
            domain.DoCallBack(PreloadAssemblies);

            // Load the specified flow
            Reload();

            // Watch the specified path
            watcher = new FileSystemWatcher();
            watcher.Path = File.DirectoryName;
            watcher.Filter = File.Name;
            watcher.Changed += (s, e) => Reload();
            watcher.EnableRaisingEvents = true;
        }

        ~FlowEnvironment()
        {
            Dispose();
        }

        public static FlowEnvironment Load(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("The speficied flow path could not be found : " + path, path);

            Flow flow = null;

            switch (fileInfo.Extension)
            {
                case ".xflow":
                    XDocument document = XDocument.Load(fileInfo.FullName);
                    flow = XFlow.Load(document);
                    break;

                default:
                    throw new NotSupportedException("The specified flow format is not supported by FlowTomator service yet");
            }

            if (flow == null)
                throw new Exception("The specified flow could not be loaded");

            return new FlowEnvironment(path)
            {
                flow = flow
            };
        }

        private void Reload()
        {
            flow = null;

            switch (File.Extension)
            {
                case ".xflow":
                    XDocument document = XDocument.Load(File.FullName);
                    flow = XFlow.Load(document);
                    break;
            }

            if (flow == null)
                throw new Exception("The specified flow format is not supported");
        }

        public void Start()
        {
            flow.Reset();
            flow.Evaluate();
        }
        public void Stop()
        {
        }
        public void Restart(bool reload = true)
        {
            Stop();

            if (reload)
                Reload();

            Start();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            AppDomain.Unload(domain);
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