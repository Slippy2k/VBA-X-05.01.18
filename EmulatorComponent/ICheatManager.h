#pragma once
#include "CheatData.h"

namespace EmulatorComponent
{
	public interface class ICheatManager
	{
	public:
		property ICheatCodeValidator ^CheatValidator
		{
			ICheatCodeValidator ^get();
		}

		void ApplyCheats(Windows::Foundation::Collections::IVector<CheatData ^> ^cheats);
		void ReapplyCheats();
	};
}