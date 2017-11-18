using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using EmulatorComponent;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Foundation;
using Windows.UI;

namespace VBA_X
{
    public class Settings : ISettings, Utility.IKeySettings
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private ApplicationDataContainer settingsContainer;

        // DEFAULT VALUES

        // general settings
        private const ApplicationTheme DEFAULT_THEME = ApplicationTheme.Dark;
        private const ApplicationColor DEFAULT_ACCENT_COLOR = ApplicationColor.Accent;
        private const bool DEFAULT_SAVE_CONFIRMATION = true;
        private const bool DEFAULT_LOAD_CONFIRMATION = true;
        private const bool DEFAULT_RESET_CONFIRMATION = true;
        private const bool DEFAULT_MANUAL_SNAPSHOT = false;
        private const AutosaveInterval DEFAULT_AUTOSAVE_INTERVAL = AutosaveInterval.Off;

        // video settings
        private const bool DEFAULT_SHOW_FPS = false;
        private const int DEFAULT_FRAMESKIP = 0;
        private const int DEFAULT_TURBO_FRAMESKIP = 2;
        private const AspectRatio DEFAULT_ASPECT_RATIO = AspectRatio.Original;
        private const Filter DEFAULT_FILTER = Filter.Nearest;
        private const int DEFAULT_VIDEO_SCALE = 100;

        // control settings
        private const bool DEFAULT_SHOW_VIRTUAL_PAD = true;
        private const ControllerStyle DEFAULT_CONTROLLER_STYLE = ControllerStyle.EightWay;
        private const int DEFAULT_STICK_DEADZONE = 15;
        private const int DEFAULT_VIRTUAL_PAD_SCALE = 100;
        private const int DEFAULT_VIRTUAL_PAD_OPACITY = 50;
        private static readonly Point DEFAULT_DPAD_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_START_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_SELECT_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_TURBO_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_L_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_R_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_A_OFFSET = new Point(0.0, 0.0);
        private static readonly Point DEFAULT_B_OFFSET = new Point(0.0, 0.0);
        private const VirtualKey DEFAULT_BINDING_LEFT = VirtualKey.A;
        private const VirtualKey DEFAULT_BINDING_RIGHT = VirtualKey.D;
        private const VirtualKey DEFAULT_BINDING_UP = VirtualKey.W;
        private const VirtualKey DEFAULT_BINDING_DOWN = VirtualKey.S;
        private const VirtualKey DEFAULT_BINDING_A = VirtualKey.Right;
        private const VirtualKey DEFAULT_BINDING_B = VirtualKey.Down;
        private const VirtualKey DEFAULT_BINDING_L = VirtualKey.Left;
        private const VirtualKey DEFAULT_BINDING_R = VirtualKey.Up;
        private const VirtualKey DEFAULT_BINDING_START = VirtualKey.Enter;
        private const VirtualKey DEFAULT_BINDING_SELECT = VirtualKey.Space;
        private const VirtualKey DEFAULT_BINDING_TURBO = VirtualKey.Control;

        // audio settings
        private const bool DEFAULT_ENABLE_SOUND = true;
        private const bool DEFAULT_SYNC_AUDIO = true;
        private const int DEFAULT_SOUND_VOLUME = 100;

        private Page mainPage;

        public Settings()
        {
            App app = App.Current as App;
            if (app.MainPage != null)
            {
                this.mainPage = app.MainPage;
                this.mainPage.SizeChanged += MainPage_SizeChanged;
            }
            else
            {
                app.MainPageCreated += (p) =>
                {
                    this.mainPage = p;
                    this.mainPage.SizeChanged += MainPage_SizeChanged;
                };
            }

            this.InitializeContainer();
        }

        private void InitializeContainer()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            bool restoring = localSettings.Containers.ContainsKey("AppSettings");
            this.settingsContainer = localSettings.CreateContainer(
                "AppSettings",
                ApplicationDataCreateDisposition.Always
                );
            if (!restoring)
            {
                // first time - load default values into container
                this.settingsContainer.Values["Theme"] = (int)DEFAULT_THEME;
                this.settingsContainer.Values["AccentColor"] = (int)DEFAULT_ACCENT_COLOR;
                this.settingsContainer.Values["SaveConfirmation"] = DEFAULT_SAVE_CONFIRMATION;
                this.settingsContainer.Values["LoadConfirmation"] = DEFAULT_LOAD_CONFIRMATION;

                this.settingsContainer.Values["ShowFPS"] = DEFAULT_SHOW_FPS;
                this.settingsContainer.Values["FrameSkip"] = DEFAULT_FRAMESKIP;
                this.settingsContainer.Values["TurboFrameSkip"] = DEFAULT_TURBO_FRAMESKIP;
                this.settingsContainer.Values["AspectRatio"] = (int)DEFAULT_ASPECT_RATIO;
                this.settingsContainer.Values["Filter"] = (int)DEFAULT_FILTER;

                this.settingsContainer.Values["ShowVirtualController"] = DEFAULT_SHOW_VIRTUAL_PAD;
                this.settingsContainer.Values["ControllerStyle"] = (int)DEFAULT_CONTROLLER_STYLE;
                this.settingsContainer.Values["StickDeadzone"] = DEFAULT_STICK_DEADZONE;
                this.settingsContainer.Values["VirtualControllerScale"] = DEFAULT_VIRTUAL_PAD_SCALE;
                this.settingsContainer.Values["VirtualControllerOpacity"] = DEFAULT_VIRTUAL_PAD_OPACITY;
                this.settingsContainer.Values["LeftBinding"] = (int)DEFAULT_BINDING_LEFT;
                this.settingsContainer.Values["RightBinding"] = (int)DEFAULT_BINDING_RIGHT;
                this.settingsContainer.Values["UpBinding"] = (int)DEFAULT_BINDING_UP;
                this.settingsContainer.Values["DownBinding"] = (int)DEFAULT_BINDING_DOWN;
                this.settingsContainer.Values["ABinding"] = (int)DEFAULT_BINDING_A;
                this.settingsContainer.Values["BBinding"] = (int)DEFAULT_BINDING_B;
                this.settingsContainer.Values["LBinding"] = (int)DEFAULT_BINDING_L;
                this.settingsContainer.Values["RBinding"] = (int)DEFAULT_BINDING_R;
                this.settingsContainer.Values["StartBinding"] = (int)DEFAULT_BINDING_START;
                this.settingsContainer.Values["SelectBinding"] = (int)DEFAULT_BINDING_SELECT;
                this.settingsContainer.Values["TurboBinding"] = (int)DEFAULT_BINDING_TURBO;

                this.settingsContainer.Values["EnableSound"] = DEFAULT_ENABLE_SOUND;
                this.settingsContainer.Values["SyncAudio"] = DEFAULT_SYNC_AUDIO;

                this.settingsContainer.Values["SettingsVersion"] = 0;
            }
            if (this.Version < 1)
            {
                this.settingsContainer.Values["ResetConfirmation"] = DEFAULT_RESET_CONFIRMATION;

                this.Version = 1;
            }
            if (this.Version < 2)
            {
                this.settingsContainer.Values["ManualSnapshots"] = DEFAULT_MANUAL_SNAPSHOT;

                this.Version = 2;
            }
            if (this.Version < 3)
            {
                this.settingsContainer.Values["VideoScale"] = DEFAULT_VIDEO_SCALE;

                this.Version = 3;
            }
            if (this.Version < 4)
            {
                this.settingsContainer.Values["FirstLaunch"] = true;

                this.Version = 4;
            }
            if (this.Version < 5)
            {
                ApplicationDataCompositeValue dpadOffset = new ApplicationDataCompositeValue();
                dpadOffset["X"] = DEFAULT_DPAD_OFFSET.X;
                dpadOffset["Y"] = DEFAULT_DPAD_OFFSET.Y;

                ApplicationDataCompositeValue startOffset = new ApplicationDataCompositeValue();
                startOffset["X"] = DEFAULT_START_OFFSET.X;
                startOffset["Y"] = DEFAULT_START_OFFSET.Y;

                ApplicationDataCompositeValue selectOffset = new ApplicationDataCompositeValue();
                selectOffset["X"] = DEFAULT_SELECT_OFFSET.X;
                selectOffset["Y"] = DEFAULT_SELECT_OFFSET.Y;

                ApplicationDataCompositeValue aOffset = new ApplicationDataCompositeValue();
                aOffset["X"] = DEFAULT_A_OFFSET.X;
                aOffset["Y"] = DEFAULT_A_OFFSET.Y;

                ApplicationDataCompositeValue bOffset = new ApplicationDataCompositeValue();
                bOffset["X"] = DEFAULT_B_OFFSET.X;
                bOffset["Y"] = DEFAULT_B_OFFSET.Y;

                ApplicationDataCompositeValue lOffset = new ApplicationDataCompositeValue();
                lOffset["X"] = DEFAULT_L_OFFSET.X;
                lOffset["Y"] = DEFAULT_L_OFFSET.Y;

                ApplicationDataCompositeValue rOffset = new ApplicationDataCompositeValue();
                rOffset["X"] = DEFAULT_R_OFFSET.X;
                rOffset["Y"] = DEFAULT_R_OFFSET.Y;

                this.settingsContainer.Values["DPadOffset"] = dpadOffset;
                this.settingsContainer.Values["StartOffset"] = startOffset;
                this.settingsContainer.Values["SelectOffset"] = selectOffset;
                this.settingsContainer.Values["LOffset"] = lOffset;
                this.settingsContainer.Values["ROffset"] = rOffset;
                this.settingsContainer.Values["AOffset"] = aOffset;
                this.settingsContainer.Values["BOffset"] = bOffset;

                this.Version = 5;
            }
            if (this.Version < 6)
            {
                ApplicationDataCompositeValue dpadOffset = new ApplicationDataCompositeValue();
                dpadOffset["X"] = DEFAULT_DPAD_OFFSET.X;
                dpadOffset["Y"] = DEFAULT_DPAD_OFFSET.Y;

                ApplicationDataCompositeValue startOffset = new ApplicationDataCompositeValue();
                startOffset["X"] = DEFAULT_START_OFFSET.X;
                startOffset["Y"] = DEFAULT_START_OFFSET.Y;

                ApplicationDataCompositeValue selectOffset = new ApplicationDataCompositeValue();
                selectOffset["X"] = DEFAULT_SELECT_OFFSET.X;
                selectOffset["Y"] = DEFAULT_SELECT_OFFSET.Y;

                ApplicationDataCompositeValue aOffset = new ApplicationDataCompositeValue();
                aOffset["X"] = DEFAULT_A_OFFSET.X;
                aOffset["Y"] = DEFAULT_A_OFFSET.Y;

                ApplicationDataCompositeValue bOffset = new ApplicationDataCompositeValue();
                bOffset["X"] = DEFAULT_B_OFFSET.X;
                bOffset["Y"] = DEFAULT_B_OFFSET.Y;

                ApplicationDataCompositeValue lOffset = new ApplicationDataCompositeValue();
                lOffset["X"] = DEFAULT_L_OFFSET.X;
                lOffset["Y"] = DEFAULT_L_OFFSET.Y;

                ApplicationDataCompositeValue rOffset = new ApplicationDataCompositeValue();
                rOffset["X"] = DEFAULT_R_OFFSET.X;
                rOffset["Y"] = DEFAULT_R_OFFSET.Y;

                this.settingsContainer.Values["PDPadOffset"] = dpadOffset;
                this.settingsContainer.Values["PStartOffset"] = startOffset;
                this.settingsContainer.Values["PSelectOffset"] = selectOffset;
                this.settingsContainer.Values["PLOffset"] = lOffset;
                this.settingsContainer.Values["PROffset"] = rOffset;
                this.settingsContainer.Values["PAOffset"] = aOffset;
                this.settingsContainer.Values["PBOffset"] = bOffset;

                this.Version = 6;
            }
            if(this.Version < 7)
            {
                ApplicationDataCompositeValue turboOffset = new ApplicationDataCompositeValue();
                turboOffset["X"] = DEFAULT_TURBO_OFFSET.X;
                turboOffset["Y"] = DEFAULT_TURBO_OFFSET.Y;

                this.settingsContainer.Values["TurboOffset"] = turboOffset;
                this.settingsContainer.Values["PTurboOffset"] = turboOffset;

                this.Version = 7;
            }
            if (this.Version < 8)
            {
                this.settingsContainer.Values["SoundVolume"] = DEFAULT_SOUND_VOLUME;

                this.Version = 8;
            }
            if(this.Version < 9)
            {
                this.settingsContainer.Values["AutoSaveInterval"] = (int) DEFAULT_AUTOSAVE_INTERVAL;

                this.Version = 9;
            }
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            ApplicationView.PreferredLaunchWindowingMode =
                view.IsFullScreenMode ?
                ApplicationViewWindowingMode.FullScreen :
                ApplicationViewWindowingMode.PreferredLaunchViewSize;
            this.NotifyPropertyChanged("Fullscreen");
        }

        public bool FirstLaunch
        {
            get { return (bool)this.settingsContainer.Values["FirstLaunch"]; }
            set
            {
                this.settingsContainer.Values["FirstLaunch"] = value;
            }
        }

        private int Version
        {
            get { return (int)this.settingsContainer.Values["SettingsVersion"]; }
            set
            {
                this.settingsContainer.Values["SettingsVersion"] = value;
            }
        }

        public ApplicationTheme Theme
        {
            get
            {
                return (ApplicationTheme)(int)this.settingsContainer.Values["Theme"];
            }

            set
            {
                if (this.Theme != value)
                {
                    this.settingsContainer.Values["Theme"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public ApplicationColor AccentColor
        {
            get
            {
                return (ApplicationColor)(int)this.settingsContainer.Values["AccentColor"];
            }

            set
            {
                if (this.AccentColor != value)
                {
                    this.settingsContainer.Values["AccentColor"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public Color CurrentAccentColor
        {
            get
            {
                object tmp = null;
                if(!App.Current.Resources.TryGetValue("SystemAccentColor", out tmp) || !(tmp is Color))
                {
                    return Color.FromArgb(100, 100, 100, 100);
                }
                return (Color)tmp;
            }
        }

        public bool Fullscreen
        {
            get
            {
                return ApplicationView.PreferredLaunchWindowingMode == ApplicationViewWindowingMode.FullScreen;
            }
            set
            {
                if (this.Fullscreen != value)
                {
                    if (value)
                    {
                        ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                    }
                    else
                    {
                        ApplicationView.GetForCurrentView().ExitFullScreenMode();
                    }
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool SaveConfirmation
        {
            get
            {
                return (bool)this.settingsContainer.Values["SaveConfirmation"];
            }

            set
            {
                if (value != this.SaveConfirmation)
                {
                    this.settingsContainer.Values["SaveConfirmation"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool LoadConfirmation
        {
            get
            {
                return (bool)this.settingsContainer.Values["LoadConfirmation"];
            }

            set
            {
                if (value != this.LoadConfirmation)
                {
                    this.settingsContainer.Values["LoadConfirmation"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool ResetConfirmation
        {
            get
            {
                return (bool)this.settingsContainer.Values["ResetConfirmation"];
            }

            set
            {
                if (value != this.ResetConfirmation)
                {
                    this.settingsContainer.Values["ResetConfirmation"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool ManualSnapshots
        {
            get
            {
                return (bool)this.settingsContainer.Values["ManualSnapshots"];
            }

            set
            {
                if (value != this.ManualSnapshots)
                {
                    this.settingsContainer.Values["ManualSnapshots"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public AutosaveInterval AutoSaveInterval
        {
            get
            {
                return (AutosaveInterval)(int)this.settingsContainer.Values["AutoSaveInterval"];
            }

            set
            {
                if(value != this.AutoSaveInterval)
                {
                    this.settingsContainer.Values["AutoSaveInterval"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool ShowFPS
        {
            get
            {
                return (bool)this.settingsContainer.Values["ShowFPS"];
            }

            set
            {
                if (value != this.ShowFPS)
                {
                    this.settingsContainer.Values["ShowFPS"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int FrameSkip
        {
            get
            {
                return (int)this.settingsContainer.Values["FrameSkip"];
            }
            set
            {
                if (value != this.FrameSkip)
                {
                    this.settingsContainer.Values["FrameSkip"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int TurboFrameSkip
        {
            get
            {
                return (int)this.settingsContainer.Values["TurboFrameSkip"];
            }

            set
            {
                if (value != this.TurboFrameSkip)
                {
                    this.settingsContainer.Values["TurboFrameSkip"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public AspectRatio AspectRatio
        {
            get
            {
                return (AspectRatio)(int)this.settingsContainer.Values["AspectRatio"];
            }

            set
            {
                if (this.AspectRatio != value)
                {
                    this.settingsContainer.Values["AspectRatio"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public Filter Filter
        {
            get
            {
                return (Filter)(int)this.settingsContainer.Values["Filter"];
            }

            set
            {
                if (this.Filter != value)
                {
                    this.settingsContainer.Values["Filter"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int VideoScale
        {
            get
            {
                return (int)this.settingsContainer.Values["VideoScale"];
            }

            set
            {
                if (this.VideoScale != value)
                {
                    this.settingsContainer.Values["VideoScale"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool ShowVirtualController
        {
            get
            {
                return (bool)this.settingsContainer.Values["ShowVirtualController"];
            }

            set
            {
                if (this.ShowVirtualController != value)
                {
                    this.settingsContainer.Values["ShowVirtualController"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public ControllerStyle ControllerStyle
        {
            get
            {
                return (ControllerStyle)(int)this.settingsContainer.Values["ControllerStyle"];
            }

            set
            {
                if (this.ControllerStyle != value)
                {
                    this.settingsContainer.Values["ControllerStyle"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int StickDeadzone
        {
            get
            {
                return (int)this.settingsContainer.Values["StickDeadzone"];
            }

            set
            {
                if (this.StickDeadzone != value)
                {
                    this.settingsContainer.Values["StickDeadzone"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int VirtualControllerScale
        {
            get
            {
                return (int)this.settingsContainer.Values["VirtualControllerScale"];
            }

            set
            {
                if (this.VirtualControllerScale != value)
                {
                    this.settingsContainer.Values["VirtualControllerScale"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int VirtualControllerOpacity
        {
            get
            {
                return (int)this.settingsContainer.Values["VirtualControllerOpacity"];
            }
            set
            {
                if (this.VirtualControllerOpacity != value)
                {
                    this.settingsContainer.Values["VirtualControllerOpacity"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        private void SetOffsetValue(Point value, string propName)
        {
            object val = null;
            ApplicationDataCompositeValue compValue = null;
            if (this.settingsContainer.Values.TryGetValue(propName, out val) &&
              (compValue = val as ApplicationDataCompositeValue) != null)
            {
                if (((double)compValue["X"]) != value.X || (((double)compValue["Y"]) != value.Y))
                {
                    compValue["X"] = value.X;
                    compValue["Y"] = value.Y;
                    this.settingsContainer.Values[propName] = compValue;

                    this.NotifyPropertyChanged(propName);
                }
            }
        }

        private Point GetOffsetValue(Point p, String propName)
        {
            object value = null;
            ApplicationDataCompositeValue compValue = null;
            if (this.settingsContainer.Values.TryGetValue(propName, out value) &&
              (compValue = value as ApplicationDataCompositeValue) != null)
            {
                p.X = (double)compValue["X"];
                p.Y = (double)compValue["Y"];
            }
            return p;
        }

        public Point BOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_B_OFFSET, "BOffset");
            }

            set
            {
                SetOffsetValue(value, "BOffset");
            }
        }

        public Point AOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_A_OFFSET, "AOffset");
            }

            set
            {
                SetOffsetValue(value, "AOffset");
            }
        }

        public Point ROffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_R_OFFSET, "ROffset");
            }

            set
            {
                SetOffsetValue(value, "ROffset");
            }
        }

        public Point LOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_L_OFFSET, "LOffset");
            }

            set
            {
                SetOffsetValue(value, "LOffset");
            }
        }

        public Point SelectOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_SELECT_OFFSET, "SelectOffset");
            }

            set
            {
                SetOffsetValue(value, "SelectOffset");
            }
        }

        public Point TurboOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_TURBO_OFFSET, "TurboOffset");
            }

            set
            {
                SetOffsetValue(value, "TurboOffset");
            }
        }

        public Point StartOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_START_OFFSET, "StartOffset");
            }

            set
            {
                SetOffsetValue(value, "StartOffset");
            }
        }

        public Point DPadOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_DPAD_OFFSET, "DPadOffset");
            }

            set
            {
                SetOffsetValue(value, "DPadOffset");
            }
        }

        public Point PBOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_B_OFFSET, "PBOffset");
            }

            set
            {
                SetOffsetValue(value, "PBOffset");
            }
        }

        public Point PAOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_A_OFFSET, "PAOffset");
            }

            set
            {
                SetOffsetValue(value, "PAOffset");
            }
        }

        public Point PROffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_R_OFFSET, "PROffset");
            }

            set
            {
                SetOffsetValue(value, "PROffset");
            }
        }

        public Point PLOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_L_OFFSET, "PLOffset");
            }

            set
            {
                SetOffsetValue(value, "PLOffset");
            }
        }

        public Point PSelectOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_SELECT_OFFSET, "PSelectOffset");
            }

            set
            {
                SetOffsetValue(value, "PSelectOffset");
            }
        }

        public Point PTurboOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_TURBO_OFFSET, "PTurboOffset");
            }

            set
            {
                SetOffsetValue(value, "PTurboOffset");
            }
        }

        public Point PStartOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_START_OFFSET, "PStartOffset");
            }

            set
            {
                SetOffsetValue(value, "PStartOffset");
            }
        }

        public Point PDPadOffset
        {
            get
            {
                return GetOffsetValue(DEFAULT_DPAD_OFFSET, "PDPadOffset");
            }

            set
            {
                SetOffsetValue(value, "PDPadOffset");
            }
        }

        public VirtualKey LeftBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["LeftBinding"];
            }
            set
            {
                if (this.LeftBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["LeftBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey RightBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["RightBinding"];
            }

            set
            {
                if (this.RightBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["RightBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey UpBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["UpBinding"];
            }

            set
            {
                if (this.UpBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["UpBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey DownBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["DownBinding"];
            }

            set
            {
                if (this.DownBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["DownBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey ABinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["ABinding"];
            }

            set
            {
                if (this.ABinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["ABinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey BBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["BBinding"];
            }

            set
            {
                if (this.BBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["BBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey LBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["LBinding"];
            }

            set
            {
                if (this.LBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["LBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey RBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["RBinding"];
            }

            set
            {
                if (this.RBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["RBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey StartBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["StartBinding"];
            }

            set
            {
                if (this.StartBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["StartBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey SelectBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["SelectBinding"];
            }

            set
            {
                if (this.SelectBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["SelectBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public VirtualKey TurboBinding
        {
            get
            {
                return (VirtualKey)(int)this.settingsContainer.Values["TurboBinding"];
            }

            set
            {
                if (this.TurboBinding != value && value != VirtualKey.None)
                {
                    this.settingsContainer.Values["TurboBinding"] = (int)value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool EnableSound
        {
            get
            {
                return (bool)this.settingsContainer.Values["EnableSound"];
            }

            set
            {
                if (this.EnableSound != value)
                {
                    this.settingsContainer.Values["EnableSound"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool SyncAudio
        {
            get
            {
                return (bool)this.settingsContainer.Values["SyncAudio"];
            }

            set
            {
                if (this.SyncAudio != value)
                {
                    this.settingsContainer.Values["SyncAudio"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int SoundVolume
        {
            get
            {
                return (int)this.settingsContainer.Values["SoundVolume"];
            }

            set
            {
                if (this.SoundVolume != value)
                {
                    this.settingsContainer.Values["SoundVolume"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
