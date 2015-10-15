using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class DependencyModel : INotifyPropertyChanged
    {
        private Dictionary<string, List<PropertyInfo>> propertyDependencies = new Dictionary<string, List<PropertyInfo>>();
        private Dictionary<string, INotifyPropertyChanged> propertyValues = new Dictionary<string, INotifyPropertyChanged>();

        public event PropertyChangedEventHandler PropertyChanged;

        public DependencyModel()
        {
            PropertyInfo[] propertyInfos = GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                // Inspect each INotifyPropertyChanged
                if (propertyInfo.PropertyType.GetInterfaces().Contains(typeof(INotifyPropertyChanged)))
                {
                    INotifyPropertyChanged propertyValue = propertyInfo.GetValue(this) as INotifyPropertyChanged;

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

        protected void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property == null)
                return;

            //Console.WriteLine("Model: " + property);

            // If the changed property is a INotifyPropertyChanged object, monitor its changes
            PropertyInfo propertyInfo = GetType().GetProperty(property);
            if (propertyInfo != null && propertyInfo.PropertyType.GetInterfaces().Contains(typeof(INotifyPropertyChanged)))
            {
                INotifyPropertyChanged oldValue;
                INotifyPropertyChanged newValue = propertyInfo.GetValue(this) as INotifyPropertyChanged;

                propertyValues.TryGetValue(property, out oldValue);

                if (oldValue != newValue)
                {
                    if (oldValue != null)
                        oldValue.PropertyChanged -= PropertyValue_PropertyChanged;
                    if (newValue != null)
                        newValue.PropertyChanged += PropertyValue_PropertyChanged;

                    propertyValues[property] = newValue;
                }
            }

            // Trigger dependencies changes
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));

                List<PropertyInfo> dependencies;
                if (propertyDependencies.TryGetValue(property, out dependencies))
                {
                    foreach (PropertyInfo dependency in dependencies)
                    {
                        //Console.WriteLine("Model: " + dependency.Name);
                        NotifyPropertyChanged(dependency.Name);

                        // Special case for IPropertyUpdatable objects, trigger an update
                        if (dependency.PropertyType.GetInterfaces().Contains(typeof(IPropertyUpdatable)))
                        {
                            IPropertyUpdatable propertyUpdatableObject = dependency.GetValue(this) as IPropertyUpdatable;
                            if (propertyUpdatableObject != null)
                                propertyUpdatableObject.PropertyUpdate();
                        }
                    }
                }
            }
        }
        protected void NotifyPropertyChanged(params string[] properties)
        {
            foreach (string property in properties)
                NotifyPropertyChanged(property);
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
                //Console.WriteLine("Model: " + property);
                NotifyPropertyChanged(property);
            }
        }
    }
}