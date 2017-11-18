using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Storage;

namespace Utility
{
    public sealed class HIDManager : INotifyPropertyChanged
    {
        #region singleton

        private static HIDManager instance;

        public static HIDManager Current
        {
            get
            {
                if(instance == null)
                {
                    instance = new HIDManager();
                }
                return instance;
            }
        }

        #endregion

        private const string HID_DEVICE_ID = "LastActiveHID";
        internal const string HID_CONTAINER_ID = "HID";

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private IReadOnlyList<DeviceInformation> connectedDevices;
        private IVBAXServiceProvider services;
        private ResourceLoader resources;
        private HIDInputChannel currentChannel;

        private string[] supportedDeviceSelectors = new string[]
        {
            HidDevice.GetDeviceSelector(0x01, 0x05),
            HidDevice.GetDeviceSelector(0x01, 0x04)
        };

        public IVBAXServiceProvider ServiceProvider
        {
            get { return this.services; }
            set {
                this.services = value;
            }
        }

        public IReadOnlyList<DeviceInformation> AvailableDevices
        {
            get
            {
                return connectedDevices;
            }

            private set
            {
                if (connectedDevices != value)
                {
                    connectedDevices = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public HIDInputChannel CurrentChannel
        {
            get
            {
                return currentChannel;
            }
        }

        private HIDManager()
        {
            this.resources = new ResourceLoader();
        }

        public IAsyncAction RefreshDeviceListAsync()
        {
            Func<Task> helper = async () =>
            {
                List<DeviceInformation> results = new List<DeviceInformation>();

                foreach (var selector in this.supportedDeviceSelectors)
                {
                    var deviceResults = await DeviceInformation.FindAllAsync(selector);
                    results.AddRange(deviceResults);
                }

                this.AvailableDevices = results;
            };

            return helper().AsAsyncAction();
        }

        public IAsyncOperation<HIDInputChannel> ConnectAsync(DeviceInformation deviceInfo)
        {
            if(this.currentChannel != null)
            {
                this.currentChannel.Dispose();
                this.currentChannel = null;
            }
            Func<Task<HIDInputChannel>> helper = async () =>
            {
                if(this.connectedDevices == null)
                {
                    return null;
                }
                var device = await this.connectAsync(deviceInfo.Id);
                if (device == null)
                {
                    return null;
                }

                this.StoreHIDId(deviceInfo.Id);

                this.currentChannel = new HIDInputChannel(deviceInfo, device)
                {
                    Services = this.services
                };

                return this.currentChannel;
            };

            return helper().AsAsyncOperation();
        }

        public IAsyncOperation<HIDInputChannel> ReconnectAsync()
        {
            Func<Task<HIDInputChannel>> resumeHelper = async () =>
            {
                string id = this.currentChannel.DeviceId;
                if (id == null)
                {
                    return null;
                }
                var device = await this.connectAsync(id);
                if (device == null)
                {
                    return null;
                }
                this.currentChannel.Resume(device);
                return this.currentChannel;
            };

            Func<Task<HIDInputChannel>> restartHelper = async () =>
            {
                string id = this.GetStoredHIDId();
                if (id == null)
                {
                    return null;
                }
                var deviceInfo = await DeviceInformation.CreateFromIdAsync(id);
                var device = await this.connectAsync(id);
                if (device == null)
                {
                    this.ResetStoredHIDId();
                    return null;
                }
                this.currentChannel = new HIDInputChannel(deviceInfo, device)
                {
                    Services = this.services
                };
                return this.currentChannel;
            };

            if(this.currentChannel != null)
            {
                return resumeHelper().AsAsyncOperation();
            }
            else
            {
                return restartHelper().AsAsyncOperation();
            }
        }

        private async Task<HidDevice> connectAsync(string id)
        {
            try
            {
                var device = await HidDevice.FromIdAsync(id, Windows.Storage.FileAccessMode.Read);
                if(device == null)
                {
                    var deviceAccessStatus = DeviceAccessInformation.CreateFromId(id).CurrentStatus;
                    var caption = this.resources.GetString("errorCaption");
                    string msg = string.Empty;
                    switch (deviceAccessStatus)
                    {
                        case DeviceAccessStatus.DeniedBySystem:
                            msg = this.resources.GetString("hidDeniedBySystem");
                            break;
                        case DeviceAccessStatus.DeniedByUser:
                            msg = this.resources.GetString("hidDeniedByUser");
                            break;
                    }
                    if (this.services != null && msg != string.Empty)
                    {
                        await this.services.MessageService.ShowMessage(msg, caption);
                    }
                    return null;
                }
                return device;
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
                return null;
            }
        }

        private String GetStoredHIDId()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Getting stored HID id");
#endif
            var settings = ApplicationData.Current.LocalSettings;
            ApplicationDataContainer container = null;

            if(settings.Containers.ContainsKey(HID_CONTAINER_ID))
            {
                try
                {
                    container = settings.CreateContainer(HID_CONTAINER_ID, ApplicationDataCreateDisposition.Existing);
                }
                catch (Exception)
                {
                    return null;
                }

                object id = null;
                container.Values.TryGetValue(HID_DEVICE_ID, out id);
                return (id as string);
            }
            return null;
        }

        private void StoreHIDId(String id)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Storing HID id");
#endif
            var settings = ApplicationData.Current.LocalSettings;
            ApplicationDataContainer container = 
                settings.CreateContainer(HID_CONTAINER_ID, ApplicationDataCreateDisposition.Always);
            if(settings.Containers.ContainsKey(HID_CONTAINER_ID))
            {
                container.Values[HID_DEVICE_ID] = id;
            }
        }

        private void ResetStoredHIDId()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Resetting stored HID id");
#endif
            var settings = ApplicationData.Current.LocalSettings;
            ApplicationDataContainer container =
                settings.CreateContainer(HID_CONTAINER_ID, ApplicationDataCreateDisposition.Always);
            if (settings.Containers.ContainsKey(HID_CONTAINER_ID))
            {
                container.Values.Remove(HID_DEVICE_ID);
            }
        }

        public void DisconnectChannel()
        {
            if(this.currentChannel != null)
            {
                this.currentChannel.Dispose();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
