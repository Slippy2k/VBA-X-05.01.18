using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using EmulatorComponent;
using Windows.UI.Xaml;

namespace VBA_X
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool inverse = parameter != null && parameter.ToString().Equals("42");
            bool src = (bool)value;
            if(inverse)
            {
                src = !src;
            }
            return src ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Visibility src = (Visibility)value;
            return src == Visibility.Visible;
        }
    }
}
