using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using FlowTomator.Common;
using SmartSync.Common;

namespace FlowTomator.SmartSync
{
    [Node("Sync profile", "SmartSync", "Use the specified profile to perform a synchronization")]
    public class ProfileSync : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return profile;
            }
        }

        private Variable<FileInfo> profile = new Variable<FileInfo>("Profile", null, "The profile to sync");

        public override NodeResult Run()
        {
            if (profile.Value == null)
            {
                Log.Error("You must specify a profile to sync");
                return NodeResult.Fail;
            }
            if (!profile.Value.Exists)
            {
                Log.Error("The specified profile could not be found");
                return NodeResult.Fail;
            }

            try
            {
                Profile p = null;

                try
                {
                    switch (profile.Value.Extension)
                    {
                        case ".xsync": p = XProfile.Load(XDocument.Load(profile.Value.FullName)); break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error while loading the specified profile: " + e.Message);
                }

                // Compute differences and actions
                Diff[] differences = p.GetDifferences().ToArray();
                global::SmartSync.Common.Action[] actions = differences.Select(d => d.GetAction(p.SyncType)).ToArray();

                if (actions.Length > 0)
                {
                    // Process actions
                    for (int i = 0; i < actions.Length; i++)
                    {
                        Log.Info("{1} % - {0} ...", actions[i], i * 100 / actions.Length);
                        actions[i].Process();
                    }

                    Log.Info("Flushing data to storage...");
                }

                p.Dispose();

                Log.Info("Everything is in sync. {0} actions processed.", actions.Length);

                return NodeResult.Success;
            }
            catch (Exception e)
            {
                Log.Error("Error while trying to sync specified profile: " + e.Message);
                return NodeResult.Fail;
            }
        }
    }
}