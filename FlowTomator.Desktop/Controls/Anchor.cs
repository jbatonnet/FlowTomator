using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FlowTomator.Desktop
{
    public class Anchor : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty BinderProperty = DependencyProperty.Register(nameof(Binder), typeof(AnchorBinder), typeof(Anchor), new PropertyMetadata(BinderPropertyChanged));

        public event PropertyChangedEventHandler PropertyChanged;

        public AnchorBinder Binder
        {
            get
            {
                return null;
            }
            set
            {
                value.Anchor = this;
            }
        }

        public Anchor()
        {
            LayoutUpdated += Anchor_LayoutUpdated;
            Width = 1;
            Height = 1;
        }

        private void Anchor_LayoutUpdated(object sender, EventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(null, new PropertyChangedEventArgs(null));
        }

        private static void BinderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Anchor anchor = d as Anchor;
            anchor.Binder = e.NewValue as AnchorBinder;
        }
    }

    public class AnchorBinder : INotifyPropertyChanged
    {
        public Anchor Anchor
        {
            get
            {
                return anchor;
            }
            set
            {
                if (anchor != null)
                    anchor.PropertyChanged -= Anchor_PropertyChanged;

                anchor = value;

                if (anchor != null)
                    anchor.PropertyChanged += Anchor_PropertyChanged;

                Anchor_PropertyChanged(this, new PropertyChangedEventArgs(nameof(Anchor)));
            }
        }
        private Anchor anchor;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Update()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Anchor)));
        }

        private void Anchor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Anchor)));
        }
    }

    public class AnchorPointConverter : IMultiValueConverter
    {
        public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                throw new NotSupportedException();
            if (targetType != typeof(Point))
                throw new NotSupportedException();

            AnchorBinder anchorBinder = values[0] as AnchorBinder;
            Visual visual = values[1] as Visual;

            if (anchorBinder == null || anchorBinder.Anchor == null)
                return new Point(0, 0);
            if (visual == null)
                throw new NotSupportedException();

            // Check if the anchor is part of the specified visual
            DependencyObject commonAncestor = anchorBinder.Anchor.FindCommonVisualAncestor(visual);
            if (commonAncestor == null)
                return new Point(0, 0);

            //try
            {
                GeneralTransform transform = anchorBinder.Anchor.TransformToVisual(visual);
                Point translation = transform.Transform(new Point(0, 0));

                if (parameter is Point)
                {
                    Point offset = (Point)parameter;
                    translation.Offset(offset.X, offset.Y);
                }

                return translation;
            }
            //catch
            //{
            //    return new Point(0, 0);
            //}
        }
        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class AnchorXConverter : AnchorPointConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(double))
                throw new NotSupportedException();

            object point = base.Convert(values, typeof(Point), parameter, culture);
            return (point as Point?)?.X;
        }
    }
    public class AnchorYConverter : AnchorPointConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(double))
                throw new NotSupportedException();

            object point = base.Convert(values, typeof(Point), parameter, culture);
            return (point as Point?)?.Y;
        }
    }
}