using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using EmulatorComponent;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Resources;

namespace VBA_X
{
    public class BoolInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(!(value is bool))
            {
                return false;
            }
            bool src = (bool)value;

            return !src;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool))
            {
                return false;
            }
            bool src = (bool) value;

            return !src;
        }
    }
}
