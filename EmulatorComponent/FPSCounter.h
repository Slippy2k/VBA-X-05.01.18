#pragma once

#include "GameTime.h"
#include <stdint.h>

namespace EmulatorComponent
{
	ref class FPSCounter sealed
	{
	private:
		double fps;
		double elapsedTime;
		bool changed;
		short unchangedIntervals;

	internal:
		FPSCounter();

	public:
		property double FPS
		{
			double get();
		}

		property bool Changed
		{
			bool get();
		}

		virtual ~FPSCounter();

		void Update(GameTime ^gameTime);
	};
}