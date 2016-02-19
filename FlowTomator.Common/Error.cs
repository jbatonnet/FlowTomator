using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common
{
    public static class Error
    {
        public static void Throw(Exception exception)
        {
            Log.Error(exception.Message);

            if (Debugger.IsAttached)
                throw exception;
        }

        public static void Show(string message, string title)
        {
            Show(null, null, title);
        }
        public static void Show(Exception exception, string title)
        {
            Show(exception, null, title);
        }
        public static void Show(Exception exception, string message, string title)
        {
            if (message == null)
                message = "";
            else if (!message.EndsWith("."))
                message += ".";

            if (exception != null)
                message += " " + exception.Message;

            Log.Error(message);

            if (Debugger.IsAttached)
                throw exception;

            if (Log.Verbosity <= LogVerbosity.Debug)
                message += Environment.NewLine + Environment.NewLine + exception.StackTrace;

            MessageBox.Show(message.Trim(), title);
        }
    }
}