using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using EmulatorComponent;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VBA_X.Pages
{

    public class AccentColorEntry
    {
        public ApplicationColor ColorName
        {
            get; set;
        }
        public SolidColorBrush Brush
        {
            get; set;
        }
        public Color Color
        {
            get
            {
                return this.Brush.Color;
            }
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private App app;
        private ObservableCollection<AccentColorEntry> accentColors;
        private UWPMessageService messages;
        private ResourceLoader resources;

        public ObservableCollection<AccentColorEntry> AccentColors
        {
            get { return this.accentColors; }
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            this.InitializeSettings();
            this.AdjustPlatformSettings();
            this.RegisterEvents();
        }

        private void InitializeSettings()
        {
            app = App.Current as App;
            this.resources = new ResourceLoader();
            this.messages = new UWPMessageService(this.Dispatcher);

            // fill accent color ComboBox
            this.accentColors = new ObservableCollection<AccentColorEntry>();
            foreach (var accent in Enum.GetValues(typeof(ApplicationColor)).Cast<ApplicationColor>())
            {
                this.accentColors.Add(
                        new AccentColorEntry()
                        {
                            ColorName = accent,
                            Brush = App.Current.Resources[accent.ToString() + "Brush"] as SolidColorBrush
                        }
                    );
            }
            this.accentComboBox.ItemsSource = this.accentColors;

            this.DataContext = app.Settings;
            this.hq2xItem.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.hq3xItem.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.hq4xItem.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR2Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR3Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR4Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR5Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = app.Settings;
            this.hq2xItem.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.hq3xItem.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.hq4xItem.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR2Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR3Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR4Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
            this.xBR5Item.DataContext = EmulatorManager.Current.Emulator.RenderComponent;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.DataContext = null;
            this.hq2xItem.DataContext = null;
            this.hq3xItem.DataContext = null;
            this.hq4xItem.DataContext = null;
            this.xBR2Item.DataContext = null;
            this.xBR3Item.DataContext = null;
            this.xBR4Item.DataContext = null;
            this.xBR5Item.DataContext = null;
        }

        private void AdjustPlatformSettings()
        {
            this.bindingGrid.Visibility = PlatformProperties.HasKeyBindings ? Visibility.Visible : Visibility.Collapsed;
            this.fullscreenTriggerLabel.Visibility = PlatformProperties.AlwaysFullscreen ? Visibility.Collapsed : Visibility.Visible;
            this.fullscreenCheckbox.Visibility = PlatformProperties.AlwaysFullscreen ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RegisterEvents()
        {
        }

        private void scrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //this.wrappingPanel.MaxWidth = this.scrollViewer.ActualWidth;
            this.wrappingPanel.Width = this.scrollViewer.ActualWidth - this.wrappingPanel.Margin.Left - this.wrappingPanel.Margin.Right;
        }

        private void fpsTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.fpsTriggerCheckbox.IsChecked = !this.fpsTriggerCheckbox.IsChecked;
        }

        private void soundTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.soundTriggerCheckbox.IsChecked = !this.soundTriggerCheckbox.IsChecked;
        }

        private void soundQualityTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.soundQualityTriggerCheckbox.IsChecked = !this.soundQualityTriggerCheckbox.IsChecked;
        }

        private void fullscreenTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.fullscreenCheckbox.IsChecked = !this.fullscreenCheckbox.IsChecked;
        }

        private void saveConfirmTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.saveConfirmCheckbox.IsChecked = !this.saveConfirmCheckbox.IsChecked;
        }

        private void loadConfirmTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.loadConfirmCheckbox.IsChecked = !this.loadConfirmCheckbox.IsChecked;
        }

        private void resetConfirmTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.resetConfirmCheckbox.IsChecked = !this.resetConfirmCheckbox.IsChecked;
        }

        private void settingsManualSnapshotLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.manualSnapshotCheckbox.IsChecked = !this.manualSnapshotCheckbox.IsChecked;
        }

        private void virtualPadTriggerLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.virtualPadTrigger.IsChecked = !this.virtualPadTrigger.IsChecked;
        }

        private void leftBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void leftBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.LeftBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void rightBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void rightBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.RightBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void upBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void upBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.UpBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void downBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void downBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.DownBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void aBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void aBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.ABinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void bBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void bBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.BBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void lBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void lBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.LBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void rBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void rBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.RBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void startBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void startBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.StartBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void selectBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void selectBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.SelectBinding = e.Key;

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void turboBindingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void turboBindingBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var app = App.Current as App;
            app.Settings.TurboBinding = e.Key;
        }

        private void configHidButton_Click(object sender, RoutedEventArgs e)
        {
            this.app.MainPage.NavigateTo(typeof(HIDSetupPage));
        }

        private void filterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
