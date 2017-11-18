#include "pch.h"
#include "Rectangle.h"

using namespace std;

namespace EmulatorComponent
{
	Rectangle::Rectangle()
		: X(0), Y(0), Width(0), Height(0), TopLeft(Point(0, 0)), SizeField(Size(0, 0))
	{ }

	Rectangle::Rectangle(int x, int y, unsigned int width, unsigned int height)
		: X(x), Y(y), Width(width), Height(height), TopLeft(Point(x, y)), SizeField(Size(width, height))
	{ }

	Rectangle::Rectangle(const Point &topleft, const Size &size)
		: TopLeft(topleft), SizeField(size), X(topleft.X), Y(topleft.Y), Width(size.Width), Height(size.Height)
	{ }

	Rectangle::Rectangle(const Rectangle &other)
	{
		this->TopLeft = other.TopLeft;
		this->SizeField = other.SizeField;
	}

	Rectangle::~Rectangle() { }

	Point Rectangle::GetBottomRight(void) const
	{
		return Point(this->X + this->Width,
			this->Y + this->Height);
	}

	void Rectangle::GetBottomRight(Point *p) const
	{
		p->X = this->X + this->Width;
		p->Y = this->Y + this->Height;
	}

	void Rectangle::SetBottomRight(Point p)
	{
		if (p < this->TopLeft)
		{
			Point tmp;
			tmp = this->TopLeft;
			this->TopLeft = p;
			p = tmp;
		}
		this->Width = p.X - this->TopLeft.X;
		this->Height = p.Y - this->TopLeft.Y;
	}

	bool Rectangle::Contains(const Rectangle &other) const
	{
		Point P1 = this->TopLeft;
		Point P2 = this->GetBottomRight();
		Point otherP1 = other.TopLeft;
		Point otherP2 = other.GetBottomRight();
		return (P1 <= otherP1 &&
			P2 >= otherP2);
	}

	uint8_t Rectangle::Get4WayCollisionDirection(const Vector2 &pos, float offset) const
	{
		uint8_t result = (uint8_t)CollisionType::None;

		float startX = this->X - (this->Width * offset);
		float startY = this->Y - (this->Height * offset);
		float width = this->Width * (1.0f + 2.0f * offset);
		float height = this->Height * (1.0f + 2.0f * offset);

		float x = (pos.X - startX) / width;
		float y = (pos.Y - startY) / height;
		float testX = x - 0.5f;
		float testY = y - 0.5f;

		float centerSpace = 0.3f;
		float offsetCenterSpace = centerSpace / (1.0f + 2.0f * offset);
		float correction = (centerSpace - offsetCenterSpace) * 0.5f;
		float boundary1 = 0.35f + correction;
		float boundary2 = 0.65f - correction;

		if (x >= 0.0f && x <= 1.0f &&
			y >= 0.0f && y <= 1.0f)
		{
			if (abs(testX) > abs(testY))
			{
				if (x >= 0.0f && x <= boundary1)
				{
					result |= CollisionType::Left;
				}
				else if (x > 0.35f && x < boundary2) {
					result |= CollisionType::Center;
				}
				else {
					result |= CollisionType::Right;
				}
			}
			else
			{
				if (y >= 0.0f && y <= boundary1)
				{
					result |= CollisionType::Up;
				}
				else if (y > 0.35f && y < boundary2) {
					result |= CollisionType::Center;
				}
				else {
					result |= CollisionType::Down;
				}
			}
		}

		return result;
	}

	uint8_t Rectangle::GetCollisionDirection(const Vector2 &pos, float offset) const
	{
		uint8_t result = (uint8_t)CollisionType::None;

		float startX = this->X - (this->Width * offset);
		float startY = this->Y - (this->Height * offset);
		float width = this->Width * (1.0f + 2.0f * offset);
		float height = this->Height * (1.0f + 2.0f * offset);

		float x = (pos.X - startX) / width;
		float y = (pos.Y - startY) / height;

		float centerSpace = 0.3f;
		float offsetCenterSpace = centerSpace / (1.0f + 2.0f * offset);
		float correction = (centerSpace - offsetCenterSpace) * 0.5f;
		float boundary1 = 0.35f + correction;
		float boundary2 = 0.65f - correction;

		if (x >= 0.0f && x <= 1.0f &&
			y >= 0.0f && y <= 1.0f)
		{
			if (x >= 0.0f && x <= boundary1)
			{
				result |= CollisionType::Left;
			}
			else if (x > 0.35f && x < boundary2) {
				result |= CollisionType::Center;
			}
			else {
				result |= CollisionType::Right;
			}

			if (y >= 0.0f && y <= boundary1)
			{
				result |= CollisionType::Up;
			}
			else if (y > 0.35f && y < boundary2) {
				result |= CollisionType::Center;
			}
			else {
				result |= CollisionType::Down;
			}
		}

		return result;
	}

	bool Rectangle::Contains(const Vector2 &pos, float offset) const
	{
		float startX = this->X - (this->Width * offset);
		float startY = this->Y - (this->Height * offset);
		float width = this->Width * (1.0f + 2.0f * offset);
		float height = this->Height * (1.0f + 2.0f * offset);

		return (pos.X >= startX && pos.X <= (startX + width)) &&
			(pos.Y >= startY && pos.Y <= (startY + height));
	}

	bool Rectangle::ContainsInCircle(const Vector2 &pos, float offset)
	{
		float startX = this->X - (this->Width * offset);
		float startY = this->Y - (this->Height * offset);
		float width = this->Width * (1.0f + 2.0f * offset);
		float height = this->Height * (1.0f + 2.0f * offset);
		float radiusSqrt = min(width, height) * 0.5f;
		radiusSqrt *= radiusSqrt;
		Vector2 center(startX + width * 0.5f, startY + height * 0.5f);
		return (center - pos).GetLengthSquared() < radiusSqrt;
	}

	bool Rectangle::Intersects(const Rectangle &other) const
	{
		Point P1 = this->TopLeft;
		Point P2 = this->GetBottomRight();
		Point otherP1 = other.TopLeft;
		Point otherP2 = other.GetBottomRight();
		return ((P1 < otherP1 && P2 > otherP1) ||
			(P1 < otherP2 && P2 > otherP2));
	}
}