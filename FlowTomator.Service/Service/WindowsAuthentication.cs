using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Service
{
    public class WindowsAuthentication
    {
        [DllImport("dllmain.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SecureZeroMem(IntPtr ptr, uint cnt);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [DllImport("credui.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CredUnPackAuthenticationBuffer(int dwFlags,
                                                                  IntPtr pAuthBuffer,
                                                                  uint cbAuthBuffer,
                                                                  StringBuilder pszUserName,
                                                                  ref int pcchMaxUserName,
                                                                  StringBuilder pszDomainName,
                                                                  ref int pcchMaxDomainame,
                                                                  StringBuilder pszPassword,
                                                                  ref int pcchMaxPassword);

        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        private static extern int CredUIPromptForWindowsCredentials(ref CREDUI_INFO notUsedHere,
                                                                    int authError,
                                                                    ref uint authPackage,
                                                                    IntPtr InAuthBuffer,
                                                                    uint InAuthBufferSize,
                                                                    out IntPtr refOutAuthBuffer,
                                                                    out uint refOutAuthBufferSize,
                                                                    ref bool fSave,
                                                                    int flags);

        public static void GetCredentialsVistaAndUp(string serverName, out NetworkCredential networkCredential)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent(TokenAccessLevels.AllAccess);

            using (var context = identity.Impersonate())
            {



                CREDUI_INFO credui = new CREDUI_INFO();
                credui.pszCaptionText = "Please enter the credentails for " + serverName;
                credui.pszMessageText = "DisplayedMessage";
                credui.cbSize = Marshal.SizeOf(credui);

                uint authPackage = 0;
                IntPtr outCredBuffer = new IntPtr();
                uint outCredSize;
                bool save = false;

                var usernameBuf = new StringBuilder(100);
                var passwordBuf = new StringBuilder(100);
                var domainBuf = new StringBuilder(100);

                int maxUserName = 100;
                int maxDomain = 100;
                int maxPassword = 100;

                int result = CredUIPromptForWindowsCredentials(ref credui,
                                                               0,
                                                               ref authPackage,
                                                               IntPtr.Zero,
                                                               0,
                                                               out outCredBuffer,
                                                               out outCredSize,
                                                               ref save,
                                                               0x1000);   /* CREDUIWIN_SECURE_PROMPT */

                try
                {
                    if (result == 0)
                    {
                        try
                        {
                            if (CredUnPackAuthenticationBuffer(0,
                                                                 outCredBuffer,
                                                                 outCredSize,
                                                                 usernameBuf,
                                                                 ref maxUserName,
                                                                 domainBuf,
                                                                 ref maxDomain,
                                                                 passwordBuf,
                                                                 ref maxPassword))
                            {
                                networkCredential = new NetworkCredential(
                                                            userName: usernameBuf.ToString(),
                                                            password: passwordBuf.ToString(),
                                                            domain: domainBuf.ToString()
                                                        );
                            }
                            else
                            {
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            }
                        }
                        finally
                        {
                            passwordBuf.Clear();
                            networkCredential = null;
                        }
                    }
                    else
                        Debugger.Break();
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(outCredBuffer);

                }
                networkCredential = null;
            }
        }
    }
}
