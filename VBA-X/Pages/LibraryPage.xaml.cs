using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Utility;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
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
    public sealed partial class LibraryPage : Page
    {
        private bool refreshing = false;
        private PropertyChangedEventHandler romdirUpdateCallback;

        private bool contextMenuOpened = false;

        public int LibraryItemWidth
        {
            get; set;
        }

        StorageFolder ROMDirectory
        {
            get { return StorageManager.Current.ROMDirectory; }
        }

        public LibraryPage()
        {
            this.InitializeComponent();

            this.romdirUpdateCallback = new PropertyChangedEventHandler(this.updateROMDir);

            this.setROMList();

            this.SizeChanged += LibraryPage_SizeChanged;
        }

        private void LibraryPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.updateEntryWidth();
        }

        private void setROMList()
        {
            this.gridView.ItemsSource = StorageManager.Current.ROMList.OrderByDescending((entry) =>
            {
                if (entry.DatabaseEntry.LastPlayed == 1337)
                {
                    return long.MinValue;
                }
                return entry.DatabaseEntry.LastPlayed;
            });

            this.updateEntryWidth();
        }

        private void updateEntryWidth()
        {
            if(this.gridView == null || this.gridView.ActualWidth == 0)
            {
                return;
            }
            IOrderedEnumerable<ROMEntry> entries = null;
            if ((entries = this.gridView.ItemsSource as IOrderedEnumerable<ROMEntry>) == null)
            {
                return;
            }
            try
            {
                double itemContainerSpace = 0.0;
                foreach (var item in this.gridView.ItemContainerStyle.Setters.Cast<Setter>())
                {
                    if (item.Property == FrameworkElement.MarginProperty)
                    {
                        Thickness margin = (Thickness)item.Value;
                        itemContainerSpace += margin.Left + margin.Right;
                    }
                }

                double entryWidth = 0.0;
                double baseWidth = Math.Floor(Math.Max(0, this.gridView.ActualWidth - this.gridView.Padding.Right - this.gridView.Padding.Left));
                int numColumns = Math.Max(1, (int)(baseWidth / PlatformProperties.LibraryEntryTargetWidth));
                entryWidth = Math.Floor(baseWidth / numColumns) - itemContainerSpace - 1;

                foreach (var item in entries)
                {
                    item.EntryDisplayWidth = entryWidth;
                }
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("updateEntryWidth: " + e.Message);
#endif
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StorageManager.Current.PropertyChanged += this.romdirUpdateCallback;

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StorageManager.Current.PropertyChanged -= this.romdirUpdateCallback;

            base.OnNavigatedFrom(e);
        }

        private void updateROMDir(object sender, PropertyChangedEventArgs args)
        {
            if(args.PropertyName == "ROMList")
            {
                this.setROMList();
            }
        }

        private async void romDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            await StorageManager.Current.PickROMDirectoryAsync();
        }

        private async void gridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0)
            {
                var item = e.RemovedItems[0] as ROMEntry;

                if (item != null)
                {
                    await EmulatorManager.Current.StartROM(item);
                }
            }
        }

        private async void LibEntry_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.gridView.SelectedIndex >= 0)
            {
                ROMEntry rom = this.gridView.SelectedItem as ROMEntry;

                if (rom != null)
                {
                    await EmulatorManager.Current.StartROM(rom);
                }
            }
        }

        private async void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            if(!this.refreshing)
            {
                refreshing = true;
                await StorageManager.Current.RefreshROMListAsync();
                refreshing = false;
            }
        }

        private void entry_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
#if DEBUG
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                return;
            }
            if (this.contextMenuOpened)
            {
                return;
            }
            FrameworkElement target = sender as FrameworkElement;
            if (target == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Right tapped element is not a FrameworkElement.");
#endif
                return;
            }
            e.Handled = this.showMenuAt(target, e.GetPosition(target));

#endif
        }

        private void entry_Holding(object sender, HoldingRoutedEventArgs e)
        {
#if DEBUG
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                return;
            }
            if (this.contextMenuOpened)
            {
                return;
            }
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                FrameworkElement target = sender as FrameworkElement;
                if (target == null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Right tapped element is not a FrameworkElement.");
#endif
                    return;
                }
                e.Handled = this.showMenuAt(target, e.GetPosition(target));
            }
#endif
        }

        private bool showMenuAt(FrameworkElement target, Point position)
        {
            var flyout = this.Resources["EntryContextMenu"] as MenuFlyout;
            if (flyout == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Context menu not found.");
#endif
                return false;
            }
            flyout.ShowAt(target, position);
            return true;
        }

        private async void entryMenuPin_click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem);
            ROMEntry entry = item.DataContext as ROMEntry;
            if (entry == null)
            {
                return;
            }
            await TileManager.Current.PinTile(entry);
        }

        private void entryMenuRename_click(object sender, RoutedEventArgs e)
        {

        }

        private void entryMenuDelete_click(object sender, RoutedEventArgs e)
        {

        }

        private void contextMenu_Opened(object sender, object e)
        {
            this.contextMenuOpened = true;
        }

        private void contextMenu_Closed(object sender, object e)
        {
            this.contextMenuOpened = false;
        }
    }
}
