#include "pch.h"
#include "FPSCounter.h"

namespace EmulatorComponent
{
	FPSCounter::FPSCounter()
	{

	}

	FPSCounter::~FPSCounter()
	{

	}

	double FPSCounter::FPS::get()
	{
		this->changed = false;
		return this->fps;
	}

	bool FPSCounter::Changed::get()
	{
		return this->changed;
	}

	void FPSCounter::Update(GameTime ^gameTime)
	{
		this->elapsedTime += gameTime->ElapsedSeconds;

		if (this->elapsedTime > 0.5)
		{
			this->elapsedTime -= 0.5;
			float fps;
			if (gameTime->ElapsedSeconds == 0.0)
			{
				fps = 60.0;
			}
			else {
				fps = (int)(10.0 / gameTime->ElapsedSeconds) / 10.0;
			}
			if (fps != this->fps || this->unchangedIntervals > 3)
			{
				this->fps = fps;
				this->changed = true;
				this->unchangedIntervals = 0;
			}
			else {
				this->unchangedIntervals++;
			}
		}
	}
}