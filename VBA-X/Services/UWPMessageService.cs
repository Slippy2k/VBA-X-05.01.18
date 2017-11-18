using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VBA_X
{
    public enum ConfirmDialogResult
    {
        Yes,
        YesDontAsk,
        No,
        NoDontAsk
    }

    public class UWPMessageService
        : IMessageService
    {
        private CoreDispatcher dispatcher;
        private ResourceLoader resources;

        public CoreDispatcher Dispatcher
        {
            get
            {
                return dispatcher;
            }
        }

        public UWPMessageService(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.resources = new ResourceLoader();
        }

        public IAsyncAction ShowMessage(string message, string caption)
        {
            return this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
            {
                await new MessageDialog(message, caption).ShowAsync();
            }));
        }

        public IAsyncOperation<int> ShowOptionDialog(string message, params string[] options)
        {
            if (options.Length == 0)
            {
                throw new InvalidOperationException("There must be at least one option");
            }
            Func<Task<int>> helper = async () => {
                int result = -1;
                MessageDialog dialog = new MessageDialog(message);
                for (int i = 0; i < options.Length && i < 2; i++)
                {
                    int value = i;
                    dialog.Commands.Add(new UICommand(options[i], (c) =>
                    {
                        result = value;
                    }));
                }
                await dialog.ShowAsync();
                return result;
            };
            return helper().AsAsyncOperation<int>();
        }

        public IAsyncOperation<ConfirmDialogResult> ShowConfirmDialog(string title, string message)
        {
            Func<Task<ConfirmDialogResult>> helper = async () => {
                ConfirmDialogResult result = ConfirmDialogResult.No;

                ContentDialog dialog = new ContentDialog()
                {
                    MaxWidth = (App.Current as App).MainPage.ActualWidth,
                    Title = title
                };
                var panel = new StackPanel();
                panel.Orientation = Orientation.Vertical;

                panel.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap,
                    Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
                });
                var checkBox = new CheckBox()
                {
                    Content = resources.GetString("confirmDialogDontAsk"),
                    IsChecked = false
                };
                panel.Children.Add(checkBox);
                dialog.Content = panel;

                dialog.PrimaryButtonText = this.resources.GetString("confirmDialogYes");
                dialog.IsPrimaryButtonEnabled = true;
                dialog.PrimaryButtonClick += delegate
                {
                    if (checkBox.IsChecked != null && checkBox.IsChecked.Value)
                    {
                        result = ConfirmDialogResult.YesDontAsk;
                    }
                    else
                    {
                        result = ConfirmDialogResult.Yes;
                    }
                };

                dialog.SecondaryButtonText = this.resources.GetString("confirmDialogNo");
                dialog.IsSecondaryButtonEnabled = true;
                dialog.SecondaryButtonClick += delegate
                {
                    if (checkBox.IsChecked != null && checkBox.IsChecked.Value)
                    {
                        result = ConfirmDialogResult.NoDontAsk;
                    }
                    else
                    {
                        result = ConfirmDialogResult.No;
                    }
                };

                await dialog.ShowAsync();

                return result;
            };
            return helper().AsAsyncOperation<ConfirmDialogResult>();
        }
    }
}
