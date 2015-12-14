using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

using FlowTomator.Common;
using SmartSync.Common;

namespace FlowTomator.SmartSync
{
    public enum StorageType
    {
        Basic,
        Sftp,
        Zip
    }

    [Node("Sync manually", "SmartSync", "Use the specified parameters to perform a synchronization")]
    public class ManualSync : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return diffType;
                yield return syncType;
                yield return exclusions;

                yield return leftType;
                foreach (Variable leftVariable in GetStorageVariables(leftType.Value, "L."))
                    yield return leftVariable;

                yield return rightType;
                foreach (Variable rightVariable in GetStorageVariables(rightType.Value, "R."))
                    yield return rightVariable;
            }
        }

        private Variable<DiffType> diffType = new Variable<DiffType>("Diff", DiffType.Dates, "The method used to detect differences");
        private Variable<SyncType> syncType = new Variable<SyncType>("Sync", SyncType.Clone, "The method used to sync your files");
        private Variable<string[]> exclusions = new Variable<string[]>("Exclusions", new string[0], "List of paths excluded from the sync");
        private Variable<StorageType> leftType = new Variable<StorageType>("L", StorageType.Basic);
        private Variable<StorageType> rightType = new Variable<StorageType>("R", StorageType.Basic);

        private Dictionary<Type, Dictionary<string, Variable>> variables = new Dictionary<Type, Dictionary<string, Variable>>();

        public override NodeResult Run()
        {
            BasicProfile profile = new BasicProfile();

            profile.DiffType = diffType.Value;
            profile.SyncType = syncType.Value;

            profile.Exclusions.AddRange(exclusions.Value);

            profile.Left = BuildStorage(leftType.Value, "L.");
            profile.Right = BuildStorage(rightType.Value, "R.");

            try
            {
                // Compute differences and actions
                Diff[] differences = profile.GetDifferences().ToArray();
                global::SmartSync.Common.Action[] actions = differences.Select(d => d.GetAction(profile.SyncType)).ToArray();

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

                profile.Dispose();

                Log.Info("Everything is in sync. {0} actions processed.", actions.Length);

                return NodeResult.Success;
            }
            catch (Exception e)
            {
                Log.Error("Error while trying to sync specified profile: " + e.Message);
                return NodeResult.Fail;
            }
        }

        private IEnumerable<Variable> GetStorageVariables(StorageType storageType, string prefix = "")
        {
            Type type = null;

            switch (storageType)
            {
                case StorageType.Basic: type = typeof(BasicStorage); break;
                //case StorageType.Sftp: type = typeof(SftpStorage); break;
                case StorageType.Zip: type = typeof(ZipStorage); break;
            }

            if (type == null)
                yield break;

            Dictionary<string, Variable> variableCache;
            if (!variables.TryGetValue(type, out variableCache))
                variables.Add(type, variableCache = new Dictionary<string, Variable>());

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                EditorBrowsableState? state = property.GetCustomAttribute<EditorBrowsableAttribute>()?.State;
                if (state != null && state != EditorBrowsableState.Always)
                    continue;

                string name = prefix + property.Name;

                Variable variable;

                if (property.PropertyType == typeof(Storage))
                {
                    if (!variableCache.TryGetValue(name, out variable))
                        variableCache.Add(name, variable = new Variable<StorageType>(name, StorageType.Basic, name));
                    Variable<StorageType> storageVariable = variable as Variable<StorageType>;

                    yield return variable;

                    foreach (Variable subVariable in GetStorageVariables(storageVariable.Value, name + "."))
                        yield return subVariable;
                }
                else
                {
                    if (!variableCache.TryGetValue(name, out variable))
                        variableCache.Add(name, variable = new Variable(name, property.PropertyType, null, name));

                    yield return variable;
                }
            }
        }
        private Storage BuildStorage(StorageType storageType, string prefix = "")
        {
            Type type = null;

            switch (storageType)
            {
                case StorageType.Basic: type = typeof(BasicStorage); break;
                //case StorageType.Sftp: type = typeof(SftpStorage); break;
                case StorageType.Zip: type = typeof(ZipStorage); break;
            }

            if (type == null)
                return null;

            Dictionary<string, Variable> variableCache;
            if (!variables.TryGetValue(type, out variableCache))
                variables.Add(type, variableCache = new Dictionary<string, Variable>());

            Storage storage = Activator.CreateInstance(type) as Storage;

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                EditorBrowsableState? state = property.GetCustomAttribute<EditorBrowsableAttribute>()?.State;
                if (state != null && state != EditorBrowsableState.Always)
                    continue;

                string name = prefix + property.Name;
                Variable variable;

                if (!variableCache.TryGetValue(name, out variable))
                    continue;

                if (property.PropertyType == typeof(Storage))
                {
                    StorageType subType = (StorageType)variable.Value;
                    property.SetValue(storage, BuildStorage(subType, name + "."));
                }
                else
                    property.SetValue(storage, variable.Value);
            }

            return storage;
        }
    }
}