using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FlowTomator.Sftp
{
    internal static class ModuleInitializer
    {
        private const string resourcePrefix = "FlowTomator.Sftp.Extern";
        private static string[] assemblyNames =
        {
            "Renci.SshNet.dll",
        };

        internal static void Run()
        {
            foreach (string assemblyName in assemblyNames)
            {
                using (Stream assemblyStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePrefix + "." + assemblyName))
                {
                    // Read assembly
                    byte[] assemblyBytes = new byte[assemblyStream.Length];
                    assemblyStream.Read(assemblyBytes, 0, assemblyBytes.Length);

                    // Load assembly
                    Assembly.Load(assemblyBytes);
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;

            // Check already loaded assemblies
            assembly = assembly ?? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);

            // Try to load full assembly name
            try
            {
                assembly = assembly ?? Assembly.Load(new AssemblyName(args.Name));
            }
            catch { }

            return assembly;
        }
    }
}