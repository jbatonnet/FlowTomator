using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Service
{
    using System;
    using System.Runtime.InteropServices;

    [Flags]
    public enum ServiceManagerRights
    {
        Connect = 0x0001,
        CreateService = 0x0002,
        EnumerateService = 0x0004,
        Lock = 0x0008,
        QueryLockStatus = 0x0010,
        ModifyBootConfig = 0x0020,
        StandardRightsRequired = 0xF0000,
        AllAccess = (StandardRightsRequired | Connect | CreateService |
        EnumerateService | Lock | QueryLockStatus | ModifyBootConfig)
    }

    [Flags]
    public enum ServiceRights
    {
        QueryConfig = 0x1,
        ChangeConfig = 0x2,
        QueryStatus = 0x4,
        EnumerateDependants = 0x8,
        Start = 0x10,
        Stop = 0x20,
        PauseContinue = 0x40,
        Interrogate = 0x80,
        UserDefinedControl = 0x100,
        Delete = 0x00010000,
        StandardRightsRequired = 0xF0000,
        AllAccess = (StandardRightsRequired | QueryConfig | ChangeConfig |
        QueryStatus | EnumerateDependants | Start | Stop | PauseContinue |
        Interrogate | UserDefinedControl)
    }

    public enum ServiceBootFlag
    {
        Start = 0x00000000,
        SystemStart = 0x00000001,
        AutoStart = 0x00000002,
        DemandStart = 0x00000003,
        Disabled = 0x00000004
    }

    public enum ServiceState
    {
        Unknown = -1, // The state cannot be (has not been) retrieved.
        NotFound = 0, // The service is not known on the host server.
        Stop = 1, // The service is NET stopped.
        Run = 2, // The service is NET started.
        Stopping = 3,
        Starting = 4,
    }

    public enum ServiceControl
    {
        Stop = 0x00000001,
        Pause = 0x00000002,
        Continue = 0x00000003,
        Interrogate = 0x00000004,
        Shutdown = 0x00000005,
        ParamChange = 0x00000006,
        NetBindAdd = 0x00000007,
        NetBindRemove = 0x00000008,
        NetBindEnable = 0x00000009,
        NetBindDisable = 0x0000000A
    }

    public enum ServiceError
    {
        Ignore = 0x00000000,
        Normal = 0x00000001,
        Severe = 0x00000002,
        Critical = 0x00000003
    }

    public class ServiceManager
    {
        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;

        [StructLayout(LayoutKind.Sequential)]
        private class SERVICE_STATUS
        {
            public int dwServiceType = 0;
            public ServiceState dwCurrentState = 0;
            public int dwControlsAccepted = 0;
            public int dwWin32ExitCode = 0;
            public int dwServiceSpecificExitCode = 0;
            public int dwCheckPoint = 0;
            public int dwWaitHint = 0;
        }

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerA")]
        private static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, ServiceManagerRights dwDesiredAccess);
        [DllImport("advapi32.dll", EntryPoint = "OpenServiceA", CharSet = CharSet.Ansi)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceRights dwDesiredAccess);
        [DllImport("advapi32.dll", EntryPoint = "CreateServiceA")]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, ServiceRights dwDesiredAccess, int dwServiceType, ServiceBootFlag dwStartType, ServiceError dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lp, string lpPassword);
        [DllImport("advapi32.dll")]
        private static extern int CloseServiceHandle(IntPtr hSCObject);
        [DllImport("advapi32.dll")]
        private static extern int QueryServiceStatus(IntPtr hService, SERVICE_STATUS lpServiceStatus);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int DeleteService(IntPtr hService);
        [DllImport("advapi32.dll")]
        private static extern int ControlService(IntPtr hService, ServiceControl dwControl, SERVICE_STATUS lpServiceStatus);
        [DllImport("advapi32.dll", EntryPoint = "StartServiceA")]
        private static extern int StartService(IntPtr hService, int dwNumServiceArgs, int lpServiceArgVectors);

        public static bool ServiceIsInstalled(string serviceName)
        {
            IntPtr serviceManager = OpenSCManager(null, null, ServiceManagerRights.Connect);
            try
            {
                IntPtr service = OpenService(serviceManager, serviceName, ServiceRights.QueryStatus);
                if (service == IntPtr.Zero)
                    return false;

                CloseServiceHandle(service);
                return true;
            }
            finally
            {
                CloseServiceHandle(serviceManager);
            }
        }

        public static void InstallService(string serviceName, string displayName, string fileName)
        {
            IntPtr serviceManager = OpenSCManager(null, null, ServiceManagerRights.Connect | ServiceManagerRights.CreateService);

            try
            {
                IntPtr service = OpenService(serviceManager, serviceName, ServiceRights.QueryStatus | ServiceRights.Start);
                if (service != IntPtr.Zero)
                {
                    CloseServiceHandle(service);
                    throw new Exception("The specified service already exists");
                }

                service = CreateService(serviceManager, serviceName, displayName, ServiceRights.QueryStatus | ServiceRights.Start, SERVICE_WIN32_OWN_PROCESS, ServiceBootFlag.AutoStart, ServiceError.Normal, fileName, null, IntPtr.Zero, null, null, null);
                if (service == IntPtr.Zero)
                    throw new Exception("Failed to create the specified service");

                CloseServiceHandle(service);
            }
            finally
            {
                CloseServiceHandle(serviceManager);
            }
        }

        public static void UninstallService(string serviceName)
        {
            IntPtr serviceManager = OpenSCManager(null, null, ServiceManagerRights.Connect);
            try
            {
                IntPtr service = OpenService(serviceManager, serviceName, ServiceRights.StandardRightsRequired | ServiceRights.Stop | ServiceRights.QueryStatus);
                if (service == IntPtr.Zero)
                    throw new Exception("Service not installed.");

                try
                {
                    StopService(serviceName);

                    int result = DeleteService(service);
                    if (result == 0)
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Exception("Could not delete service: " + error);
                    }
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(serviceManager);
            }
        }

        public static void StartService(string serviceName)
        {
            IntPtr serviceManager = OpenSCManager(null, null, ServiceManagerRights.Connect);
            try
            {
                IntPtr service = OpenService(serviceManager, serviceName, ServiceRights.QueryStatus | ServiceRights.Start);
                if (service == IntPtr.Zero)
                    throw new Exception("Could not open specified service");

                try
                {
                    StartService(service, 0, 0);
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(serviceManager);
            }
        }

        public static void StopService(string serviceName)
        {
            IntPtr serviceManager = OpenSCManager(null, null, ServiceManagerRights.Connect);
            try
            {
                IntPtr service = OpenService(serviceManager, serviceName, ServiceRights.QueryStatus | ServiceRights.Stop);
                if (service == IntPtr.Zero)
                    throw new Exception("Could not open specified service");

                try
                {
                    SERVICE_STATUS serviceStatus = new SERVICE_STATUS();
                    ControlService(service, ServiceControl.Stop, serviceStatus);
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(serviceManager);
            }
        }
    }
}