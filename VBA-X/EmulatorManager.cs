using EmulatorComponent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Utility;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace VBA_X
{
    public class EmulatorManager : INotifyPropertyChanged
    {
        #region Singleton
        private static EmulatorManager instance = null;

        public static EmulatorManager Current
        {
            get
            {
                //if (instance == null)
                //{
                //    throw new InvalidOperationException("EmulatorManager not initialized.");
                //}
                return instance;
            }
        }

        public async static Task<EmulatorManager> InitializeAsync(Window window, IMainView mainView, Settings settings)
        {
            instance = new EmulatorManager();
            await instance.InitAsync(window, mainView, settings);
            return instance;
        }
        #endregion

        private StorageManager storage;
        private Window window;
        private IMainView mainView;
        private Settings settings;
        private EmulatorComponent.EmulatorComponent emulator;
        private ROMEntry currentROM;
        private UWPMessageService messageService;
        private ResourceLoader resources;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public EmulatorComponent.EmulatorComponent Emulator
        {
            get
            {
                return emulator;
            }
        }

        public int SaveSlot
        {
            get
            {
                if (this.currentROM == null)
                {
                    return 0;
                }
                return currentROM.DatabaseEntry.SaveSlot;
            }

            set
            {
                if (this.currentROM != null && value != this.SaveSlot)
                {
                    currentROM.DatabaseEntry.SaveSlot = value;
                }
            }
        }

        public bool ROMLoaded
        {
            get
            {
                return this.emulator.ROMLoaded;
            }
        }

        private EmulatorManager()
        {
        }

        private async Task InitAsync(Window window, IMainView mainView, Settings settings)
        {
            this.window = window;
            this.settings = settings;
            this.mainView = mainView;
            this.messageService = new UWPMessageService(mainView.Dispatcher);
            this.resources = new ResourceLoader();

            UWPServiceProvider serviceProvider = new UWPServiceProvider(this.mainView.Dispatcher);

            this.emulator = new EmulatorComponent.EmulatorComponent(
                    this.settings,
                    this.mainView.SwapChainPanel,
                    this.mainView.Dispatcher,
                    PlatformProperties.DeviceType
                );

            await StorageManager.InitializeAsync(
                    serviceProvider,
                    new SaveInfoWrapper(this.emulator.SaveInfo)
                );

            this.emulator.SaveProvider = new SaveProviderWrapper();

            this.storage = StorageManager.Current;

            await this.emulator.LoadConfigAsync(
                await storage.ReadFileToStringAsync(
                    await storage.GetAssetFileAsync("Config/vba-over.ini")
                ));

            KeyboardInputChannel keyboard = new KeyboardInputChannel(new CoreWindowKeyProvider(this.window.CoreWindow), this.settings);
            KeyInputWrapper keyboardWrapper = new KeyInputWrapper(keyboard, this.window.CoreWindow);
            this.emulator.GameController.AddInputChannel(keyboardWrapper);

            this.window.VisibilityChanged += Window_VisibilityChanged;

            if (this.settings.FirstLaunch)
            {
                await this.storage.CopyDemoROM("Demos/Bunny Advance (Demo).gba");
                await this.storage.CopyDemoROM("Demos/Pong.gb");
                this.settings.FirstLaunch = false;
            }

            HIDManager.Current.ServiceProvider = serviceProvider;

            this.emulator.GameController.SetHIDChannel(
                    HIDInputWrapper.FromChannel(await HIDManager.Current.ReconnectAsync())
                );
        }

        public void PointerPressed(PointerPoint point)
        {
            if (this.mainView.IsOnGameView && this.settings.ShowVirtualController)
            {
                this.emulator.TouchHandler.PointerPressed(point);
            }
        }

        public void PointerMoved(PointerPoint point)
        {
            if (this.mainView.IsOnGameView && this.settings.ShowVirtualController)
            {
                this.emulator.TouchHandler.PointerMoved(point);
            }
        }

        public void PointerReleased(PointerPoint point)
        {
            this.emulator.TouchHandler.PointerReleased(point);
        }

        private void Window_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (this.emulator == null)
            {
                return;
            }
            if (e.Visible)
            {
                this.emulator.Maximized(this.mainView.IsOnGameView);
            }
            else
            {
                this.emulator.Minimized();
            }
        }

        public async Task SuspendAsync()
        {
            if(this.emulator != null)
            {
                await this.emulator.SaveSRAMAsync();
                await this.SaveAutoState();
                if (!this.settings.ManualSnapshots)
                {
                    await this.SaveSnapshotAsync();
                }
                this.emulator.Suspend();
            }
            HIDManager.Current.DisconnectChannel();
        }

        public void MovedToBackground()
        {
            this.emulator.RenderComponent.Blur = true;
        }

        public void MovedToForeground()
        {
            this.emulator.RenderComponent.Blur = false;
        }

        public async Task Resume()
        {
            this.emulator.GameController.SetHIDChannel(
                    HIDInputWrapper.FromChannel(await HIDManager.Current.ReconnectAsync())
                );
            this.emulator.Resume(this.mainView.IsOnGameView);
        }

        private async Task saveState(int slot)
        {
            if (!this.ROMLoaded)
            {
                return;
            }

            ByteWrapper bytes = this.emulator.GetSaveStateData();
            if (bytes != null)
            {
                SaveStateData stateData = new SaveStateData()
                {
                    Data = bytes.AsArray(),
                    Slot = slot
                };
                await this.storage.SaveStateDataAsync(stateData);
            }
#if DEBUG
            else{
                System.Diagnostics.Debug.WriteLine("Unable to get save state data.");
            }
#endif
        }

        public async Task SaveState()
        {
            await this.saveState(this.SaveSlot);
        }

        public async Task SaveAutoState()
        {
            await this.saveState(StorageManager.AutosaveSlot);
        }

        public async Task LoadState()
        {
            if (!this.ROMLoaded)
            {
                return;
            }

            SaveStateData state = await this.storage.GetStateDataAsync(this.SaveSlot);
            if (state != null)
            {
                await this.emulator.LoadSaveStateAsync(new ByteWrapper(state.Data));
            }
#if DEBUG
            else
            {
                System.Diagnostics.Debug.WriteLine("No state loaded for slot " + this.SaveSlot);
            }
#endif
        }

        public async Task StopROMAsync()
        {
            if (this.ROMLoaded)
            {
                if (!this.settings.ManualSnapshots)
                {
                    await this.SaveSnapshotAsync();
                }
                await this.emulator.StopROMAsync();
            }
        }

        public async Task SaveSnapshotAsync()
        {
            if (this.emulator.ROMLoaded)
            {
                int rowPitch = 0;
                ByteWrapper bytes = this.emulator.GetSnapshot(out rowPitch);
                await this.storage.SaveSnapshotAsync(new SnapshotData()
                {
                    Data = bytes.AsArray(),
                    Pitch = rowPitch
                });
            }
        }

        public async Task SaveScreenshotAsync()
        {
            if (this.emulator.ROMLoaded)
            {
                int rowPitch = 0;
                ByteWrapper bytes = this.emulator.GetSnapshot(out rowPitch);
                await this.storage.SaveScreenshotAsync(new SnapshotData()
                {
                    Data = bytes.AsArray(),
                    Pitch = rowPitch
                });
            }
        }

        public void TogglePause()
        {
            if (this.emulator.IsPaused)
            {
                this.emulator.Unpause();
            }
            else
            {
                this.emulator.Pause();
            }
        }

        public bool Pause()
        {
            if (this.ROMLoaded)
            {
                return this.emulator.Pause();
            }
            return false;
        }

        public bool Unpause()
        {
            if (this.ROMLoaded)
            {
                return this.emulator.Unpause();
            }
            return false;
        }

        internal async Task StartROM(ROMEntry rom)
        {
            this.RemoveOldEventHandlers();
            this.currentROM = rom;

            UpdateDBEntry(rom);

            this.mainView.ShowEmulatorPage();

            IROMData data = await StartNewROM();

            if (data != null)
            {
                await LoadCheats();
            }
        }

        private async Task LoadCheats()
        {
            try
            {
                IList<Utility.CheatData> cheatData = await this.storage.LoadCheatDataAsync();
                if (cheatData != null)
                {
                    this.emulator.CheatManager.ApplyCheats(cheatData.ConvertCheatData());
                }
#if DEBUG
                else
                {
                    System.Diagnostics.Debug.WriteLine("No cheat data for ROM " + this.currentROM.Name);
                }
#endif
            }
            catch (Exception)
            {
                await this.messageService.ShowMessage(this.resources.GetString("loadCheatDataError"), this.resources.GetString("errorCaption"));
            }
        }

        private async Task<IROMData> StartNewROM()
        {
            await this.StopROMAsync();
            IROMData data = null;
            try
            {
                data = ROMData.FromUtility(await StorageManager.Current.LoadROMAsync(this.currentROM));
                await this.emulator.LoadROMAsync(data);
            }
            catch (IOException)
            {
                this.currentROM = null;
                data = null;
                await this.messageService.ShowMessage(this.resources.GetString("invalidROMData"), this.resources.GetString("errorCaption"));
            }

            return data;
        }

        private void UpdateDBEntry(ROMEntry rom)
        {
            ROMDatabaseEntry dbEntry = rom.DatabaseEntry;
            dbEntry.PropertyChanged += Rom_PropertyChanged;
            dbEntry.LastPlayed = DateTime.Now.ToBinary();
        }

        private void RemoveOldEventHandlers()
        {
            if (this.currentROM != null)
            {
                try
                {
                    this.currentROM.DatabaseEntry.PropertyChanged -= Rom_PropertyChanged;
                }
                catch (Exception) {  }
            }
        }

        internal async Task ResetROMAsync()
        {
            if (this.currentROM == null)
            {
                return;
            }
            await this.StartROM(this.currentROM);
        }

        private void Rom_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SaveSlot")
            {
                this.NotifyPropertyChanged("SaveSlot");
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
