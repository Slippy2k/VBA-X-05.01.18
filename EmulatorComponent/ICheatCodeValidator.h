#pragma once
#include <collection.h>

namespace EmulatorComponent
{
	public interface class ICheatCodeValidator
	{
	public:
		bool CheckCode(Platform::String ^code, Windows::Foundation::Collections::IVector<Platform::String ^> ^singleCodes);
	};
}