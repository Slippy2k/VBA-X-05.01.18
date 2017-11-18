#include "pch.h"
#include "IInputChannel.h"

namespace EmulatorComponent
{
	ControllerState::ControllerState()
	{

	}

	ControllerState::~ControllerState()
	{

	}

	ButtonStates ControllerState::Buttons::get()
	{
		return this->buttons;
	}

	void ControllerState::Buttons::set(ButtonStates value)
	{
		this->buttons = value;
	}

	void ControllerState::Copy(ControllerState ^state)
	{
		state->buttons = this->buttons;
	}

	void ControllerState::Reset()
	{
		this->buttons = ButtonStates();
	}
}