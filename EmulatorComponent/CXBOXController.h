#ifndef CXBOXCONTROLLER_H_
#define CXBOXCONTROLLER_H_

#include <Windows.h>
#include <Xinput.h>
#include "IInputChannel.h"

namespace EmulatorComponent
{
	ref class CXBOXController sealed
		: public IInputChannel
	{
	private:
		ControllerState ^currentState;
		int controllerNumber;

	public:
		CXBOXController(int playerNumber);
		virtual ~CXBOXController();

		virtual bool IsConnected();
		void Vibrate(int leftVal = 0, int rightVal = 0);

		virtual ControllerState ^GetCurrentState();
		virtual void Update();

		virtual void DisposeController();
	};
}

#endif