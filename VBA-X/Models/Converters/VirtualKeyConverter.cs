using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Data;

namespace VBA_X
{
    public class VirtualKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            VirtualKey src = (VirtualKey)value;
            string key = src.ToString();
            return key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return VirtualKey.None;
        }
    }
}
