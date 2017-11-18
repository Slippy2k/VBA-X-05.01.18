#include "pch.h"
#include "GameTime.h"

using namespace Windows::Foundation;

namespace EmulatorComponent
{
	GameTime::GameTime()
		: elapsedSeconds(0.0), totalElapsedSeconds(0.0)
	{ }
	
	double GameTime::ElapsedSeconds::get()
	{
		return this->elapsedSeconds;
	}

	double GameTime::TotalElapsedSeconds::get()
	{
		return this->totalElapsedSeconds;
	}

	void GameTime::Update(TimeSpan timespan)
	{
		long long delta = timespan.Duration - this->lastFrameTime.Duration;
		this->elapsedSeconds = delta / 10000000.0;
		this->totalElapsedSeconds += this->elapsedSeconds;
		this->lastFrameTime = timespan;
	}
}