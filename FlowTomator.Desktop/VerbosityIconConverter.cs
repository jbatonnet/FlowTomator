using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class VerbosityIconConverter : IValueConverter
    {
        private BitmapImage traceIcon = new BitmapImage(new Uri("/FlowTomator.Desktop;component/Resources/Trace.png", UriKind.RelativeOrAbsolute));
        private BitmapImage debugIcon = new BitmapImage(new Uri("/FlowTomator.Desktop;component/Resources/Debug.png", UriKind.RelativeOrAbsolute));
        private BitmapImage infoIcon = new BitmapImage(new Uri("/FlowTomator.Desktop;component/Resources/Info.png", UriKind.RelativeOrAbsolute));
        private BitmapImage warningIcon = new BitmapImage(new Uri("/FlowTomator.Desktop;component/Resources/Warning.png", UriKind.RelativeOrAbsolute));
        private BitmapImage errorIcon = new BitmapImage(new Uri("/FlowTomator.Desktop;component/Resources/Error.png", UriKind.RelativeOrAbsolute));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is LogVerbosity))
                throw new NotSupportedException();

            switch ((LogVerbosity)value)
            {
                case LogVerbosity.Trace: return traceIcon;
                case LogVerbosity.Debug: return debugIcon;
                case LogVerbosity.Info: return infoIcon;
                case LogVerbosity.Warning: return warningIcon;
                case LogVerbosity.Error: return errorIcon;
            }

            throw new NotSupportedException();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}