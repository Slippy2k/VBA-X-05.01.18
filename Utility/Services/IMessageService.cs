using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Utility
{
    public interface IMessageService
    {
        CoreDispatcher Dispatcher { get; }

        IAsyncAction ShowMessage(String message, String caption);

        IAsyncOperation<int> ShowOptionDialog(String message, params String[] options);
    }
}
