using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace FlowTomator.Common
{
    enum LoginCredentialType
    {
        PlainText,
        WindowsVault
    }
    enum LogonType
    {
        Batch = 4,
        Interactive = 2,
        Network = 3,
        NetworkClearText = 8,
        NewCredentials = 9,
        Service = 5,
        Unlock = 7
    }
    enum LogonProvider
    {
        Default = 0,
        WINNT35 = 1,
        WINNT40 = 2,
        WINNT50 = 3
    }
    enum CredentialType : uint
    {
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4,
        GenericCertificate = 5,
        DomainExtended = 6,
        Maximum = 7,      // Maximum supported cred type
        MaximumEx = (Maximum + 1000),  // Allow new applications to run on old OSes
    }
    enum CredentialPersistance : uint
    {
        Session = 1,
        LocalMachine = 2,
        Enterprise = 3,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Credential
    {
        public uint Flags;
        public CredentialType Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CredentialPersistance Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CredentialAttribute
    {
        string Keyword;
        uint Flags;
        uint ValueSize;
        IntPtr Value;
    }

    [Node("LoginDevice", "System", "Log the specified user in the current device")]
    public class LoginDevice : Choice
    {
        private const int ERROR_NOT_FOUND = 1168;
        private const int ERROR_NO_SUCH_LOGON_SESSION = 1312;
        private const int ERROR_INVALID_FLAGS = 1004;
        private const int ERROR_LOGON_FAILURE = 1326;

        [DllImport("Advapi32.dll", EntryPoint = "LogonUserW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, out IntPtr phToken);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref Credential credential, int flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, CredentialType type, int reservedFlag, [MarshalAs(UnmanagedType.Struct)] out Credential credential);

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credential);

        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return user;
                yield return credentialType;

                if (credentialType.Value == LoginCredentialType.PlainText)
                    yield return pass;
                else if (credentialType.Value == LoginCredentialType.WindowsVault)
                {
                    yield return reset;

                    if (reset.Value)
                    {
                        if (string.IsNullOrWhiteSpace(pass.Value))
                            yield return pass;
                        else
                        {
                            // Retrieve username and domain
                            string username = user.Value;
                            string domain = ".";

                            int separator = username.IndexOf('\\');
                            if (separator >= 0)
                            {
                                domain = username.Substring(0, separator);
                                username = username.Substring(separator + 1);
                            }

                            // Store new password
                            Credential credential = new Credential();

                            credential.TargetName = domain + "\\" + Environment.MachineName;
                            credential.Type = CredentialType.Generic;
                            credential.UserName = domain + "\\" + username;
                            credential.AttributeCount = 0;
                            credential.Persist = CredentialPersistance.LocalMachine;

                            byte[] passwordBytes = Encoding.Unicode.GetBytes(pass.Value);
                            credential.CredentialBlobSize = passwordBytes.Length;
                            credential.CredentialBlob = Marshal.StringToCoTaskMemUni(pass.Value);

                            if (!CredWrite(ref credential, 0))
                                Log.Error("Error while saving credentials : " + new Win32Exception(Marshal.GetLastWin32Error()).Message);
                            else
                                Log.Info("Successfully updated {0} credentials", username);

                            reset.Value = false;
                            pass.Value = "";
                        }
                    }
                }
            }
        }
        public override IEnumerable<Slot> Slots
        {
            get
            {
                yield return successSlot;
                yield return failSlot;
            }
        }

        private Variable<string> user = new Variable<string>("User", WindowsIdentity.GetCurrent().Name, "The user to log in");
        private Variable<LoginCredentialType> credentialType = new Variable<LoginCredentialType>("Credential", LoginCredentialType.WindowsVault, "The credential type to be used to log the specified user");
        private Variable<string> pass = new Variable<string>("Password", "", "The password used to log the specified user");
        private Variable<bool> reset = new Variable<bool>("Reset", false, "Choose chether to reset credentials");

        private Slot successSlot = new Slot("Success");
        private Slot failSlot = new Slot("Wrong login");

        public override NodeStep Evaluate()
        {
            if (user.Value == null)
                return new NodeStep(NodeResult.Fail);

            // Retrieve username and domain
            string username = user.Value;
            string domain = ".";

            int separator = username.IndexOf('\\');
            if (separator >= 0)
            {
                domain = username.Substring(0, separator);
                username = username.Substring(separator + 1);
            }

            // Retrieve password
            string password = "";

            if (credentialType.Value == LoginCredentialType.PlainText)
                password = pass.Value;
            else if (credentialType.Value == LoginCredentialType.WindowsVault)
            {
                string target = domain + "\\" + Environment.MachineName;

                IntPtr credentialPtr;
                if (!CredRead(target, CredentialType.Generic, 0, out credentialPtr))
                {
                    Log.Error("Error loading credentials : " + new Win32Exception(Marshal.GetLastWin32Error()).Message);
                    return new NodeStep(NodeResult.Success, failSlot);
                }

                Credential credential = (Credential)Marshal.PtrToStructure(credentialPtr, typeof(Credential));
                if (credential.CredentialBlobSize > 0 && credential.CredentialBlob != IntPtr.Zero)
                    password = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            }

            // Logon user
            IntPtr token;
            if (!LogonUser(username, domain, password, LogonType.Interactive, LogonProvider.Default, out token))
            {
                int lastError = Marshal.GetLastWin32Error();

                if (lastError == ERROR_LOGON_FAILURE)
                    return new NodeStep(NodeResult.Success, failSlot);
                else
                {
                    Log.Error("Error while loging user in : {0}", new Win32Exception(lastError).Message);
                    return new NodeStep(NodeResult.Fail);
                }
            }

            return new NodeStep(NodeResult.Success, successSlot);
        }
    }
}