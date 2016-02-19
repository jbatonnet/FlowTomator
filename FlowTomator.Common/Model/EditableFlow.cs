using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public abstract class EditableFlow : Flow
    {
        public override IEnumerable<Origin> Origins
        {
            get
            {
                return Nodes.OfType<Origin>();
            }
        }
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                return Variables;
            }
        }

        public virtual IList<Node> Nodes { get; } = new List<Node>();
        public virtual IList<Variable> Variables { get; } = new List<Variable>();

        public override IEnumerable<Node> GetAllNodes()
        {
            return Nodes;
        }

        public abstract void Save(string path);
        public static EditableFlow Load(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("The specified file path could not be found", path);

            string extension = fileInfo.Extension;
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                                                                  .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(EditableFlow)))
                                                                  .Where(t => t.GetCustomAttribute<FlowStorageAttribute>()?.Extensions?.Contains(extension) == true)
                                                                  .ToArray();

            if (types.Length == 0)
                throw new FormatException("Unable to find any loader for extension " + extension);
            if (types.Length > 1)
                throw new FormatException("Several loaders exist for extension " + extension);

            return Load(path, types[0]);
        }
        public static EditableFlow Load<T>(string path) where T : EditableFlow
        {
            return Load(path, typeof(T));
        }
        public static EditableFlow Load(string path, Type type)
        {
            if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(EditableFlow)))
                throw new TypeLoadException("The specified type " + type + " is not a valid flow loader");

            MethodInfo loaderMethod = type.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[0] );
            if (loaderMethod == null || (loaderMethod.ReturnType != typeof(EditableFlow) && !loaderMethod.ReturnType.IsSubclassOf(typeof(EditableFlow))))
                throw new TypeLoadException("Could not find a Load method in type " + type);

            return loaderMethod.Invoke(null, new object[] { path }) as EditableFlow;
        }
    }
}