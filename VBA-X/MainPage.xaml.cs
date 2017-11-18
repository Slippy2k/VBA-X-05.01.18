using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using VBA_X.Pages;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VBA_X
{
    public delegate void CallbackDelegate();

    public class NavLink
    {
        public string Label { get; set; }
        public Symbol Symbol { get; set; }
        public Type TargetPage { get; set; }
        public SplitViewDisplayMode MenuDisplayMode { get; set; }
        public bool PaneCloseOnTap { get; set; } = false;
        public bool PaneCloseOnNavigation { get; set; } = false;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IMainView
    {
        public event CallbackDelegate PaneOpening = delegate { };
        private ResourceLoader resources;
        private ObservableCollection<NavLink> topNavLinks;
        private ObservableCollection<NavLink> bottomNavLinks;
        private bool closePaneOnTap = true;

        public SwapChainPanel SwapChainPanel
        {
            get { return this.swapChainPanel; }
        }

        public UIElement BackgroundPane
        {
            get { return this.mainGrid; }
        }

        public SplitView SplitView
        {
            get { return this.splitView; }
        }

        private ObservableCollection<NavLink> TopNavLinks
        {
            get
            {
                return topNavLinks;
            }
        }
        private ObservableCollection<NavLink> BottomNavLinks
        {
            get
            {
                return bottomNavLinks;
            }
        }

        public bool IsOnGameView
        {
            get
            {
                return (this.topItemsList.SelectedIndex == 0 || (this.topItemsList.SelectedIndex < 0 && this.bottomItemsList.SelectedIndex < 0)) && !this.splitView.IsPaneOpen;
            }
        }

        public MainPage()
        {
            this.SetPageTheme();

            this.InitializeComponent();

            this.resources = new ResourceLoader();

            this.topNavLinks = new ObservableCollection<NavLink>() {
                new NavLink() {
                    Label = resources.GetString("MenuPlayLabel"),
                    Symbol = Symbol.Play,
                    TargetPage = typeof(ControllerOverlayPage),
                    MenuDisplayMode = PlatformProperties.InGamePaneDisplayMode,
                    PaneCloseOnTap = true,
                    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                },
                new NavLink() {
                    Label = resources.GetString("MenuLibLabel"),
                    Symbol = Symbol.Library,
                    TargetPage = typeof(LibraryPage),
                    MenuDisplayMode = PlatformProperties.InMenuPaneDisplayMode,
                    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                },
                new NavLink() {
                    Label = resources.GetString("MenuCheatsLabel"),
                    Symbol = Symbol.Repair,
                    TargetPage = typeof(CheatsPage),
                    MenuDisplayMode = PlatformProperties.InMenuPaneDisplayMode,
                    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                }//,
                //new NavLink() {
                //    Label = resources.GetString("MenuCloudLabel"),
                //    Symbol = Symbol.Upload,
                //    TargetPage = typeof(CloudPage),
                //    MenuDisplayMode = PlatformProperties.InMenuPaneDisplayMode,
                //    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                //}
                //,
                //new NavLink() {
                //    Label = "HID",
                //    Symbol = Symbol.Calculator,
                //    TargetPage = typeof(HIDSetupPage),
                //    MenuDisplayMode = PlatformProperties.InMenuPaneDisplayMode,
                //    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                //}
            };

            this.bottomNavLinks = new ObservableCollection<NavLink>() {
                new NavLink() {
                    Label = resources.GetString("MenuSettingsLabel"),
                    Symbol = Symbol.Setting,
                    TargetPage = typeof(SettingsPage),
                    MenuDisplayMode = PlatformProperties.InMenuPaneDisplayMode,
                    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                },
                new NavLink() {
                    Label = resources.GetString("MenuAboutLabel"),
                    Symbol = Symbol.Help,
                    TargetPage = typeof(AboutPage),
                    MenuDisplayMode = PlatformProperties.InMenuPaneDisplayMode,
                    PaneCloseOnNavigation = PlatformProperties.ClosePaneOnNavigation
                }
            };
        }

        public void InitializeMainPage()
        {
            this.registerEvents();

            this.navigateToMenuItem(this.topNavLinks[1]);
        }

        private void registerEvents()
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) =>
            {
                if ((this.topItemsList.SelectedIndex != 0) &&
                    (this.topItemsList.SelectedIndex != -1 || this.bottomItemsList.SelectedIndex != -1))
                {
                    this.topItemsList.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (this.topItemsList.SelectedIndex == 0 ||
                        (this.topItemsList.SelectedIndex == -1 && this.bottomItemsList.SelectedIndex == -1))
                {
                    TriggerPane();
                    e.Handled = true;
                }
            };

            this.splitView.PaneClosed += (o, e) =>
            {
                // prevents to accidentally reopen the pane when pressing enter 
                // or space in-game
                splitView.Focus(FocusState.Programmatic);
            };

            this.SizeChanged += (o, e) =>
            {
                if (e.NewSize.Width < e.NewSize.Height)
                {
                    VisualStateManager.GoToState(this, "mobilePortraitState", false);
                }
                else
                {
                    VisualStateManager.GoToState(this, "normalState", false);
                }
            };
        }

        private void TriggerPane()
        {
            this.splitView.IsPaneOpen = !this.splitView.IsPaneOpen;
            if (this.splitView.IsPaneOpen)
            {
                PaneOpening();
            }
        }

        private async Task<bool> requestExitAsync()
        {
            MessageDialog dialog = new MessageDialog(this.resources.GetString("exitAppConfirm"), this.resources.GetString("exitAppConfirmCaption"));
            bool result = false;
            dialog.Commands.Add(new UICommand(this.resources.GetString("exitAppConfirmYes"), (c) =>
            {
                result = true;
            }));
            dialog.Commands.Add(new UICommand(this.resources.GetString("exitAppConfirmNo"), (c) =>
            {
                result = false;
            }));

            await dialog.ShowAsync();
            return result;
        }

        private void SetPageTheme()
        {
            Settings settings = (App.Current as App)?.Settings;

            if (settings == null)
            {
                throw new InvalidOperationException("Settings not initialized.");
            }

            this.RequestedTheme = (ElementTheme)((int)settings.Theme + 1);

            settings.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName.Equals("Theme"))
                {
                    this.RequestedTheme = (ElementTheme)((int)settings.Theme + 1);
                }
            };
        }

        private void splitviewToggle_Click(object sender, RoutedEventArgs e)
        {
            TriggerPane();
        }

        private void topItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (navigateMenuItem(e))
            {
                bottomItemsList.SelectedItem = null;
            }
        }

        private void bottomItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (navigateMenuItem(e))
            {
                topItemsList.SelectedItem = null;
            }
        }

        private bool navigateMenuItem(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var menuItem = e.AddedItems.First() as NavLink;
                navigateToMenuItem(menuItem);

                return true;
            }
            return false;
        }

        private void navigateToMenuItem(NavLink menuItem)
        {
            this.closePaneOnTap = menuItem.PaneCloseOnTap;

            SplitViewFrame.Navigate(menuItem.TargetPage);
            splitView.DisplayMode = menuItem.MenuDisplayMode;

            if (menuItem.PaneCloseOnNavigation)
            {
                splitView.IsPaneOpen = false;
            }
        }

        private void Content_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.closePaneOnTap)
            {
                this.splitView.IsPaneOpen = false;
            }
        }

        public void ShowEmulatorPage()
        {
            this.topItemsList.SelectedIndex = 0;
            splitView.IsPaneOpen = false;
        }

        public void ShowNaviPane()
        {
            this.splitView.IsPaneOpen = true;
        }

        public void NavigateTo(Type page)
        {
            this.SplitViewFrame.Navigate(page);
        }

        public void NavigateBack()
        {
            this.SplitViewFrame.GoBack();
        }
    }
}
