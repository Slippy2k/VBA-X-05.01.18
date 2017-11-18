#pragma once

namespace EmulatorComponent
{
	public ref class CheatData sealed
	{
	private:
		Platform::String ^code;
		Platform::String ^description;
		bool enabled;
	public:
		property Platform::String ^CheatCode
		{
			Platform::String ^get();
			void set(Platform::String ^value);
		}

		property Platform::String ^Description
		{
			Platform::String ^get();
			void set(Platform::String ^value);
		}

		property bool Enabled
		{
			bool get();
			void set(bool value);
		}
	};
}