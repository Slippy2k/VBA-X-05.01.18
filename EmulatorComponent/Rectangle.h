#ifndef RECTANGLE_H_
#define RECTANGLE_H_

#include "Point.h"
#include "Size.h"
#include "Vector2.h"

namespace EmulatorComponent
{
	enum CollisionType : uint8_t
	{
		None = 0,
		Left = 1,
		Right = 2,
		Down = 4,
		Up = 8,
		Center = 16
	};

	struct Rectangle
	{
	public:
		union
		{
			struct
			{
				int X;
				int Y;
				unsigned int Width;
				unsigned int Height;
			};
			struct
			{
				Point TopLeft;
				Size SizeField;
			};
		};

		Rectangle();
		Rectangle(int x, int y, unsigned int width, unsigned int height);
		Rectangle(const Point &topleft, const Size &size);
		Rectangle(const Rectangle &other);
		~Rectangle();

		Point GetBottomRight(void) const;
		void GetBottomRight(Point *p) const;
		void SetBottomRight(Point p);

		uint8_t Get4WayCollisionDirection(const Vector2 &pos, float offset) const;
		uint8_t GetCollisionDirection(const Vector2 &pos, float offset) const;
		bool ContainsInCircle(const Vector2 &pos, float offset);
		bool Contains(const Vector2 &pos, float offset) const;
		bool Contains(const Rectangle &other) const;
		bool Intersects(const Rectangle &other) const;
	};
}

#endif