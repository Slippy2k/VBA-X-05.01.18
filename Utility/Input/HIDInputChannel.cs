using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Utility
{
    public delegate void ControlChangedCallback(HIDMapping mapping);

    public enum DPadDirection
    {
        Left,
        Up,
        Right,
        Down
    }

    internal static class HIDMappingExtensions
    {
        internal static ApplicationDataCompositeValue ToCompositeValue(this HIDMapping mapping)
        {
            ApplicationDataCompositeValue value = new ApplicationDataCompositeValue();
            value["DisplayName"] = mapping.DisplayName;
            value["UsagePage"] = mapping.UsagePage;
            value["UsageId"] = mapping.UsageId;
            value["Sign"] = mapping.Sign;
            value["Direction"] = (int)mapping.Direction;
            value["IsNumeric"] = mapping.IsNumeric;
            return value;
        }

        internal static HIDMapping FromCompositeValue(ApplicationDataCompositeValue value)
        {
            HIDMapping mapping = new HIDMapping();
            mapping.DisplayName = value["DisplayName"].ToString();
            mapping.UsagePage = ushort.Parse(value["UsagePage"].ToString());
            mapping.UsageId = ushort.Parse(value["UsageId"].ToString());
            mapping.Sign = short.Parse(value["Sign"].ToString());
            mapping.Direction = (DPadDirection)(int.Parse(value["Direction"].ToString()));
            mapping.IsNumeric = bool.Parse(value["IsNumeric"].ToString());
            return mapping;
        }
    }

    public struct HIDMapping
    {
        public String DisplayName;
        public ushort UsagePage;
        public ushort UsageId;
        public short Sign;
        public DPadDirection Direction;
        public bool IsNumeric;
    }

    public sealed class HIDButtonMap : IDictionary<Button, HIDMapping>
    {
        private Dictionary<Button, HIDMapping> dict;

        public HIDButtonMap()
        {
            this.dict = new Dictionary<Button, HIDMapping>();
        }


        public HIDMapping this[Button key]
        {
            get
            {
                return this.dict[key];
            }

            set
            {
                this.dict[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return this.dict.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<Button> Keys
        {
            get
            {
                return this.dict.Keys;
            }
        }

        public ICollection<HIDMapping> Values
        {
            get
            {
                return this.dict.Values;
            }
        }

        public void Add(KeyValuePair<Button, HIDMapping> item)
        {
            this.dict.Add(item.Key, item.Value);
        }

        public void Add(Button key, HIDMapping value)
        {
            this.dict.Add(key, value);
        }

        public void Clear()
        {
            this.dict.Clear();
        }

        public bool Contains(KeyValuePair<Button, HIDMapping> item)
        {
            return this.dict.Contains(item);
        }

        public bool ContainsKey(Button key)
        {
            return this.dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<Button, HIDMapping>[] array, int arrayIndex)
        {
            int i = 0;
            foreach (var item in this.dict)
            {
                array[i + arrayIndex] = item;

                i++;
            }
        }

        public IEnumerator<KeyValuePair<Button, HIDMapping>> GetEnumerator()
        {
            return this.dict.GetEnumerator();
        }

        public bool Remove(KeyValuePair<Button, HIDMapping> item)
        {
            return this.dict.Remove(item.Key);
        }

        public bool Remove(Button key)
        {
            return this.dict.Remove(key);
        }

        public bool TryGetValue(Button key, out HIDMapping value)
        {
            return this.dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.dict.GetEnumerator();
        }
    }

    public enum Button
    {
        Left,
        Up,
        Right,
        Down,
        Start,
        Select,
        A, B,
        X, Y,
        L, R,
        L2, R2,
        L3, R3,
        Turbo,
        TurboToggle
    }

    public struct ButtonStates
	{
		public bool LeftPressed;
        public bool UpPressed;
        public bool RightPressed;
        public bool DownPressed;
        public bool APressed;
        public bool BPressed;
        public bool LPressed;
        public bool RPressed;
        public bool StartPressed;
        public bool SelectPressed;
        public bool TurboPressed;
    };

    public sealed class ControllerState 
	{
		internal ButtonStates buttons;

		public ButtonStates Buttons { get { return this.buttons; } }

        public ControllerState()
        {
            this.buttons = default(ButtonStates);
        }

        public void Reset()
        {
            this.buttons = new ButtonStates();
        }
	};

    public sealed class HIDInputChannel : IDisposable
    {
        private const string HID_SAVE_ID = "HID_{0}";
        private const string PRIM_MAPPING_ID = "PRIMARY_BUTTON_MAP";
        private const string SEC_MAPPING_ID = "SECONDARY_BUTTON_MAP";

        public event ControlChangedCallback ControlChanged;

        // first element is usage page, second element is usage id
        private static ushort[] NumericUsages = new ushort[]
        {
            0x01, 0x30,             // X
            0x01, 0x31,             // Y
            0x01, 0x32,             // Z
            0x01, 0x33,             // Rx
            0x01, 0x34,             // Ry
            0x01, 0x35,             // Rz
            0x01, 0x39,             // Hat Switch (D-Pad)
            0x01, 0x40,             // Vx
            0x01, 0x41,             // Vy
            0x01, 0x42,             // Vz
            0x01, 0x43,             // Vbrx
            0x01, 0x44,             // Vbry
            0x01, 0x45,             // Vbrz
            0x02, 0xC4,             // Accelerator (Trigger)
            0x02, 0xC5              // Brake (Trigger)
        };

        // usage page as index
        private static ushort[] NumericCenters = new ushort[]
        {
            0,
            127,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        };

        private const float DEADZONE = 0.15f;

        private ControllerState state;
        private HIDButtonMap mapping;
        private HIDButtonMap mappingAlternative;
        private ResourceLoader resources;
        private IVBAXServiceProvider services;

        private HidDevice currentDevice;
        private DeviceInformation deviceInfo;
        private HidInputReport lastReport;
        private IList<HidNumericControlDescription> numControlDescs;
        private bool setupMode;

        private String axisString;
        private String hidDPadUp;
        private String hidDPadLeft;
        private String hidDPadRight;
        private String hidDPadDown;

        public DeviceInformation DeviceInfo
        {
            get
            {
                return deviceInfo;
            }
        }

        public bool SetupMode
        {
            get
            {
                return this.setupMode;
            }
            set
            {
                if(this.setupMode != value)
                {
                    this.setupMode = value;
                    if(!value)
                    {
                        this.StoreLayout();
                    }
                }
            }
        }

        public HIDButtonMap Mapping
        {
            get
            {
                return mapping;
            }
        }

        public HIDButtonMap MappingAlternative
        {
            get
            {
                return mappingAlternative;
            }
        }

        public string DeviceId
        {
            get
            {
                return deviceInfo.Id;
            }
        }

        internal IVBAXServiceProvider Services
        {
            get
            {
                return services;
            }

            set
            {
                services = value;
            }
        }

        public HIDInputChannel(DeviceInformation deviceInfo, HidDevice device)
        {
            this.currentDevice = device;
            this.deviceInfo = deviceInfo;
            this.state = new ControllerState();
            this.resources = new ResourceLoader();
            this.mapping = new HIDButtonMap();
            this.mappingAlternative = new HIDButtonMap();
            this.axisString = this.resources.GetString("hidAxisDescription");
            this.hidDPadUp = this.resources.GetString("hidDpadUp");
            this.hidDPadLeft = this.resources.GetString("hidDpadLeft");
            this.hidDPadRight = this.resources.GetString("hidDpadRight");
            this.hidDPadDown = this.resources.GetString("hidDpadDown");

            this.getNumericControlDescs();

            this.RestoreLayout();

            this.currentDevice.InputReportReceived += CurrentDevice_InputReportReceived;
        }

        private void getNumericControlDescs()
        {
            if(this.numControlDescs == null)
            {
                this.numControlDescs = new List<HidNumericControlDescription>();
            }
            else
            {
                this.numControlDescs.Clear();
            }

            for (int i = 1; i < NumericUsages.Length; i += 2)
            {
                ushort usagePage = NumericUsages[i - 1];
                ushort usageId = NumericUsages[i];

                var numDescs = this.currentDevice.GetNumericControlDescriptions(HidReportType.Input, usagePage, usageId);
                foreach (var desc in numDescs)
                {
                    this.numControlDescs.Add(desc);
                }
            }
        }

        private void CurrentDevice_InputReportReceived(HidDevice sender, HidInputReportReceivedEventArgs args)
        {
            HidInputReport report = args.Report;

            try
            {
                processReport(report);
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
            }
        }

        private void processReport(HidInputReport report)
        {
            if (this.lastReport != null)
            {
                if (this.SetupMode)
                {
                    foreach (var boolControl in report.ActivatedBooleanControls)
                    {
                        var lastControl = this.lastReport.GetBooleanControl(boolControl.UsagePage, boolControl.UsageId);
                        if (boolControl.IsActive && !lastControl.IsActive)
                        {
                            HIDMapping mapping = new HIDMapping()
                            {
                                DisplayName = String.Format(this.resources.GetString("hidButtonDescription"), boolControl.Id),
                                UsagePage = boolControl.UsagePage,
                                UsageId = boolControl.UsageId,
                                IsNumeric = false
                            };

                            this.ControlChanged(mapping);
                        }
                    }

                    foreach (var numControlDesc in this.numControlDescs)
                    {
                        var numControl = report.GetNumericControlByDescription(numControlDesc);
                        var lastNumControl = this.lastReport.GetNumericControlByDescription(numControlDesc);
                        if (numControl != null && lastNumControl != null && numControl.Value != lastNumControl.Value)
                        {
                            if (CheckDeadzone(numControlDesc, numControl) &&
                               !CheckDeadzone(lastNumControl.ControlDescription, lastNumControl))
                            {
                                ushort center = NumericCenters[numControl.UsagePage];
                                short axisSign = (short)Math.Sign((short)numControl.Value - (short)center);
                                String sign = string.Empty;
                                if (center != 0)
                                {
                                    sign = axisSign < 0 ? "-" : "+";
                                }
                                String displayName = String.Format(this.axisString, sign, numControl.Id);

                                DPadDirection direction = default(DPadDirection);
                                if (numControlDesc.UsagePage == 0x01 && numControlDesc.UsageId == 0x39)
                                {
                                    switch (numControl.Value)
                                    {
                                        case 0:
                                            direction = DPadDirection.Up;
                                            displayName = this.hidDPadUp;
                                            break;
                                        case 2:
                                            direction = DPadDirection.Right;
                                            displayName = this.hidDPadRight;
                                            break;
                                        case 4:
                                            direction = DPadDirection.Down;
                                            displayName = this.hidDPadDown;
                                            break;
                                        case 6:
                                            direction = DPadDirection.Left;
                                            displayName = this.hidDPadLeft;
                                            break;
                                    }
                                }
                                HIDMapping mapping = new HIDMapping()
                                {
                                    DisplayName = displayName,
                                    UsagePage = numControl.UsagePage,
                                    UsageId = numControl.UsageId,
                                    Sign = axisSign,
                                    Direction = direction,
                                    IsNumeric = true
                                };

                                this.ControlChanged(mapping);
                            }
                        }
                    }
                }
                else
                {
                    //ButtonStates newState = new ButtonStates();
                    //ProcessInputForMap(report, this.mapping, ref newState);
                    //ProcessInputForMap(report, this.mappingAlternative, ref newState);
                    //this.state.buttons = newState;
                }
            }
            this.lastReport = report;
        }

        private void ProcessInputForMap(HidInputReport report, HIDButtonMap buttonMap, ref ButtonStates newState)
        {
            foreach (var mappingItem in buttonMap)
            {
                HIDMapping mapping = mappingItem.Value;
                if (mapping.IsNumeric)
                {
                    HidNumericControl control = report.GetNumericControl(mapping.UsagePage, mapping.UsageId);
                    ushort value = (ushort)control.Value;
                    if (!this.CheckDeadzone(control.ControlDescription, control))
                    {
                        continue;
                    }
                    if (mapping.UsagePage == 0x01 && mapping.UsageId == 0x39)
                    {
                        // dpad
                        if ((mapping.Direction == DPadDirection.Down && value >= 3 && value <= 5) ||
                            (mapping.Direction == DPadDirection.Left && value >= 5 && value <= 7) ||
                            (mapping.Direction == DPadDirection.Up && (value == 7 || value == 0 || value == 1)) ||
                            (mapping.Direction == DPadDirection.Right && value >= 1 && value <= 3)
                            )
                        {
                            this.setButtonState(mappingItem.Key, ref newState);
                        }
                    }
                    else
                    {
                        // axis
                        ushort center = NumericCenters[mapping.UsagePage];
                        if ((value < center && mapping.Sign < 0) ||
                           (value > center && mapping.Sign > 0))
                        {
                            this.setButtonState(mappingItem.Key, ref newState);
                        }
                    }
                }
                else
                {
                    HidBooleanControl control = report.GetBooleanControl(mapping.UsagePage, mapping.UsageId);
                    if (control.IsActive)
                    {
                        this.setButtonState(mappingItem.Key, ref newState);
                    }
                }
            }
        }

        private void setButtonState(Button button, ref ButtonStates state)
        {
            switch(button)
            {
                case Button.Left:
                    state.LeftPressed = state.LeftPressed | true;
                    break;
                case Button.Up:
                    state.UpPressed = state.UpPressed | true;
                    break;
                case Button.Right:
                    state.RightPressed = state.RightPressed | true;
                    break;
                case Button.Down:
                    state.DownPressed = state.DownPressed | true;
                    break;
                case Button.Start:
                    state.StartPressed = state.StartPressed | true;
                    break;
                case Button.Select:
                    state.SelectPressed = state.SelectPressed | true;
                    break;
                case Button.L:
                    state.LPressed = state.LPressed | true;
                    break;
                case Button.R:
                    state.RPressed = state.RPressed | true;
                    break;
                case Button.A:
                    state.APressed = state.APressed | true;
                    break;
                case Button.B:
                    state.BPressed = state.BPressed | true;
                    break;
                case Button.Turbo:
                    state.TurboPressed = state.TurboPressed | true;
                    break;
            }
        }

        private bool CheckDeadzone(HidNumericControlDescription desc, HidNumericControl control)
        {
            var range = desc.LogicalMaximum - desc.LogicalMinimum;
            var absoluteDeadzone = (int)(DEADZONE * range);
            var center = NumericCenters[desc.UsagePage];
            var testValue = Math.Abs(center - control.Value);
            if(desc.UsagePage == 0x01 && desc.UsageId == 0x39)
            {
                // dpad
                return (control.Value % 2 == 0) || !this.SetupMode;
            }
            return (testValue > absoluteDeadzone);
        }

        public void Suspend()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            if (this.currentDevice != null)
            {
                try
                {
                    this.currentDevice.InputReportReceived -= CurrentDevice_InputReportReceived;
                }catch(Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("INFO: " + e.Message);
#endif
                }
                this.lastReport = null;
                this.currentDevice.Dispose();
                this.currentDevice = null;
            }
        }

        public void Resume(HidDevice device)
        {
            this.Dispose();
            this.currentDevice = device;
            this.getNumericControlDescs();
            this.currentDevice.InputReportReceived += CurrentDevice_InputReportReceived;
        }

        public ControllerState GetCurrentState()
        {
            return this.state;
        }

        public bool IsConnected()
        {
            return currentDevice != null;
        }

        public void Update()
        {
            // nothing to do here
            if(!this.setupMode && this.lastReport != null)
            {
                ButtonStates newState = new ButtonStates();
                ProcessInputForMap(this.lastReport, this.mapping, ref newState);
                ProcessInputForMap(this.lastReport, this.mappingAlternative, ref newState);
                this.state.buttons = newState;
            }
        }

        private async void StoreLayout()
        {
            try
            {
                _StoreLayout();
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
                await this.showMessage(
                        this.resources.GetString("hidStoreLayoutError"),
                        this.resources.GetString("errorCaption")
                    );
            }
        }

        private void _StoreLayout()
        {
            string deviceIdentifier = this.currentDevice.ProductId.ToString() + "_" + this.currentDevice.VendorId;
            string key = string.Format(HID_SAVE_ID, deviceIdentifier);
            var settings = ApplicationData.Current.LocalSettings;
            ApplicationDataContainer settingsContainer =
                settings.CreateContainer(HIDManager.HID_CONTAINER_ID, ApplicationDataCreateDisposition.Always);

            ApplicationDataContainer channelContainer =
                settingsContainer.CreateContainer(key, ApplicationDataCreateDisposition.Always);

            ApplicationDataContainer primaryContainer =
                channelContainer.CreateContainer(PRIM_MAPPING_ID, ApplicationDataCreateDisposition.Always);
            ApplicationDataContainer secondaryContainer =
                channelContainer.CreateContainer(SEC_MAPPING_ID, ApplicationDataCreateDisposition.Always);

            this.storeButtonMap(primaryContainer, this.mapping);
            this.storeButtonMap(secondaryContainer, this.mappingAlternative);
        }

        private void storeButtonMap(ApplicationDataContainer container, HIDButtonMap buttonMap)
        {
            container.Values.Clear();
            foreach (var entry in buttonMap)
            {
                container.Values[((int)entry.Key).ToString()] = entry.Value.ToCompositeValue();
            }
        }

        private async void RestoreLayout()
        {
            try
            {
                _RestoreLayout();
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
                await this.showMessage(
                        this.resources.GetString("hidRestoreLayoutError"),
                        this.resources.GetString("errorCaption")
                    );
            }
        }

        private void _RestoreLayout()
        {
            string deviceIdentifier = this.currentDevice.ProductId.ToString() + "_" + this.currentDevice.VendorId;
            string key = string.Format(HID_SAVE_ID, deviceIdentifier);
            var settings = ApplicationData.Current.LocalSettings;
            ApplicationDataContainer settingsContainer = null;
            ApplicationDataContainer channelContainer = null;

            try
            {
                settingsContainer = settings.CreateContainer(HIDManager.HID_CONTAINER_ID, ApplicationDataCreateDisposition.Existing);
                channelContainer = settingsContainer.CreateContainer(key, ApplicationDataCreateDisposition.Existing);
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
                return;
            }

            try
            {
                ApplicationDataContainer primaryContainer =
                    channelContainer.CreateContainer(PRIM_MAPPING_ID, ApplicationDataCreateDisposition.Existing);
                this.restoreButtonMap(primaryContainer, this.mapping);
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
            }

            try
            {
                ApplicationDataContainer secondaryContainer =
                    channelContainer.CreateContainer(SEC_MAPPING_ID, ApplicationDataCreateDisposition.Existing);
                this.restoreButtonMap(secondaryContainer, this.mappingAlternative);
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
            }
        }

        private void restoreButtonMap(ApplicationDataContainer container, HIDButtonMap buttonMap)
        {
            foreach (var entry in container.Values)
            {
                ApplicationDataCompositeValue value = entry.Value as ApplicationDataCompositeValue;
                HIDMapping mapping = HIDMappingExtensions.FromCompositeValue(value);
                buttonMap[(Button)(int.Parse(entry.Key.ToString()))] = mapping;
            }
        }

        private async Task showMessage(string msg, string caption)
        {
            if(this.services != null)
            {
                await this.services.MessageService.ShowMessage(msg, caption);
            }
        }
    }
}
