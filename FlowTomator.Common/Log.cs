using System;
using System.Collections.Generic;
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
    public enum LogVerbosity
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }

    public static class Log
    {
        public static LogVerbosity Verbosity { get; set; } = LogVerbosity.Info;

        private static object mutex = new object();

        public static void Trace(string format, params object[] args)
        {
            if (Verbosity <= LogVerbosity.Trace)
            {
                lock (mutex)
                {
                    WritePrefix(ConsoleColor.DarkGray, "[T] ");
                    Console.WriteLine(format, args);
                }
            }
        }
        public static void Debug(string format, params object[] args)
        {
            if (Verbosity <= LogVerbosity.Debug)
            {
                lock (mutex)
                {
                    WritePrefix(ConsoleColor.Gray, "[D] ");
                    Console.WriteLine(format, args);
                }
            }
        }
        public static void Info(string format, params object[] args)
        {
            if (Verbosity <= LogVerbosity.Info)
            {
                lock (mutex)
                {
                    WritePrefix(ConsoleColor.White, "[I] ");
                    Console.WriteLine(format, args);
                }
            }
        }
        public static void Warning(string format, params object[] args)
        {
            if (Verbosity <= LogVerbosity.Warning)
            {
                lock (mutex)
                {
                    WritePrefix(ConsoleColor.DarkYellow, "[W] ");
                    Console.WriteLine(format, args);
                }
            }
        }
        public static void Error(string format, params object[] args)
        {
            if (Verbosity <= LogVerbosity.Error)
            {
                lock (mutex)
                {
                    WritePrefix(ConsoleColor.DarkRed, "[E] ");
                    Console.WriteLine(format, args);
                }
            }
        }

        private static void WritePrefix(ConsoleColor color, string prefix)
        {
            ConsoleColor oldColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.Write(prefix);
            Console.ForegroundColor = oldColor;
        }
    }
}