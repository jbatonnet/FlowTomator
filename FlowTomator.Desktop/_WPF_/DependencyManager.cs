using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FlowTomator.Desktop
{
    public class DependencyManager
    {
        public INotifyPropertyChanged Instance { get; protected set; }
        public PropertyChangedEventHandler PropertyChangedEventHandler { get; protected set; }

        private Dictionary<string, List<PropertyInfo>> propertyDependencies = new Dictionary<string, List<PropertyInfo>>();
        private Dictionary<string, INotifyPropertyChanged> propertyValues = new Dictionary<string, INotifyPropertyChanged>();

        protected DependencyManager() { }
        public DependencyManager(INotifyPropertyChanged instance, PropertyChangedEventHandler propertyChangedEventHandler)
        {
            Instance = instance;
            PropertyChangedEventHandler = propertyChangedEventHandler;

            Initialize();
        }

        protected void Initialize()
        {
            Instance.PropertyChanged += Instance_PropertyChanged;

            PropertyInfo[] propertyInfos = Instance.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                // Inspect each INotifyPropertyChanged
                if (propertyInfo.PropertyType.GetInterfaces().Contains(typeof(INotifyPropertyChanged)))
                {
                    INotifyPropertyChanged propertyValue = propertyInfo.GetValue(Instance) as INotifyPropertyChanged;

                    if (propertyValue != null)
                    {
                        propertyValues.Add(propertyInfo.Name, propertyValue);
                        propertyValue.PropertyChanged += PropertyValue_PropertyChanged;
                    }
                }

                // Inspect each DependsOn attribute
                DependsOnAttribute dependsOnAttribute = propertyInfo.GetCustomAttribute<DependsOnAttribute>();
                if (dependsOnAttribute != null)
                {
                    foreach (string property in dependsOnAttribute.Properties)
                    {
                        List<PropertyInfo> dependencies;

                        if (!propertyDependencies.TryGetValue(property, out dependencies))
                            propertyDependencies.Add(property, dependencies = new List<PropertyInfo>());
                        if (!dependencies.Contains(propertyInfo))
                            dependencies.Add(propertyInfo);
                    }
                }
            }
        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If the changed property is a INotifyPropertyChanged object, monitor its changes
            PropertyInfo propertyInfo = Instance.GetType().GetProperty(e.PropertyName);
            if (propertyInfo != null && propertyInfo.PropertyType.GetInterfaces().Contains(typeof(INotifyPropertyChanged)))
            {
                INotifyPropertyChanged oldValue;
                INotifyPropertyChanged newValue = propertyInfo.GetValue(Instance) as INotifyPropertyChanged;

                propertyValues.TryGetValue(e.PropertyName, out oldValue);

                if (oldValue != newValue)
                {
                    if (oldValue != null)
                        oldValue.PropertyChanged -= PropertyValue_PropertyChanged;
                    if (newValue != null)
                        newValue.PropertyChanged += PropertyValue_PropertyChanged;

                    propertyValues[e.PropertyName] = newValue;
                }
            }

            // Trigger dependencies changes
            if (PropertyChangedEventHandler != null)
            {
                List<PropertyInfo> dependencies;
                if (propertyDependencies.TryGetValue(e.PropertyName, out dependencies))
                {
                    foreach (PropertyInfo dependency in dependencies)
                    {
                        //Console.WriteLine("Manager: " + dependency.Name);
                        PropertyChangedEventHandler(Instance, new PropertyChangedEventArgs(dependency.Name));

                        // Special case for IPropertyUpdatable objects, trigger an update
                        if (dependency.PropertyType.GetInterfaces().Contains(typeof(IPropertyUpdatable)))
                        {
                            IPropertyUpdatable propertyUpdatableObject = dependency.GetValue(Instance) as IPropertyUpdatable;
                            if (propertyUpdatableObject != null)
                                propertyUpdatableObject.PropertyUpdate();
                        }
                    }
                }
            }
        }
        private void PropertyValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            INotifyPropertyChanged notifyPropertyObject = sender as INotifyPropertyChanged;
            if (notifyPropertyObject == null)
                return;

            // Find each property referencing this INotifyPropertyChanged object
            string[] properties = propertyValues.Where(p => p.Value == notifyPropertyObject)
                                                .Select(p => p.Key)
                                                .ToArray();

            // Trigger their change
            foreach (string property in properties)
            {
                //Console.WriteLine("Manager: " + property);
                PropertyChangedEventHandler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}