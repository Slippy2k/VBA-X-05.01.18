using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace VBA_X
{
    public class CoreWindowKeyProvider : Utility.IKeyProvider
    {
        private CoreWindow window;
        private Dictionary<EventRegistrationToken, TypedEventHandler<CoreWindow, KeyEventArgs>> upHandlers;
        private EventRegistrationTokenTable<TypedEventHandler<CoreWindow, KeyEventArgs>> upTokenTable;
        private Dictionary<EventRegistrationToken, TypedEventHandler<CoreWindow, KeyEventArgs>> downHandlers;
        private EventRegistrationTokenTable<TypedEventHandler<CoreWindow, KeyEventArgs>> downTokenTable;

        public CoreWindowKeyProvider(CoreWindow window)
        {
            this.window = window;
            this.upHandlers = new Dictionary<EventRegistrationToken, TypedEventHandler<CoreWindow, KeyEventArgs>>();
            this.upTokenTable = new EventRegistrationTokenTable<TypedEventHandler<CoreWindow, KeyEventArgs>>();
            this.downHandlers = new Dictionary<EventRegistrationToken, TypedEventHandler<CoreWindow, KeyEventArgs>>();
            this.downTokenTable = new EventRegistrationTokenTable<TypedEventHandler<CoreWindow, KeyEventArgs>>();
        }

        public event TypedEventHandler<Object, KeyEventArgs> KeyDown
        {
            add
            {
                var handler = new TypedEventHandler<CoreWindow, KeyEventArgs>((w, e) =>
                {
                    value(w, e);
                });

                var token = this.downTokenTable.AddEventHandler(handler);
                this.downHandlers.Add(token, handler);

                this.window.KeyDown += handler;

                return token;
            }
            remove
            {
                TypedEventHandler<CoreWindow, KeyEventArgs> handler;
                if (this.downHandlers.TryGetValue(value, out handler))
                {
                    this.downHandlers.Remove(value);
                    this.window.KeyDown -= handler;
                }
                this.downTokenTable.RemoveEventHandler(value);
            }
        }

        public event TypedEventHandler<Object, KeyEventArgs> KeyUp
        {
            add
            {
                var handler = new TypedEventHandler<CoreWindow, KeyEventArgs>((w, e) =>
                {
                    value(w, e);
                });

                var token = this.upTokenTable.AddEventHandler(handler);
                this.upHandlers.Add(token, handler);

                this.window.KeyUp += handler;

                return token;
            }
            remove
            {
                TypedEventHandler<CoreWindow, KeyEventArgs> handler;
                if (this.upHandlers.TryGetValue(value, out handler))
                {
                    this.upHandlers.Remove(value);
                    this.window.KeyUp -= handler;
                }
                this.upTokenTable.RemoveEventHandler(value);
            }
        }

        public CoreVirtualKeyStates GetKeyState(VirtualKey vkey)
        {
            return this.window.GetAsyncKeyState(vkey);
        }
    }
}
