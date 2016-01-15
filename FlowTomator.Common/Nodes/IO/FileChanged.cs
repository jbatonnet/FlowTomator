using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common
{
    [Node("FileChanged", "IO", "Resume the flow if a file is changed in the specified directory")]
    public class FileChanged : Event
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return directory;
                yield return filter;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield break;
            }
        }

        private Variable<DirectoryInfo> directory = new Variable<DirectoryInfo>("Directory", null, "The directory to watch for changes");
        private Variable<string> filter = new Variable<string>("Filter", "*", "The filter used to monitor changes");

        private FileSystemWatcher watcher;
        private AutoResetEvent resetEvent = new AutoResetEvent(false);
        private Mutex mutex = new Mutex();

        public FileChanged()
        {
            watcher = new FileSystemWatcher();
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            new Thread(() =>
            {
                if (!mutex.WaitOne(0))
                    return;

                Thread.Sleep(250);

                resetEvent.Set();
                mutex.ReleaseMutex();
            }).Start();
        }

        public override NodeResult Check()
        {
            DateTime start = DateTime.Now;

            watcher.Path = directory.Value.FullName;
            watcher.Filter = filter.Value;
            watcher.EnableRaisingEvents = true;

            while (!resetEvent.WaitOne(500))
            {
                if (timeout.Value == TimeSpan.MaxValue)
                    continue;

                if (DateTime.Now > start + timeout.Value)
                    return NodeResult.Skip;
            }

            watcher.EnableRaisingEvents = false;
            return NodeResult.Success;
        }
    }
}