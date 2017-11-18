#pragma once
#include <stdint.h>

namespace EmulatorComponent
{
	public interface class IROMData
	{
		property uint32_t Size
		{ uint32_t get(); }

		property Platform::Array<uint8> ^Data
		{ Platform::Array<uint8> ^get(); }

		property Windows::Storage::IStorageFile ^File
		{ Windows::Storage::IStorageFile ^get();  }

		property Platform::String ^FileExtension
		{ Platform::String ^get(); }
	};
}