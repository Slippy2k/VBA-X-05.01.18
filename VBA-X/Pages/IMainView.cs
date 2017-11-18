using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace VBA_X
{
    public interface IMainView
    {
        CoreDispatcher Dispatcher { get; }

        bool IsOnGameView { get; }

        SwapChainPanel SwapChainPanel { get; }

        void ShowEmulatorPage();

        void NavigateTo(Type page);
        void NavigateBack();
    }
}
