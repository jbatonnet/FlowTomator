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

public enum LogVerbosity
{
    Trace,
    Debug,
    Info,
    Warning,
    Error
}

public delegate void LogMessageHandler(LogVerbosity verbosity, Log.Category category, string message);

public static class Log
{
    public class Category
    {
        public static Category Common { get; } = new Category("Common");

        public string Name { get; private set; }

        public Category(string name)
        {
            Name = name;
        }
    }

    public static LogVerbosity Verbosity { get; set; } =
#if DEBUG
        LogVerbosity.Trace;
#else
        LogVerbosity.Info;
#endif

    public static event LogMessageHandler Message;

    private static object mutex = new object();

    public static void Trace(string format, params object[] args)
    {
        Write(LogVerbosity.Trace, Category.Common, string.Format(format, args), ConsoleColor.DarkGray);
    }
    public static void Debug(string format, params object[] args)
    {
        Write(LogVerbosity.Debug, Category.Common, string.Format(format, args), ConsoleColor.Gray);
    }
    public static void Info(string format, params object[] args)
    {
        Write(LogVerbosity.Info, Category.Common, string.Format(format, args), ConsoleColor.White);
    }
    public static void Warning(string format, params object[] args)
    {
        Write(LogVerbosity.Warning, Category.Common, string.Format(format, args), ConsoleColor.DarkYellow);
    }
    public static void Error(string format, params object[] args)
    {
        Write(LogVerbosity.Error, Category.Common, string.Format(format, args), ConsoleColor.DarkRed);
    }

    public static void Trace(Category category, string format, params object[] args)
    {
        Write(LogVerbosity.Trace, category, string.Format(format, args), ConsoleColor.DarkGray);
    }
    public static void Debug(Category category, string format, params object[] args)
    {
        Write(LogVerbosity.Debug, category, string.Format(format, args), ConsoleColor.Gray);
    }
    public static void Info(Category category, string format, params object[] args)
    {
        Write(LogVerbosity.Info, category, string.Format(format, args), ConsoleColor.White);
    }
    public static void Warning(Category category, string format, params object[] args)
    {
        Write(LogVerbosity.Warning, category, string.Format(format, args), ConsoleColor.DarkYellow);
    }
    public static void Error(Category category, string format, params object[] args)
    {
        Write(LogVerbosity.Error, category, string.Format(format, args), ConsoleColor.DarkRed);
    }

    private static void Write(LogVerbosity verbosity, Category category, string message, ConsoleColor color = ConsoleColor.Gray)
    {
        if (Verbosity <= verbosity)
        {
            lock (mutex)
            {
                ConsoleColor lastColor = Console.ForegroundColor;

                Console.ForegroundColor = color;
                Console.Write("[{0}] ", verbosity.ToString()[0]);
                Console.ForegroundColor = lastColor;

                Console.WriteLine(message);
            }
        }

        if (Message != null)
            Message(verbosity, category, message);
    }
}