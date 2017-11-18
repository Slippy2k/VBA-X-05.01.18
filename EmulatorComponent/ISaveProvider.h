#pragma once

namespace EmulatorComponent
{
	public ref class ByteWrapper sealed
	{
	internal:
		uint8 *data;
		int length;
		bool dontDelete;
		
		ByteWrapper(uint8 *data, int length, bool dontDelete = false);
	public:

		ByteWrapper(const Platform::Array<uint8> ^arr);
		virtual ~ByteWrapper();

		Platform::Array<uint8> ^AsArray();
	};

	public interface class ISaveProvider
	{
		Windows::Foundation::IAsyncOperation<ByteWrapper ^> ^LoadSRAMAsync();
		Windows::Foundation::IAsyncAction ^SaveSRAMAsync(ByteWrapper ^bytes);
		Windows::Foundation::IAsyncAction ^TriggerAutosave();
	};
}