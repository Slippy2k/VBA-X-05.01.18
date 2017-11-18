#pragma once
#include <functional>

namespace EmulatorComponent
{
	class AutosaveCounter sealed
	{
	private:
		float totalElapsedTime;

	public:
		AutosaveCounter();
		virtual ~AutosaveCounter();

		void Update(float elapsedTime);
	};
}