#ifndef ONGAMERESIZEBUFFER_H_
#define ONGAMERESIZEBUFFER_H_

#include <D3D11.h>
#include <directxmath.h>

using namespace DirectX;

namespace EmulatorComponent
{
	struct OnGameResizeBuffer
	{
		XMVECTOR texture_size;
	};
}

#endif