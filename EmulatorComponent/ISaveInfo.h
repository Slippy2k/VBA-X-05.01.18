#pragma once

namespace EmulatorComponent
{
	public interface class ISaveInfo
	{
		property Platform::String ^SRAMExtension
		{
			Platform::String ^get();
		}

		property Platform::String ^SaveStateExtension
		{
			Platform::String ^get();
		}
	};
}