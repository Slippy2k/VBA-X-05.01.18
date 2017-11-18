using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VBA_X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();

            this.versionLabel.Text = Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." + Package.Current.Id.Version.Build + "." + Package.Current.Id.Version.Revision;
#if DEBUG
            this.versionLabel.Text += " debug";
#endif
        }

        private void scrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //wrappingPanel.MaxWidth = scrollViewer.ActualWidth;
            this.wrappingPanel.Width = this.scrollViewer.ActualWidth - this.wrappingPanel.Margin.Left - this.wrappingPanel.Margin.Right;
        }

        private async void gnuLinkButton_Click(object sender, RoutedEventArgs e)
        {         
            var resources = new ResourceLoader();
            await Launcher.LaunchUriAsync(new Uri(resources.GetString("licenseUri")));
        }
    }
}
