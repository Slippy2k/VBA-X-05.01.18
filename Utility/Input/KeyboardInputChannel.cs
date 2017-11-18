using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;

namespace Utility
{
    public sealed class KeyboardInputChannel
    {
        private IKeyProvider provider;
        private IKeySettings settings;
        private ControllerState state;

        public KeyboardInputChannel(IKeyProvider keyProvider, IKeySettings settings)
        {
            this.provider = keyProvider;
            this.settings = settings;
            this.state = new ControllerState();

            //this.provider.KeyDown += Provider_KeyDown;
            //this.provider.KeyUp += Provider_KeyUp;
        }

        private void Provider_KeyUp(object sender, KeyEventArgs args)
        {
            if(this.OnKeyUp(args.VirtualKey))
            {
                args.Handled = true;
            }
        }

        private void Provider_KeyDown(object sender, KeyEventArgs args)
        {
            if (this.OnKeyDown(args.VirtualKey))
            {
                args.Handled = true;
            }
        }

        public bool OnKeyDown(VirtualKey key)
        {
            if(this.settings.LeftBinding == key)
            {
                return this.state.buttons.LeftPressed = true;
            }
            if (this.settings.UpBinding == key)
            {
                return this.state.buttons.UpPressed = true;
            }
            if (this.settings.RightBinding == key)
            {
                return this.state.buttons.RightPressed = true;
            }
            if (this.settings.DownBinding == key)
            {
                return this.state.buttons.DownPressed = true;
            }
            if (this.settings.StartBinding == key)
            {
                return this.state.buttons.StartPressed = true;
            }
            if (this.settings.SelectBinding == key)
            {
                return this.state.buttons.SelectPressed = true;
            }
            if (this.settings.ABinding == key)
            {
                return this.state.buttons.APressed = true;
            }
            if (this.settings.BBinding == key)
            {
                return this.state.buttons.BPressed = true;
            }
            if (this.settings.LBinding == key)
            {
                return this.state.buttons.LPressed = true;
            }
            if (this.settings.RBinding == key)
            {
                return this.state.buttons.RPressed = true;
            }
            if (this.settings.TurboBinding == key)
            {
                return this.state.buttons.TurboPressed = true;
            }
            return false;
        }

        public bool OnKeyUp(VirtualKey key)
        {
            System.Diagnostics.Debug.WriteLine("ONKEYUP");
            if (this.settings.LeftBinding == key)
            {
                return !(this.state.buttons.LeftPressed = false);
            }
            if (this.settings.UpBinding == key)
            {
                return !(this.state.buttons.UpPressed = false);
            }
            if (this.settings.RightBinding == key)
            {
                return !(this.state.buttons.RightPressed = false);
            }
            if (this.settings.DownBinding == key)
            {
                return !(this.state.buttons.DownPressed = false);
            }
            if (this.settings.StartBinding == key)
            {
                return !(this.state.buttons.StartPressed = false);
            }
            if (this.settings.SelectBinding == key)
            {
                return !(this.state.buttons.SelectPressed = false);
            }
            if (this.settings.ABinding == key)
            {
                return !(this.state.buttons.APressed = false);
            }
            if (this.settings.BBinding == key)
            {
                return !(this.state.buttons.BPressed = false);
            }
            if (this.settings.LBinding == key)
            {
                return !(this.state.buttons.LPressed = false);
            }
            if (this.settings.RBinding == key)
            {
                return !(this.state.buttons.RPressed = false);
            }
            if (this.settings.TurboBinding == key)
            {
                return !(this.state.buttons.TurboPressed = false);
            }
            return false;
        }

        public ControllerState GetCurrentState()
        {
            return this.state;
        }

        public bool IsConnected()
        {
            return true;
        }

        public void Update()
        {
            ButtonStates tmp = new ButtonStates();

            tmp.LeftPressed = (this.provider.GetKeyState(this.settings.LeftBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.UpPressed = (this.provider.GetKeyState(this.settings.UpBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.RightPressed = (this.provider.GetKeyState(this.settings.RightBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.DownPressed = (this.provider.GetKeyState(this.settings.DownBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.StartPressed = (this.provider.GetKeyState(this.settings.StartBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.SelectPressed = (this.provider.GetKeyState(this.settings.SelectBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.APressed = (this.provider.GetKeyState(this.settings.ABinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.BPressed = (this.provider.GetKeyState(this.settings.BBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.LPressed = (this.provider.GetKeyState(this.settings.LBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.RPressed = (this.provider.GetKeyState(this.settings.RBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            tmp.TurboPressed = (this.provider.GetKeyState(this.settings.TurboBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

            this.state.buttons = tmp;

            ////this.state.Reset();
            //this.state.buttons.LeftPressed = (bool)(this.provider.GetKeyState(this.settings.LeftBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.RightPressed = (bool)(this.provider.GetKeyState(this.settings.RightBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.UpPressed = (bool)(this.provider.GetKeyState(this.settings.UpBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.DownPressed = (bool)(this.provider.GetKeyState(this.settings.DownBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);

            //this.state.buttons.StartPressed = (bool)(this.provider.GetKeyState(this.settings.StartBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.SelectPressed = (bool)(this.provider.GetKeyState(this.settings.SelectBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);

            //this.state.buttons.APressed = (bool)(this.provider.GetKeyState(this.settings.ABinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.BPressed = (bool)(this.provider.GetKeyState(this.settings.BBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.LPressed = (bool)(this.provider.GetKeyState(this.settings.LBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);
            //this.state.buttons.RPressed = (bool)(this.provider.GetKeyState(this.settings.RBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);

            //this.state.buttons.TurboPressed = (bool)(this.provider.GetKeyState(this.settings.TurboBinding) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;);

        }
    }
}
