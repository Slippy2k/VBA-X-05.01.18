using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorComponent;
using Utility;

namespace VBA_X
{
    public class HIDInputWrapper : EmulatorComponent.IInputChannel
    {
        public static HIDInputWrapper FromChannel(HIDInputChannel channel)
        {
            if(channel == null)
            {
                return null;
            }
            return new HIDInputWrapper(channel);
        }

        private HIDInputChannel channel;
        private EmulatorComponent.ControllerState state;

        private HIDInputWrapper(HIDInputChannel channel)
        {
            this.channel = channel;
            this.state = new EmulatorComponent.ControllerState();
        }

        public void DisposeController()
        {
            if(this.channel != null)
            {
                this.channel.Dispose();
            }
        }

        public EmulatorComponent.ControllerState GetCurrentState()
        {
            return this.state;
        }

        public bool IsConnected()
        {
            return this.channel.IsConnected();
        }

        public void Update()
        {
            EmulatorComponent.ButtonStates buttons = new EmulatorComponent.ButtonStates();
            
            this.channel.Update();

            var channelState = this.channel.GetCurrentState();

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
