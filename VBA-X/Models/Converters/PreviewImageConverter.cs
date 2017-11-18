using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using EmulatorComponent;
using Utility;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources;

namespace VBA_X
{
    public class PreviewImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            BitmapImage image = null;
            try
            {
                StorageManager storage = StorageManager.Current;
                ResourceLoader loader = new ResourceLoader();

                image = storage.GetSnapshotImage(value.ToString());
                if (image == null)
                {
                    string defaultKey = value.ToString().Substring(1).ToUpper();
                    string themeSuffix = "";
                    if ((App.Current as App).MainPage != null)
                    {
                        switch ((App.Current as App).MainPage.RequestedTheme)
                        {
                            default:
                            case Windows.UI.Xaml.ElementTheme.Dark:
                            case Windows.UI.Xaml.ElementTheme.Default:
                                themeSuffix = "_Dark";
                                break;

                            case Windows.UI.Xaml.ElementTheme.Light:
                                themeSuffix = "_Light";
                                break;
                        }
                    }
                    image = new BitmapImage(
                        new Uri("ms-appx:///" + loader.GetString("libraryDefaultImage" + defaultKey.ToString() + themeSuffix))
                        );
                }
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("PreviewImageConverter: " + e.Message);
#endif
            }

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
