#pragma once
#include "GameTime.h"

namespace EmulatorComponent
{
	ref class Renderer;

	interface class IRenderComponent
	{
		property Renderer ^Renderer
		{
			void set(::EmulatorComponent::Renderer ^value);
		}

		void Resize(float width, float height);
		void Render(GameTime ^gameTime);
	};
}