using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using EmulatorComponent;
using Windows.ApplicationModel.Resources;

namespace VBA_X
{
    public class LibraryDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            long src = (long)value;
            if(src == 1337)
            {
                ResourceLoader resources = new ResourceLoader();

                return resources.GetString("libraryNeverPlayedText");
            }
            return DateTime.FromBinary(src).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DateTime.Parse(value.ToString()).ToBinary();
        }
    }
}
