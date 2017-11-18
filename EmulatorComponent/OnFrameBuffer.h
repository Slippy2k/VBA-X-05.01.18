#ifndef ONFRAMEBUFFER_H_
#define ONFRAMEBUFFER_H_

#include <D3D11.h>
#include <directxmath.h>

using namespace DirectX;

namespace EmulatorComponent
{
	struct OnFrameBuffer
	{
		XMMATRIX world;
	};
}

#endif