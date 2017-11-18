#ifndef SIZE_H_
#define SIZE_H_

namespace EmulatorComponent
{
	struct Size
	{
		unsigned int Width, Height;
		Size() { }
		Size(unsigned int width, unsigned int height)
			: Width(width), Height(height)
		{ }
		~Size(void) { }
	};
}

#endif