using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class Variable
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public Type Type
        {
            get
            {
                return linked == null ? type : linked.Type;
            }
        }
        public object Value
        {
            get
            {
                return linked == null ? value : linked.Value;
            }
            set
            {
                if (linked == null)
                {
                    if (value != null)
                    {
                        Type valueType = value.GetType();
                        if (valueType != type && !valueType.IsSubclassOf(type))
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(type);
                            if (!converter.CanConvertFrom(valueType))
                                throw new Exception("Unable to set the specified value to this variable");

                            value = converter.ConvertFrom(value);
                        }
                    }

                    this.value = value;
                }
                else
                    linked.Value = value;
            }
        }
        public object DefaultValue
        {
            get
            {
                return defaultValue;
            }
        }
        public Variable Linked
        {
            get
            {
                return linked;
            }
        }

        private Type type = typeof(object);
        private object value = null;
        private object defaultValue = null;

        private Variable linked = null;

        public Variable(string name)
        {
            Name = name;
        }
        public Variable(string name, Type type)
        {
            Name = name;
            this.type = type;
        }
        public Variable(string name, Type type, object value)
        {
            Name = name;
            this.type = type;
            this.value = defaultValue = value;
        }
        public Variable(string name, Type type, object value, string description)
        {
            Name = name;
            this.type = type;
            this.value = defaultValue = value;
            Description = description;
        }

        public void Link(Variable other)
        {
            if (other != null && other.Type != type && !other.Type.IsSubclassOf(type))
                throw new Exception("Unable to link to the specified variable");

            linked = other;
        }

        public override string ToString()
        {
            object value = Value;

            if (value == null)
                return "null";
            else
                return value.ToString();
        }
    }

    public class Variable<T> : Variable
    {
        public new T Value
        {
            get
            {
                return (T)base.Value;
            }
            set
            {
                base.Value = value;
            }
        }

        public Variable(string name) : base(name, typeof(T), default(T)) { }
        public Variable(string name, T value) : base(name, typeof(T), value) { }
        public Variable(string name, T value, string description) : base(name, typeof(T), value, description) { }
    }
}