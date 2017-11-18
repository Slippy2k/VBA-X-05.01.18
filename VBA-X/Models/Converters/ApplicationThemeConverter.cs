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
    public class ApplicationThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ApplicationTheme src = (ApplicationTheme)value;
            return (int)src;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            int src = (int)value;
            return (ApplicationTheme)src;
        }
    }
}
