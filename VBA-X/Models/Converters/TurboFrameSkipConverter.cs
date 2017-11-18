using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace VBA_X
{
    public class TurboFrameSkipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int src = (int)value;
            return src - 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            int src = (int)value;
            return src + 1;
        }
    }
}
