using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlowTomator.Desktop
{
    public class VisualSnapshotConverter : IValueConverter
    {
        public double? MaxWidth { get; set; } = null;
        public double? MaxHeight { get; set; } = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UIElement element = value as UIElement;
            if (element == null)
                throw new NotSupportedException();
            if (targetType != typeof(ImageSource))
                throw new NotSupportedException();

            // Measure actual element size
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Size elementSize = element.DesiredSize;
            Rect elementRect = new Rect(elementSize);
            element.Arrange(elementRect);

            // Compute target bitmap size
            Size targetSize;

            if (MaxWidth.HasValue && MaxHeight.HasValue)
                targetSize = new Size(MaxWidth.Value, MaxHeight.Value);
            else if (MaxWidth.HasValue)
                targetSize = new Size(MaxWidth.Value, elementSize.Height * MaxWidth.Value / elementSize.Width);
            else if (MaxHeight.HasValue)
                targetSize = new Size(elementSize.Width * MaxHeight.Value / elementSize.Height, MaxHeight.Value);
            else
                targetSize = elementSize;

            // Create bitmap
            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)targetSize.Width, (int)targetSize.Height, 96, 96, PixelFormats.Default);
            renderTarget.Render(element);

            return renderTarget;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}