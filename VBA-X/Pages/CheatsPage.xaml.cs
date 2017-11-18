using EmulatorComponent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Utility;
using Windows.ApplicationModel.Resources;
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
    public sealed partial class CheatsPage : Page
    {
        private class CheatDataWrapper : INotifyPropertyChanged
        {
            private Utility.CheatData data;
            private bool selected;

            public Utility.CheatData Data
            {
                get
                {
                    return this.data;
                }                
                set
                {
                    if(value != this.data)
                    {
                        this.data = value;
                        this.NotifyPropertyChanged();
                    }
                }
            }

            public bool Selected
            {
                get
                {
                    return selected;
                }

                set
                {
                    if(value != selected)
                    {
                        selected = value;
                        this.NotifyPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private EmulatorManager manager;
        private StorageManager storage;
        private UWPMessageService messageService;
        private ResourceLoader resources;
        private ICheatCodeValidator validator;
        private ObservableCollection<CheatDataWrapper> cheatData;

        public CheatsPage()
        {
            this.InitializeComponent();

            this.manager = EmulatorManager.Current;
            this.storage = StorageManager.Current;
            this.resources = new ResourceLoader();
            this.validator = this.manager.Emulator.CheatValidator;
            this.messageService = new UWPMessageService(this.Dispatcher);
            this.cheatData = new ObservableCollection<CheatDataWrapper>();
            this.cheatView.ItemsSource = this.cheatData;
        }

        private async Task InitializeCheatData()
        {
            if (!this.manager.ROMLoaded)
            {
                return;
            }

            try
            {
                var cheatData = await this.storage.LoadCheatDataAsync();
                if(this.cheatData != null)
                {
                    this.cheatData.Clear();
                    foreach (var cheat in cheatData)
                    {
                        this.cheatData.Add(new CheatDataWrapper()
                        {
                            Data = cheat,
                            Selected = false
                        });
                    }
                }
            }
            catch (Exception)
            {
                await this.messageService.ShowMessage(this.resources.GetString("loadCheatDataError"), this.resources.GetString("errorCaption"));
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.addGrid.DataContext = this.manager;
            this.manageContainer.DataContext = this.manager;
            this.noROMLoadedBox.DataContext = this.manager;

            await this.InitializeCheatData();

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.addGrid.DataContext = null;
            this.manageContainer.DataContext = null;
            this.noROMLoadedBox.DataContext = null;

            this.codeBox.Focus(FocusState.Programmatic);
            
            IList<Utility.CheatData> cheats = new List<Utility.CheatData>();
            foreach (var cheat in this.cheatData)
            {
                if(!this.validator.CheckCode(cheat.Data.CheatCode, null))
                {
                    cheat.Data.Enabled = false;
                }
                cheats.Add(cheat.Data);
            }
            try
            {
                await this.storage.StoreCheatDataAsync(cheats);
            }catch(IOException)
            {
                await this.messageService.ShowMessage(this.resources.GetString("cheatsWriteError"), this.resources.GetString("errorCaption"));
            }

            this.manager.Emulator.CheatManager.ApplyCheats(cheats.ConvertCheatData());

            base.OnNavigatedFrom(e);
        }

        private void scrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.wrappingPanel.Width = this.scrollViewer.ActualWidth - this.wrappingPanel.Margin.Left - this.wrappingPanel.Margin.Right;
        }

        private void cheatView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.cheatView.SelectedIndex >= 0)
            {
                foreach (var item in this.cheatData)
                {
                    item.Selected = false;
                }
                this.cheatData[this.cheatView.SelectedIndex].Selected = true;
            }
        }

        private void deleteCheatButton_Click(object sender, RoutedEventArgs e)
        {
            CheatDataWrapper data = (e.OriginalSource as Windows.UI.Xaml.Controls.Button).DataContext as CheatDataWrapper;
            this.cheatData.Remove(data);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheatDataWrapper data = (e.OriginalSource as Windows.UI.Xaml.Controls.CheckBox).DataContext as CheatDataWrapper;
            this.cheatView.SelectedItem = data;
        }

        private async void addCheatButton_Click(object sender, RoutedEventArgs e)
        {
            List<String> cleanedCodes = new List<string>();
            if(this.descriptionBox.Text.Trim() == string.Empty)
            {
                await this.messageService.ShowMessage(this.resources.GetString("cheatsEmptyDescriptionMessage"), this.resources.GetString("errorCaption"));
                return;
            }
            String codes = this.codeBox.Text.Trim();
            if (codes == string.Empty)
            {
                await this.messageService.ShowMessage(this.resources.GetString("cheatsEmptyCodeMessage"), this.resources.GetString("errorCaption"));
                return;
            }
            if(!this.validator.CheckCode(codes, cleanedCodes))
            {
                await this.messageService.ShowMessage(this.resources.GetString("cheatsInvalidCodeMessage"), this.resources.GetString("errorCaption"));
                return;
            }
            
            foreach (var code in cleanedCodes)
            {
                Utility.CheatData cheat = new Utility.CheatData()
                {
                    CheatCode = code.Trim(),
                    Description = this.descriptionBox.Text,
                    Enabled = true
                };
                this.cheatData.Add(new CheatDataWrapper()
                {
                    Data = cheat,
                    Selected = false
                });
            }

            this.descriptionBox.Text = string.Empty;
            this.codeBox.Text = string.Empty;
        }

        private async void editCodeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (e.OriginalSource as Windows.UI.Xaml.Controls.TextBox);
            var cheatCode = textBox.DataContext as CheatDataWrapper;
            string code = textBox.Text;
            if (!this.validator.CheckCode(code, null))
            {
                cheatCode.Data.Enabled = false;
                await this.messageService.ShowMessage(this.resources.GetString("cheatsInvalidCodeMessage"), this.resources.GetString("warningCaption"));
            }
        }

        private void editDescBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (sender as TextBox);
            if(textBox != null)
            {
                var cheatCode = textBox.DataContext as CheatDataWrapper;
                if(cheatCode != null && cheatCode.Data != null)
                {
                    cheatCode.Data.Description = textBox.Text;
                }
            }
        }

        private void editCodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (sender as TextBox);
            if (textBox != null)
            {
                var cheatCode = textBox.DataContext as CheatDataWrapper;
                if (cheatCode != null && cheatCode.Data != null)
                {
                    cheatCode.Data.CheatCode = textBox.Text;
                }
            }
        }
    }
}
