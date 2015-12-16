using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class MessageSourceFilterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                throw new NotSupportedException();

            ObservableCollection<LogMessage> messages = values[0] as ObservableCollection<LogMessage>;
            LogVerbosity? verbosity = values[1] as LogVerbosity?;
            if (messages == null || verbosity == null)
                return null;
            
            CollectionViewSource collectionViewSource = new CollectionViewSource() { Source = messages as ObservableCollection<LogMessage> };

            ICollectionView collectionView = collectionViewSource.View;
            collectionView.Filter = i => ((LogMessage)i).Verbosity >= verbosity.Value;

            return collectionView;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}