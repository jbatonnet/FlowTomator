using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class ValueTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            VariableInfo variableInfo = item as VariableInfo;
            DataTemplate dataTemplate = new DataTemplate();

            if (variableInfo.Type.IsEnum)
                dataTemplate.VisualTree = new FrameworkElementFactory(typeof(EnumEditor));
            else if (variableInfo.Type == typeof(bool))
                dataTemplate.VisualTree = new FrameworkElementFactory(typeof(BoolEditor));
            else
                dataTemplate.VisualTree = new FrameworkElementFactory(typeof(TextEditor));

            return dataTemplate;
        }
    }
}