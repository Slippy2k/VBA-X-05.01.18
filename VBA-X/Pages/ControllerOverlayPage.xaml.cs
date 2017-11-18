using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Input;
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
    public sealed partial class ControllerOverlayPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private bool cmdBarUp = false;
        private bool cmdBarMoving = false;

        private int customizationBarState = 0;
        private int customizationBarMoveState = 0;

        private Settings settings;
        private ResourceLoader resources;
        private UWPMessageService msgService;
        private EmulatorManager manager;

        private KeyEventHandler keyDownHandler;
        private PointerEventHandler pointerHandler;
        private PointerEventHandler pointerMoveHandler;
        private TypedEventHandler<CoreWindow, PointerEventArgs> pointerReleaseHandler;
        private CallbackDelegate paneOpenHandler;

        private static bool pausedWhenLeavingPage = false;

        private bool eventsRegistered = false;

        private double startOpacity;
        private double startScale;
        private int startStyleIndex;


        public bool IsCustomizationMenuVisible
        {
            get
            {
                return customizationBarState != 0 || customizationBarMoveState != 0;
            }
        }

        public ControllerOverlayPage()
        {
            this.InitializeComponent();
            var app = (App.Current as App);
            this.resources = new ResourceLoader();
            this.msgService = new UWPMessageService(this.Dispatcher);
            this.settings = app.Settings;
            this.keyDownHandler = new KeyEventHandler(handleKeyDown);
            this.pointerHandler = new PointerEventHandler(handlePointerPressed);
            this.pointerMoveHandler = new PointerEventHandler(handlePointerMoved);
            this.pointerReleaseHandler = new TypedEventHandler<CoreWindow, PointerEventArgs>(handlePointerReleased);
            this.paneOpenHandler = new CallbackDelegate(handlePaneOpening);
            this.manualSnapshotButton.DataContext = this.settings;
            this.customizeVPadButton.DataContext = this.settings;
            this.controllerScaleSlider.DataContext = this.settings;
            this.controllerOpacitySlider.DataContext = this.settings;
            this.controllerStyleCombobox.DataContext = this.settings;
            this.fpsPanel.DataContext = this.settings;
            if (app.MainPage == null)
            {
                app.MainPageCreated += (p) =>
                {
                    this.manager = EmulatorManager.Current;
                    this.manager.MovedToForeground();
                    this.manager.Emulator.RenderComponent.FrameRateChanged += this.UpdateFPS;
                    SetSaveSlotContext(this.manager);
                    RegisterEvents(app);
                };
            }
            else
            {
                this.manager = EmulatorManager.Current;
                this.manager.MovedToForeground();
                this.manager.Emulator.RenderComponent.FrameRateChanged += this.UpdateFPS;
                RegisterEvents(app);
            }

            this.moveBarUp.Completed += MoveBarUp_Completed;
            this.moveBarDown.Completed += MoveBarDown_Completed;

            this.moveLayoutBarDown.Completed += MoveLayoutBarDown_Completed;
            this.moveLayoutBarUp.Completed += MoveLayoutBarUp_Completed;
            this.moveLayoutBarFurtherDown.Completed += MoveLayoutBarFurtherDown_Completed;
            this.moveLayoutBarHalfUp.Completed += MoveLayoutBarHalfUp_Completed;
            this.moveLayoutBarCompletelyUp.Completed += MoveLayoutBarCompletelyUp_Completed;
            this.moveLayoutBarCompletelyDown.Completed += MoveLayoutBarCompletelyDown_Completed;

            customizationPanel.DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.RegisterEvents(App.Current as App);
            SetSaveSlotContext(this.manager);
            this.manualSnapshotButton.DataContext = this.settings;
            this.customizeVPadButton.DataContext = this.settings;
            this.controllerScaleSlider.DataContext = this.settings;
            this.controllerOpacitySlider.DataContext = this.settings;
            this.controllerStyleCombobox.DataContext = this.settings;
            this.fpsPanel.DataContext = this.settings;
            if(this.manager != null)
            {
                this.manager.MovedToForeground();
                this.manager.Emulator.RenderComponent.FrameRateChanged += this.UpdateFPS;
            }

            if (pausedWhenLeavingPage)
            {
                pausedWhenLeavingPage = false;
                this.manager.Unpause();
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (this.customizationBarState > 0)
            {
                this.commitLayout();
            }

            this.manualSnapshotButton.DataContext = null;
            this.customizeVPadButton.DataContext = null;
            this.controllerScaleSlider.DataContext = null;
            this.controllerOpacitySlider.DataContext = null;
            this.controllerStyleCombobox.DataContext = null;
            this.fpsPanel.DataContext = null;
            SetSaveSlotContext(null);
            this.UnregisterEvents(App.Current as App);

            if (this.manager != null)
            {
                this.manager.MovedToBackground();
                this.manager.Emulator.RenderComponent.FrameRateChanged -= this.UpdateFPS;
            }

            pausedWhenLeavingPage = this.manager.Pause();

            base.OnNavigatedFrom(e);
        }

        private void UpdateFPS(float fps)
        {
            if(this.settings.ShowFPS)
            {
                this.fpsField.Text = (((int)(fps * 10.0f)) / 10.0f).ToString();
            }
        }

        private void SetSaveSlotContext(object context)
        {
            foreach (var cmd in this.cmdBar.SecondaryCommands)
            {
                var btn = (cmd as AppBarButton);
                if (btn != null && btn.CommandParameter != null)
                {
                    (cmd as AppBarButton).DataContext = context;
                }
            }
        }

        private void RegisterEvents(App app)
        {
            if (!this.eventsRegistered && app != null && app.MainPage != null)
            {
                this.eventsRegistered = true;
                app.MainPage.PaneOpening += this.paneOpenHandler;
                app.MainPage.KeyDown += this.keyDownHandler;
                app.MainPage.BackgroundPane.PointerPressed += this.pointerHandler;
                app.MainPage.BackgroundPane.PointerMoved += this.pointerMoveHandler;
                Window.Current.CoreWindow.PointerReleased += this.pointerReleaseHandler;
            }

        }

        private void UnregisterEvents(App app)
        {
            if (this.eventsRegistered && app != null && app.MainPage != null)
            {
                this.eventsRegistered = false;
                app.MainPage.PaneOpening -= this.paneOpenHandler;
                app.MainPage.KeyDown -= this.keyDownHandler;
                app.MainPage.BackgroundPane.PointerPressed -= this.pointerHandler;
                app.MainPage.BackgroundPane.PointerMoved -= this.pointerMoveHandler;
                Window.Current.CoreWindow.PointerReleased -= this.pointerReleaseHandler;
            }
        }

        private void handleKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if(args.Key != args.OriginalKey)
            {
                args.Handled = true;
                return;
            }
            if (args.Key == Windows.System.VirtualKey.Escape)
            {
                this.ToggleCmdBar();
            }
        }

        private void handlePaneOpening()
        {
            this.MoveCmdBarDown();
        }

        private void handlePointerPressed(object sender, PointerRoutedEventArgs e)
        {
            bool handled = false;
            var ptrPt = e.GetCurrentPoint(sender as UIElement);
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                if (ptrPt.Properties.IsRightButtonPressed)
                {
                    this.ToggleCmdBar();
                    handled = true;
                }
            }
            if (!handled)
            {
                this.MoveCmdBarDown();
            }

            this.manager.PointerPressed(ptrPt);
        }

        private void handlePointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var ptrPt = e.GetCurrentPoint(sender as UIElement);
            this.manager.PointerMoved(ptrPt);
        }

        private void handlePointerReleased(CoreWindow window, PointerEventArgs e)
        {
            this.manager.PointerReleased(e.CurrentPoint);
        }

        private void barButton(object sender, RoutedEventArgs e)
        {
            this.MoveCmdBarUp();
        }

        private void MoveBarDown_Completed(object sender, object e)
        {
            this.cmdBarUp = false;
            this.cmdBarMoving = false;
        }

        private void MoveBarUp_Completed(object sender, object e)
        {
            this.cmdBarUp = true;
            this.cmdBarMoving = false;
        }

        private void MoveCmdBarUp()
        {
            if (!this.cmdBarMoving && !this.cmdBarUp)
            {
                this.moveBarUp.Begin();
                this.cmdBarMoving = true;

                if(this.customizationBarState > 0)
                {
                    this.commitLayout();
                }
            }
        }

        private void MoveCmdBarDown()
        {
            if (!this.cmdBarMoving && this.cmdBarUp)
            {
                this.moveBarDown.Begin();
                this.cmdBarMoving = true;
            }
        }

        private void MoveCustomizationBarUp(int where)
        {
            if (this.customizationBarState == this.customizationBarMoveState)
            {
                if(this.customizationBarState == 2)
                {
                    if (where == 1)
                    {
                        this.moveLayoutBarHalfUp.Begin();
                        this.customizationBarMoveState = where;
                    }else if(where == 0)
                    {
                        this.moveLayoutBarCompletelyUp.Begin();
                        this.customizationBarMoveState = where;
                    }
                }else if(this.customizationBarState == 1 && where == 0)
                {
                    this.moveLayoutBarUp.Begin();
                    this.customizationBarMoveState = where;
                }
                this.NotifyPropertyChanged("IsCustomizationMenuVisible");
            }
        }

        private void MoveCustomizationBarDown(int where)
        {
            if (this.customizationBarState == this.customizationBarMoveState)
            {
                if (this.customizationBarState == 0)
                {
                    if(where == 1)
                    {
                        this.moveLayoutBarDown.Begin();
                        this.customizationBarMoveState = where;
                    }else if(where == 2)
                    {
                        this.moveLayoutBarCompletelyDown.Begin();
                        this.customizationBarMoveState = where;
                    }
                }
                else if (this.customizationBarState == 1 && where == 2)
                {
                    this.moveLayoutBarFurtherDown.Begin();
                    this.customizationBarMoveState = where;
                }
                this.NotifyPropertyChanged("IsCustomizationMenuVisible");
            }
        }

        private void MoveLayoutBarUp_Completed(object sender, object e)
        {
            this.customizationBarState = 0;
            this.NotifyPropertyChanged("IsCustomizationMenuVisible");
        }

        private void MoveLayoutBarDown_Completed(object sender, object e)
        {
            this.customizationBarState = 1;
        }

        private void MoveLayoutBarCompletelyUp_Completed(object sender, object e)
        {
            this.customizationBarState = 0;
            this.NotifyPropertyChanged("IsCustomizationMenuVisible");
        }

        private void MoveLayoutBarHalfUp_Completed(object sender, object e)
        {
            this.customizationBarState = 1;
        }

        private void MoveLayoutBarFurtherDown_Completed(object sender, object e)
        {
            this.customizationBarState = 2;
        }

        private void MoveLayoutBarCompletelyDown_Completed(object sender, object e)
        {
            this.customizationBarState = 2;
        }

        private void ToggleCmdBar()
        {
            if (this.cmdBarUp)
            {
                this.MoveCmdBarDown();
            }
            else
            {
                this.MoveCmdBarUp();
            }
        }

        private void ToggleCustomizationBar()
        {
            if (this.customizationBarState > 0)
            {
                this.MoveCustomizationBarUp(0);
            }
            else
            {
                this.MoveCustomizationBarDown(1);
            }
        }

        private void selectStateButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (sender as AppBarButton);
            if (button == null)
            {
                return;
            }

            int slot = 0;
            if (!int.TryParse(button.CommandParameter.ToString(), out slot))
            {
                return;
            }

            this.manager.SaveSlot = slot;
        }

        private async void saveStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.manager.ROMLoaded)
            {
                return;
            }
            if (this.settings.SaveConfirmation)
            {
                var result = await this.msgService.ShowConfirmDialog(
                    this.resources.GetString("saveConfirmTitle"),
                    this.resources.GetString("saveConfirmMessage"));
                if (result == ConfirmDialogResult.YesDontAsk || result == ConfirmDialogResult.NoDontAsk)
                {
                    this.settings.SaveConfirmation = false;
                }
                if (result == ConfirmDialogResult.YesDontAsk || result == ConfirmDialogResult.Yes)
                {
                    await this.manager.SaveState();
                }
            }
            else
            {
                await this.manager.SaveState();
            }
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            this.manager.TogglePause();
        }

        private async void loadStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.manager.ROMLoaded)
            {
                return;
            }
            if (this.settings.LoadConfirmation)
            {
                var result = await this.msgService.ShowConfirmDialog(
                    this.resources.GetString("loadConfirmTitle"),
                    this.resources.GetString("loadConfirmMessage"));
                if (result == ConfirmDialogResult.YesDontAsk || result == ConfirmDialogResult.NoDontAsk)
                {
                    this.settings.LoadConfirmation = false;
                }
                if (result == ConfirmDialogResult.YesDontAsk || result == ConfirmDialogResult.Yes)
                {
                    await this.manager.LoadState();
                }
            }
            else
            {
                await this.manager.LoadState();
            }
        }

        private async void resetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.manager.ROMLoaded)
            {
                return;
            }
            if (this.settings.ResetConfirmation)
            {
                var result = await this.msgService.ShowConfirmDialog(
                    this.resources.GetString("resetConfirmTitle"),
                    this.resources.GetString("resetConfirmMessage"));
                if (result == ConfirmDialogResult.YesDontAsk || result == ConfirmDialogResult.NoDontAsk)
                {
                    this.settings.ResetConfirmation = false;
                }
                if (result == ConfirmDialogResult.YesDontAsk || result == ConfirmDialogResult.Yes)
                {
                    await this.manager.ResetROMAsync();
                }
            }
            else
            {
                await this.manager.ResetROMAsync();
            }
        }

        private async void manualSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await this.manager.SaveSnapshotAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
            }
        }

        private async void screenshotButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await this.manager.SaveScreenshotAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
            }
        }

        private void customizeVPadButton_Click(object sender, RoutedEventArgs e)
        {
            this.startOpacity = this.controllerOpacitySlider.Value;
            this.startScale = this.controllerScaleSlider.Value;
            this.startStyleIndex = this.controllerStyleCombobox.SelectedIndex;

            this.manager.Emulator.TouchHandler.StartCustomizing();
            this.MoveCmdBarDown();
            this.MoveCustomizationBarDown(2);
        }

        private void commitLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            commitLayout();
        }

        private void commitLayout()
        {
            this.manager.Emulator.TouchHandler.CommitCustomizing();
            this.MoveCustomizationBarUp(0);
        }

        private void cancelLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            this.controllerOpacitySlider.Value = this.startOpacity;
            this.controllerScaleSlider.Value = this.startScale;
            this.controllerStyleCombobox.SelectedIndex = this.startStyleIndex;

            this.manager.Emulator.TouchHandler.CancelCustomizing();
            this.MoveCustomizationBarUp(0);
        }

        private void resetLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            this.controllerOpacitySlider.Value = this.startOpacity;
            this.controllerScaleSlider.Value = this.startScale;
            this.controllerStyleCombobox.SelectedIndex = this.startStyleIndex;

            this.manager.Emulator.TouchHandler.ResetCustomization();
        }

        private void extendCustomPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.customizationBarState == 2)
            {
                this.MoveCustomizationBarUp(1);
            }
            else if (this.customizationBarState == 1)
            {
                this.MoveCustomizationBarDown(2);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
