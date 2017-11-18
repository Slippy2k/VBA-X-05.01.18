#pragma once

namespace EmulatorComponent
{
	ref class GameTime sealed
	{
	private:
		Windows::Foundation::TimeSpan lastFrameTime;
		double totalElapsedSeconds;
		double elapsedSeconds;

	internal:
		GameTime();

		property double ElapsedSeconds
		{
			double get();
		}

		property double TotalElapsedSeconds
		{
			double get();
		}

		void Update(Windows::Foundation::TimeSpan timespan);
	};
}