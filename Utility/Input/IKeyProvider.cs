using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Utility
{
    public interface IKeyProvider
    {
        CoreVirtualKeyStates GetKeyState(VirtualKey vkey);

        event TypedEventHandler<Object, KeyEventArgs> KeyDown;
        event TypedEventHandler<Object, KeyEventArgs> KeyUp;
    }
}
