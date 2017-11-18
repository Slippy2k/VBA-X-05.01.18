using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Utility;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class HIDSetupPage : Page
    {
        private HIDManager manager;
        private HIDInputChannel channel;
        private HIDButtonMap map;
        private HIDButtonMap mapAlt;
        private Dictionary<UIElement, Utility.Button> boxToButtonMappings;
        private Dictionary<UIElement, Utility.Button> boxToButtonMappingsAlt;
        private ResourceLoader resources;
        private IVBAXServiceProvider services;

        public HIDSetupPage()
        {
            this.InitializeComponent();

            this.manager = HIDManager.Current;
            this.resources = new ResourceLoader();
            this.boxToButtonMappings = new Dictionary<UIElement, Utility.Button>();
            this.boxToButtonMappingsAlt = new Dictionary<UIElement, Utility.Button>();

            this.services = new UWPServiceProvider(this.Dispatcher);

            this.boxToButtonMappings.Add(this.leftBindingBox, Utility.Button.Left);
            this.boxToButtonMappingsAlt.Add(this.leftBindingBox2, Utility.Button.Left);
            this.boxToButtonMappings.Add(this.upBindingBox, Utility.Button.Up);
            this.boxToButtonMappingsAlt.Add(this.upBindingBox2, Utility.Button.Up);
            this.boxToButtonMappings.Add(this.rightBindingBox, Utility.Button.Right);
            this.boxToButtonMappingsAlt.Add(this.rightBindingBox2, Utility.Button.Right);
            this.boxToButtonMappings.Add(this.downBindingBox, Utility.Button.Down);
            this.boxToButtonMappingsAlt.Add(this.downBindingBox2, Utility.Button.Down);
            this.boxToButtonMappings.Add(this.startBindingBox, Utility.Button.Start);
            this.boxToButtonMappingsAlt.Add(this.startBindingBox2, Utility.Button.Start);
            this.boxToButtonMappings.Add(this.selectBindingBox, Utility.Button.Select);
            this.boxToButtonMappingsAlt.Add(this.selectBindingBox2, Utility.Button.Select);
            this.boxToButtonMappings.Add(this.lBindingBox, Utility.Button.L);
            this.boxToButtonMappingsAlt.Add(this.lBindingBox2, Utility.Button.L);
            this.boxToButtonMappings.Add(this.rBindingBox, Utility.Button.R);
            this.boxToButtonMappingsAlt.Add(this.rBindingBox2, Utility.Button.R);
            this.boxToButtonMappings.Add(this.aBindingBox, Utility.Button.A);
            this.boxToButtonMappingsAlt.Add(this.aBindingBox2, Utility.Button.A);
            this.boxToButtonMappings.Add(this.bBindingBox, Utility.Button.B);
            this.boxToButtonMappingsAlt.Add(this.bBindingBox2, Utility.Button.B);
            this.boxToButtonMappings.Add(this.turboBindingBox, Utility.Button.Turbo);
            this.boxToButtonMappingsAlt.Add(this.turboBindingBox2, Utility.Button.Turbo);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await refreshList();
        }

        private async Task refreshList()
        {
            await this.manager.RefreshDeviceListAsync();
            this.hidDevicesBox.ItemsSource = this.manager.AvailableDevices;
            if (this.manager.CurrentChannel != null)
            {
                for (int i = 0; i < this.manager.AvailableDevices.Count; i++)
                {
                    if (this.manager.AvailableDevices[i].Id == this.manager.CurrentChannel.DeviceInfo.Id)
                    {
                        this.channel = this.manager.CurrentChannel;
                        this.hidDevicesBox.SelectedIndex = i;
                        this.editChannel();
                        break;
                    }
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.hidDevicesBox.ItemsSource = null;
            this.DisposeOldChannel();

            base.OnNavigatedFrom(e);
        }

        private void scrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.wrappingPanel.Width = this.scrollViewer.ActualWidth - this.wrappingPanel.Margin.Left - this.wrappingPanel.Margin.Right;
        }

        private async void hidDevicesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceInformation deviceInfo = this.hidDevicesBox.SelectedItem as DeviceInformation;
            if(this.channel != null && deviceInfo != null && this.channel.DeviceId == deviceInfo.Id)
            {
                return;
            }

            this.DisposeOldChannel();

            if (deviceInfo != null)
            {
                this.channel = await this.manager.ConnectAsync(deviceInfo);

                if (this.channel == null)
                {
                    this.map = null;
                    this.mapAlt = null;
                    this.channel = null;
                    this.RefreshBindings();

                    await this.services.MessageService.ShowMessage(resources.GetString("hidConnectError"), resources.GetString("errorCaption"));

                    return;
                }

                editChannel();

                EmulatorManager.Current.Emulator.GameController.SetHIDChannel(
                        HIDInputWrapper.FromChannel(this.channel)
                    );
            }
            else
            {
                this.map = null;
                this.mapAlt = null;
                this.channel = null;
                this.RefreshBindings();
            }
        }

        private void editChannel()
        {
            this.map = this.channel.Mapping;
            this.mapAlt = this.channel.MappingAlternative;
            this.channel.SetupMode = true;
            this.channel.ControlChanged += Channel_ControlChanged;
            this.RefreshBindings();
        }

        private async void Channel_ControlChanged(HIDMapping mapping)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Control changed");
#endif
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                this.ProcessInput(mapping);
            });
        }

        private void ProcessInput(HIDMapping mapping)
        {
            var focussedElement = FocusManager.GetFocusedElement() as UIElement;
            if (focussedElement != null)
            {
                this.processInputForMap(mapping, this.boxToButtonMappings, this.map, focussedElement);
                this.processInputForMap(mapping, this.boxToButtonMappingsAlt, this.mapAlt, focussedElement);
                this.RefreshBindings();
            }
        }

        private void processInputForMap(
            HIDMapping mapping, 
            Dictionary<UIElement, 
            Utility.Button> map, 
            HIDButtonMap buttonMap, 
            UIElement uiElement)
        {
            Utility.Button button;
            if (map.TryGetValue(uiElement, out button))
            {
                buttonMap[button] = mapping;
                FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
                FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
            }
        }

        private void RefreshBindings()
        {
            this.refreshBindingsForMap(this.boxToButtonMappings, this.map);
            this.refreshBindingsForMap(this.boxToButtonMappingsAlt, this.mapAlt);
        }

        private void refreshBindingsForMap(Dictionary<UIElement, Utility.Button> map, HIDButtonMap buttonMap)
        {
            if (buttonMap != null)
            {
                HIDMapping mapping = default(HIDMapping);

                foreach (var item in map)
                {
                    TextBox textBox = item.Key as TextBox;
                    if (textBox == null)
                    {
                        continue;
                    }

                    if (buttonMap.TryGetValue(item.Value, out mapping))
                    {
                        textBox.Text = mapping.DisplayName;
                    }
                    else
                    {
                        textBox.Text = string.Empty;
                    }
                }
            }
            else
            {
                foreach (var item in map)
                {
                    TextBox textBox = item.Key as TextBox;
                    if (textBox == null)
                    {
                        continue;
                    }
                    textBox.Text = string.Empty;
                }
            }
        }

        private void DisposeOldChannel()
        {
            if (this.channel != null)
            {
                this.channel.SetupMode = false;
                this.channel.ControlChanged -= Channel_ControlChanged;
                this.map = null;
                this.mapAlt = null;
            }
        }

        private async void refreshDevices_Click(object sender, RoutedEventArgs e)
        {
            await this.refreshList();
        }

        private void clearButton1_Click(object sender, RoutedEventArgs e)
        {
            this.clearMap(this.map);
        }

        private void clearButton2_Click(object sender, RoutedEventArgs e)
        {
            this.clearMap(this.mapAlt);
        }

        private void clearMap(HIDButtonMap map)
        {
            if (map != null)
            {
                map.Clear();
                this.RefreshBindings();
            }
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).MainPage.NavigateBack();
        }
    }
}
