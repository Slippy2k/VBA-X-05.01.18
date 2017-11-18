#include "pch.h"
#include "CXBOXController.h"
#include "EmulatorComponent.h"

namespace EmulatorComponent
{
	CXBOXController::CXBOXController(int playerNumber)
		: controllerNumber(playerNumber)
	{ 
		this->currentState = ref new ControllerState();
	}

	CXBOXController::~CXBOXController()
	{ }

	bool CXBOXController::IsConnected(void)
	{
		XINPUT_STATE state;

		DWORD result = XInputGetState(this->controllerNumber, &state);

		return (result == ERROR_SUCCESS);
	}

	void CXBOXController::Vibrate(int leftVal, int rightVal)
	{
		XINPUT_VIBRATION vibration;
		ZeroMemory(&vibration, sizeof(XINPUT_VIBRATION));

		vibration.wLeftMotorSpeed = leftVal;
		vibration.wRightMotorSpeed = rightVal;

		XInputSetState(this->controllerNumber, &vibration);
	}


	ControllerState ^CXBOXController::GetCurrentState()
	{
		return this->currentState;
	}

	void CXBOXController::Update()
	{
		XINPUT_STATE state;

		ButtonStates tmpState;
		ZeroMemory(&state, sizeof(XINPUT_STATE));

		DWORD result = XInputGetState(this->controllerNumber, &state);
		if (result != ERROR_SUCCESS)
		{
			return;
		}

		tmpState.LeftPressed = ((state.Gamepad.sThumbLX + XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE) < 0);
		tmpState.LeftPressed = tmpState.LeftPressed || (state.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_LEFT);

		tmpState.RightPressed = ((state.Gamepad.sThumbLX - XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE) > 0);
		tmpState.RightPressed = tmpState.RightPressed || (state.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_RIGHT);

		tmpState.UpPressed = ((state.Gamepad.sThumbLY - XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE) > 0);
		tmpState.UpPressed = tmpState.UpPressed || (state.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_UP);

		tmpState.DownPressed = ((state.Gamepad.sThumbLY + XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE) < 0);
		tmpState.DownPressed = tmpState.DownPressed || (state.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_DOWN);

		tmpState.TurboPressed = state.Gamepad.bRightTrigger > 100;
		tmpState.APressed = (state.Gamepad.wButtons & (XINPUT_GAMEPAD_B | XINPUT_GAMEPAD_Y)) != 0;
		tmpState.BPressed = (state.Gamepad.wButtons & (XINPUT_GAMEPAD_A | XINPUT_GAMEPAD_X)) != 0;
		tmpState.LPressed = (state.Gamepad.wButtons & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0;
		tmpState.RPressed = (state.Gamepad.wButtons & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0;
		tmpState.SelectPressed = (state.Gamepad.wButtons & XINPUT_GAMEPAD_BACK) != 0;
		tmpState.StartPressed = (state.Gamepad.wButtons & XINPUT_GAMEPAD_START) != 0;

		this->currentState->buttons = tmpState;
	}

	void CXBOXController::DisposeController()
	{

	}

}