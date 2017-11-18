#include "pch.h"
#include "CheatData.h"

using namespace Windows::UI::Xaml::Data;

namespace EmulatorComponent
{
	Platform::String ^CheatData::CheatCode::get()
	{
		return this->code;
	}

	void CheatData::CheatCode::set(Platform::String ^value)
	{
		if (this->code != value)
		{
			this->code = value;
		}
	}

	Platform::String ^CheatData::Description::get()
	{
		return this->description;
	}

	void CheatData::Description::set(Platform::String ^value)
	{
		if (this->description != value)
		{
			this->description = value;
		}
	}

	bool CheatData::Enabled::get()
	{
		return this->enabled;
	}

	void CheatData::Enabled::set(bool value)
	{
		if (this->enabled != value)
		{
			this->enabled = value;
		}
	}
}