using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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
using Windows.UI.Input;
using Utility;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.ApplicationModel.Store;
using Windows.Gaming.Input;

namespace VBA_X
{
    public delegate void PageDelegate(Page page);

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public event PageDelegate MainPageCreated = delegate { };

        private MainPage mainPage;
        private Color originalAccentColor;

        private Settings settings;        
        private EmulatorManager manager;

        public Color OriginalAccentColor
        {
            get { return this.originalAccentColor; }
        }

        public Settings Settings
        {
            get { return settings; }
        }

        public MainPage MainPage
        {
            get { return this.mainPage; }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.RestoreSettings();
            this.SetApplicationTheme();

            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            this.UnhandledException += App_UnhandledException;
        }

        private async void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ResourceLoader loader = new ResourceLoader();
            UWPMessageService service = new UWPMessageService(this.mainPage.Dispatcher);

            await service.ShowMessage(loader.GetString("unexpectedException"), loader.GetString("errorCaption"));
        }

        private void RestoreSettings()
        {
            this.settings = new Settings();
        }

        private void SetApplicationTheme()
        {
            if (this.settings == null)
            {
                this.RequestedTheme = ApplicationTheme.Dark;
                return;
            }
            
            ApplicationTheme theme = this.settings.Theme;
            this.RequestedTheme = theme;
        }

        private void UpdateApplicationColor()
        {
            if(this.settings == null)
            {
                throw new InvalidOperationException("Settings not initialized.");
            }

            ApplicationColor accentColor = this.settings.AccentColor;
            if (accentColor != EmulatorComponent.ApplicationColor.Accent)
            {
                (Resources["AccentBrush"] as SolidColorBrush).Color = this.originalAccentColor;

                object accentBrushTmp = null;
                var resourceKey = accentColor.ToString() + "Brush";
                if (!this.Resources.TryGetValue(resourceKey, out accentBrushTmp))
                {
                    return;
                }
                SolidColorBrush accentBrush = accentBrushTmp as SolidColorBrush;
                if (accentBrush == null)
                {
                    return;
                }

                this.Resources["SystemAccentColor"] = accentBrush.Color;
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            this.originalAccentColor = (Color)Resources["SystemAccentColor"];
            this.UpdateApplicationColor();

            base.OnActivated(args);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (e.PreviousExecutionState != ApplicationExecutionState.ClosedByUser &&
                e.PreviousExecutionState != ApplicationExecutionState.NotRunning &&
                e.PreviousExecutionState != ApplicationExecutionState.Terminated)
            {
                return;
            }

            //if (PlatformProperties.AlwaysFullscreen)
            //{
            //    bool test = ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            //}

            this.originalAccentColor = (Color)Resources["SystemAccentColor"];
            this.UpdateApplicationColor();

            // deactivate pointer trail for performance reasons
            PointerVisualizationSettings pointerSetting = PointerVisualizationSettings.GetForCurrentView();
            pointerSetting.IsBarrelButtonFeedbackEnabled = pointerSetting.IsContactFeedbackEnabled = false;

            //Window.Current.Content = this.mainPage = new MainPage();
            //this.Initialize();
            //this.mainPage.InitializeMainPage();

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);

                this.mainPage = rootFrame.Content as MainPage;

                this.MainPageCreated(this.mainPage);
            }

            this.Initialize();
            this.mainPage.InitializeMainPage();

            // Ensure the current window is active
            Window.Current.Activate();
        }

        private async void Initialize()
        {
            if (this.settings == null)
            {
                throw new InvalidOperationException("Settings not initialized.");
            }

            Window window = Window.Current;

            this.manager = await EmulatorManager.InitializeAsync(window, this.mainPage, this.settings);

            if (PlatformProperties.AlwaysFullscreen)
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
                ApplicationView view = ApplicationView.GetForCurrentView();
                if (!view.IsFullScreenMode)
                {
                    view.TryEnterFullScreenMode();
                }
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        public async Task RequestExitAsync()
        {
            await this.SuspendApp();
            this.Exit();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            await SuspendApp();

            deferral.Complete();
        }

        private async Task SuspendApp()
        {
            if(this.manager != null)
            {
                await this.manager.SuspendAsync();
            }
        }

        private async void OnResuming(object sender, object e)
        {
            if (this.manager != null)
            {
                await this.manager.Resume();
            }
        }
    }
}
