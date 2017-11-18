#pragma once

namespace EmulatorComponent
{
	public value struct ButtonStates sealed
	{
	public:
		bool LeftPressed;
		bool UpPressed;
		bool RightPressed;
		bool DownPressed;
		bool APressed;
		bool BPressed;
		bool LPressed;
		bool RPressed;
		bool StartPressed;
		bool SelectPressed;
		bool TurboPressed;
	};

	public ref struct ControllerState sealed
	{
	internal:
		ButtonStates buttons;

	public:
		property ButtonStates Buttons
		{
			ButtonStates get();
			void set(ButtonStates value);
		}

		ControllerState();
		virtual ~ControllerState();
		void Copy(ControllerState ^state);
		void Reset();
	};

	public interface class IInputChannel
	{
		bool IsConnected();
		ControllerState ^GetCurrentState();
		void Update();

		void DisposeController();
	};
}