#pragma once
#include "CXBOXController.h"
#include <vector>

namespace EmulatorComponent
{
	public ref class Controller sealed
	{
	private:
		std::vector<IInputChannel ^> inputChannels;
		ControllerState ^currentState;
		IInputChannel ^currentHIDChannel;

		uint32_t turboPressedUpdates;
		bool holdTurbo;

	internal:
		Controller(bool xboxSupport);

		ControllerState ^GetControllerState() const;
		void Update();

	public:
		virtual ~Controller();

		void AddInputChannel(IInputChannel ^inputChannel);
		void SetHIDChannel(IInputChannel ^inputChannel);
	};
}