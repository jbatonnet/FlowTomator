using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class VariableEditorTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            VariableInfo variableInfo = item as VariableInfo;
            DataTemplate dataTemplate = new DataTemplate();

            if (variableInfo.Type.IsEnum)
                dataTemplate.VisualTree = new FrameworkElementFactory(typeof(EnumerableVariableEditor));
            else
            {
                Type variableEditorType = Assembly.GetExecutingAssembly().GetTypes()
                                                                         .FirstOrDefault(t => !t.IsAbstract && t.GetCustomAttribute<VariableEditorAttribute>()?.Types?.Contains(variableInfo.Type) == true);

                dataTemplate.VisualTree = new FrameworkElementFactory(variableEditorType ?? typeof(TextVariableEditor));
            }

            return dataTemplate;
        }
    }
}