#include "pch.h"
#include "ISaveProvider.h"

using namespace Platform;

namespace EmulatorComponent
{
	ByteWrapper::ByteWrapper(uint8_t *data, int length, bool dontDelete)
		: data(data), length(length), dontDelete(dontDelete)
	{ }

	ByteWrapper::ByteWrapper(const Array<uint8> ^arr)
	{
		this->length = arr->Length;
		this->data = new uint8_t[this->length];
		for (int i = 0; i < this->length; i++)
		{
			this->data[i] = arr[i];
		}
	}

	ByteWrapper::~ByteWrapper()
	{
		if (this->data && !this->dontDelete)
		{
			delete[] this->data;
		}
		this->data = nullptr;
		this->length = 0;
	}

	Array<uint8> ^ByteWrapper::AsArray()
	{
		Array<uint8> ^arr = ref new Array<uint8>(this->length);
		memcpy_s(arr->Data, this->length, this->data, this->length);
		return arr;
	}
}
