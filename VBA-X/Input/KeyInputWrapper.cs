using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorComponent;
using Utility;
using Windows.UI.Core;

namespace VBA_X
{
    public class KeyInputWrapper : EmulatorComponent.IInputChannel
    {
        private KeyboardInputChannel input;
        private EmulatorComponent.ControllerState state;
        private CoreWindow window;

        public KeyInputWrapper(KeyboardInputChannel input, CoreWindow window)
        {
            this.input = input;
            this.state = new EmulatorComponent.ControllerState();
            this.window = window;
        }

        public void DisposeController()
        {

        }

        public EmulatorComponent.ControllerState GetCurrentState()
        {
            return this.state;
        }

        public bool IsConnected()
        {
            return this.input.IsConnected();
        }

        public void Update()
        {
            this.input.Update();
            var channelState = this.input.GetCurrentState();

            EmulatorComponent.ButtonStates buttons = new EmulatorComponent.ButtonStates();

            buttons.APressed = channelState.Buttons.APressed;
            buttons.BPressed = channelState.Buttons.BPressed;
            buttons.LPressed = channelState.Buttons.LPressed;
            buttons.RPressed = channelState.Buttons.RPressed;
            buttons.StartPressed = channelState.Buttons.StartPressed;
            buttons.SelectPressed = channelState.Buttons.SelectPressed;
            buttons.LeftPressed = channelState.Buttons.LeftPressed;
            buttons.UpPressed = channelState.Buttons.UpPressed;
            buttons.RightPressed = channelState.Buttons.RightPressed;
            buttons.DownPressed = channelState.Buttons.DownPressed;
            buttons.TurboPressed = channelState.Buttons.TurboPressed;

            this.state.Buttons = buttons;
        }
    }
}
