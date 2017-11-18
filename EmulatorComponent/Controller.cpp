#include "pch.h"
#include "Controller.h"

namespace EmulatorComponent
{
	Controller::Controller(bool xboxSupport)
		: currentHIDChannel(nullptr),
		holdTurbo(false), turboPressedUpdates(0)
	{
		this->currentState = ref new ControllerState();

		if (xboxSupport)
		{
			CXBOXController ^xBoxController = ref new CXBOXController(0);
			this->inputChannels.push_back(xBoxController);
		}
	}

	Controller::~Controller()
	{
		this->inputChannels.clear();
	}

	ControllerState ^Controller::GetControllerState() const
	{
		return this->currentState;
	}

	void Controller::AddInputChannel(IInputChannel ^inputChannel)
	{
		this->inputChannels.push_back(inputChannel);
	}

	void Controller::SetHIDChannel(IInputChannel ^inputChannel)
	{
		if (inputChannel == this->currentHIDChannel)
		{
			return;
		}
		if (this->currentHIDChannel != nullptr)
		{
			for (int i = 0; i < this->inputChannels.size(); i++)
			{
				if (this->inputChannels.at(i) == this->currentHIDChannel)
				{
					this->inputChannels.erase(this->inputChannels.begin() + i);
					this->currentHIDChannel = nullptr;
					break;
				}
			}
		}

		if (inputChannel != nullptr)
		{
			this->currentHIDChannel = inputChannel;
			this->inputChannels.push_back(inputChannel);
		}
	}

	void Controller::Update()
	{
		//this->currentState->Reset();

		ButtonStates tmpState;

		for (int i = 0; i < this->inputChannels.size(); i++)
		{
			IInputChannel ^channel = this->inputChannels.at(i);
			if (channel->IsConnected())
			{
				channel->Update();

				ControllerState ^state = channel->GetCurrentState();

				tmpState.APressed = tmpState.APressed || state->buttons.APressed;
				tmpState.BPressed = tmpState.BPressed || state->buttons.BPressed;
				tmpState.LPressed = tmpState.LPressed || state->buttons.LPressed;
				tmpState.RPressed = tmpState.RPressed || state->buttons.RPressed;

				tmpState.SelectPressed = tmpState.SelectPressed || state->buttons.SelectPressed;
				tmpState.StartPressed = tmpState.StartPressed || state->buttons.StartPressed;

				tmpState.LeftPressed = tmpState.LeftPressed || state->buttons.LeftPressed;
				tmpState.RightPressed = tmpState.RightPressed || state->buttons.RightPressed;
				tmpState.UpPressed = tmpState.UpPressed || state->buttons.UpPressed;
				tmpState.DownPressed = tmpState.DownPressed || state->buttons.DownPressed;

				tmpState.TurboPressed = tmpState.TurboPressed || state->buttons.TurboPressed;
			}
		}

		if (tmpState.TurboPressed)
		{
			this->turboPressedUpdates++;
		}
		else if (this->currentState->buttons.TurboPressed && this->turboPressedUpdates > 0)
		{
			if (this->holdTurbo)
			{
				this->holdTurbo = false;
			}
			else {
				this->holdTurbo = (this->turboPressedUpdates < 20);
			}

			this->turboPressedUpdates = 0;
		}
		else {
			this->turboPressedUpdates = 0;
		}

		tmpState.TurboPressed = tmpState.TurboPressed || this->holdTurbo;

		this->currentState->buttons = tmpState;
	}
}