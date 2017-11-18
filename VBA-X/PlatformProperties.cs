using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace VBA_X
{
    static class PlatformProperties
    {
        public static EmulatorComponent.DeviceType DeviceType
        {
            get
            {
                return IsMobile ? 
                    EmulatorComponent.DeviceType.Mobile : 
                    EmulatorComponent.DeviceType.Default;
            }
        }


        public static bool IsMobile
        {
            get
            {
                var platformFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
                return platformFamily.ToLower().Contains("mobile");
            }
        }

        public static Size AdSize
        {
            get
            {
                if(IsMobile)
                {
                    return new Size(320, 50);
                }
                return new Size(728, 90);
            }
        }

        public static float AdMaxWidth
        {
            get
            {
                if(IsMobile)
                {
                    return 320;
                }
                return 500;
            }
        }

        public static double LibraryEntryTargetWidth
        {
            get
            {
                if(IsMobile)
                {
                    return 250.0;
                }
                return 350.0;
            }
        }

        public static SplitViewDisplayMode InMenuPaneDisplayMode
        {
            get
            {
                return (IsMobile) ? SplitViewDisplayMode.CompactOverlay : SplitViewDisplayMode.CompactInline;
            }
        }

        public static SplitViewDisplayMode InGamePaneDisplayMode
        {
            get
            {
                return SplitViewDisplayMode.Overlay;
            }
        }

        public static bool ClosePaneOnNavigation
        {
            get
            {
                return IsMobile;
            }
        }

        public static bool HasKeyBindings
        {
            get
            {
                return !IsMobile;
            }
        }

        public static bool AlwaysFullscreen
        {
            get
            {
                return IsMobile;
            }
        }
    }
}
