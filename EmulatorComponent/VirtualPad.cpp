#include "pch.h"
#include "VirtualPad.h"
#include "TextureLoader.h"
#include "DXSpriteBatch.h"
#include "Renderer.h"
#include "EmulatorComponent.h"

#if _DEBUG
#include <sstream>
using namespace std;
#endif

#define DPAD_TEXTURE_PATH L"Assets/Pad/dpad.dds"
#define BUTTON_A_TEXTURE_PATH L"Assets/Pad/buttonA.dds"
#define BUTTON_B_TEXTURE_PATH L"Assets/Pad/buttonB.dds"
#define BUTTON_L_TEXTURE_PATH L"Assets/Pad/buttonL.dds"
#define BUTTON_R_TEXTURE_PATH L"Assets/Pad/buttonR.dds"
#define BUTTON_START_TEXTURE_PATH L"Assets/Pad/buttonStart.dds"
#define BUTTON_SELECT_TEXTURE_PATH L"Assets/Pad/buttonSelect.dds"
#define BUTTON_TURBO_TEXTURE_PATH L"Assets/Pad/buttonTurbo.dds"
#define STICK_TEXTURE_PATH L"Assets/Pad/stick.dds"
#define STICK_ANCHOR_TEXTURE_PATH L"Assets/Pad/stick2.dds"
#define PIXEL_TEXTURE_PATH	L"Assets/Pad/pixel.dds"

#define PIXEL_HIGHLIGHT_COLOR	Color(0.6, 0.2, 0.2, 0.6)

#define DPAD_SIZE			Size(125, 125)
#define BUTTON_A_SIZE		Size(65, 65)
#define BUTTON_B_SIZE		Size(65, 65)
#define BUTTON_L_SIZE		Size(72, 30)
#define BUTTON_R_SIZE		Size(72, 30)
#define BUTTON_START_SIZE	Size(70, 23)
#define BUTTON_SELECT_SIZE	Size(70, 23)
#define BUTTON_TURBO_SIZE	Size(70, 23)
#define STICK_SIZE			Size(80, 80)
#define STICK_ANCHOR_SIZE	Size(20, 20)

#define TOUCH_INPUT_OFFSET	0.1f
#define DPAD_TOUCH_INPUT_OFFSET	0.2f

using namespace Windows::Foundation;
using namespace Windows::UI::Xaml::Data;

namespace EmulatorComponent
{
	bool PadControl::Contains(Vector2 point, float offset)
	{
		if (this->UseBoundingCircle)
		{
			return this->TargetRect.ContainsInCircle(point, offset);
		}
		else {
			return this->TargetRect.Contains(point, offset);
		}
	}

	VirtualPad::VirtualPad()
		: activePtrs(0),
		joystickGrabbedPtrId(-1)
	{
		this->settings = EmulatorComponent::Current->Settings;
		this->state = ref new ControllerState();

		this->settingsToken = this->settings->PropertyChanged +=
			ref new PropertyChangedEventHandler(
				this, &::EmulatorComponent::VirtualPad::OnPropertyChanged
				);
	}

	VirtualPad::~VirtualPad()
	{
		this->settings->PropertyChanged -= this->settingsToken;
		this->spriteBatch = nullptr;
		this->device = nullptr;
	}

	void VirtualPad::EnableShoulderButtons(bool enable)
	{
		this->lControl.Enabled = enable;
		this->rControl.Enabled = enable;
		this->CalculateRectangles();
	}

	void VirtualPad::StartCustomizing()
	{
		for (int i = 0; i < ARRAYSIZE(this->controls); i++)
		{
			PadControl *control = this->controls[i];
			if (!control->LayoutingDisabled)
			{
				control->GetCustomizationCallback(control->OriginalOffset, false);
				control->GetCustomizationCallback(control->OriginalPOffset, true);
			}
		}
		this->layoutMode = true;
	}

	void VirtualPad::CommitCustomizing()
	{
		this->layoutMode = false;
		this->joystickGrabbedPtrId = -1;
		this->grabbedControl = nullptr;
	}

	void VirtualPad::CancelCustomizing()
	{
		for (int i = 0; i < ARRAYSIZE(this->controls); i++)
		{
			PadControl *control = this->controls[i];
			if (!control->LayoutingDisabled)
			{
				control->CustomizationCallback(control->OriginalOffset, false);
				control->CustomizationCallback(control->OriginalPOffset, true);
			}
		}
		this->layoutMode = false;
		this->joystickGrabbedPtrId = -1;
		this->grabbedControl = nullptr;

		this->CalculateRectangles();
	}

	void VirtualPad::ResetCustomization()
	{
		Vector2 defValue(0.0f, 0.0f);
		for (int i = 0; i < ARRAYSIZE(this->controls); i++)
		{
			PadControl *control = this->controls[i];
			if (!control->LayoutingDisabled)
			{
				control->CustomizationCallback(defValue, this->portrait);
			}
		}
		this->CalculateRectangles();
	}

	void VirtualPad::PointerPressed(Windows::UI::Input::PointerPoint ^point)
	{
		TouchPointer &ptr = this->touchPositions[this->activePtrs++];
		ptr.id = point->PointerId;
		ptr.position.X = point->Position.X;
		ptr.position.Y = point->Position.Y;
		ptr.fresh = true;
	}

	void VirtualPad::PointerMoved(Windows::UI::Input::PointerPoint ^point)
	{
		for (int i = 0; i < this->activePtrs; i++)
		{
			if (this->touchPositions[i].id == point->PointerId)
			{
				this->touchPositions[i].position.X = point->Position.X;
				this->touchPositions[i].position.Y = point->Position.Y;
				break;
			}
		}
	}

	void VirtualPad::PointerReleased(Windows::UI::Input::PointerPoint ^point)
	{
		this->releasePtrId(point->PointerId);
	}

	void VirtualPad::releasePtrId(uint32_t id)
	{
		int i = 0;
		for (; i < this->activePtrs; i++)
		{
			if (this->touchPositions[i].id == id)
			{
				break;
			}
		}
		if (i < this->activePtrs)
		{
			for (; i < this->activePtrs - 1; i++)
			{
				this->touchPositions[i] = this->touchPositions[i + 1];
			}

			this->activePtrs--;

			ZeroMemory(&this->touchPositions[this->activePtrs], sizeof(TouchPointer));
		}

		if (id == this->joystickGrabbedPtrId)
		{
			this->joystickGrabbedPtrId = -1;
			this->grabbedControl = nullptr;
			if (!this->layoutMode)
			{
				this->stickControl.UseAlternativeRectangle = false;

				if (this->settings->ControllerStyle == ControllerStyle::DynamicStick)
				{
					this->stickControl.Enabled = false;
					this->stickAnchorControl.Enabled = false;
					this->stickAnchorControl.UseAlternativeRectangle = false;
				}
			}
		}
	}

	void VirtualPad::OnPropertyChanged(Platform::Object ^sender, PropertyChangedEventArgs ^e)
	{
		if (e->PropertyName->Equals(L"VirtualControllerScale") ||
			e->PropertyName->Equals(L"AspectRatio") ||
			e->PropertyName->Equals(L"VideoScale"))
		{
			this->CalculateRectangles();
		}
		else if (e->PropertyName->Equals(L"ControllerStyle"))
		{
			this->setStickVisibilities();
		}
	}

	void VirtualPad::Renderer::set(::EmulatorComponent::Renderer ^value)
	{
		this->renderer = value;
		this->device = renderer->Device;
		this->spriteBatch = renderer->SpriteBatch;

		Rectangle pixelBounds;
		LoadTextureFromFile(
			this->device.Get(), 
			PIXEL_TEXTURE_PATH, 
			this->pixelTexture.GetAddressOf(), 
			this->pixelSRV.GetAddressOf(), 
			pixelBounds
			);

		this->LoadControls();
	}

	void VirtualPad::LoadControls()
	{
		this->controls[0] = &this->stickAnchorControl;
		this->controls[1] = &this->stickControl;
		this->controls[2] = &this->dpadControl;
		this->controls[3] = &this->aControl;
		this->controls[4] = &this->bControl;
		this->controls[5] = &this->lControl;
		this->controls[6] = &this->rControl;
		this->controls[7] = &this->startControl;
		this->controls[8] = &this->selectControl;
		this->controls[9] = &this->turboControl;

		const wchar_t *texturePaths[] = {
			STICK_ANCHOR_TEXTURE_PATH,
			STICK_TEXTURE_PATH,
			DPAD_TEXTURE_PATH,
			BUTTON_A_TEXTURE_PATH,
			BUTTON_B_TEXTURE_PATH,
			BUTTON_L_TEXTURE_PATH,
			BUTTON_R_TEXTURE_PATH,
			BUTTON_START_TEXTURE_PATH,
			BUTTON_SELECT_TEXTURE_PATH,
			BUTTON_TURBO_TEXTURE_PATH
		};

		Size sizes[] = {
			STICK_ANCHOR_SIZE,
			STICK_SIZE,
			DPAD_SIZE,
			BUTTON_A_SIZE,
			BUTTON_B_SIZE,
			BUTTON_L_SIZE,
			BUTTON_R_SIZE,
			BUTTON_START_SIZE,
			BUTTON_SELECT_SIZE,
			BUTTON_TURBO_SIZE
		
		};

		for (int i = 0; i < ARRAYSIZE(texturePaths) && i < ARRAYSIZE(controls); i++)
		{
			ZeroMemory(controls[i], sizeof(PadControl));

			LoadTextureFromFile(
				this->device.Get(),
				texturePaths[i],
				controls[i]->Texture.GetAddressOf(),
				controls[i]->SRV.GetAddressOf(),
				controls[i]->Bounds
				);

			controls[i]->Enabled = true;
			controls[i]->UnscaledSize = sizes[i];
		}

		this->startControl.Pressed = &this->state->buttons.StartPressed;
		this->selectControl.Pressed = &this->state->buttons.SelectPressed;
		this->turboControl.Pressed = &this->state->buttons.TurboPressed;
		this->aControl.Pressed = &this->state->buttons.APressed;
		this->bControl.Pressed = &this->state->buttons.BPressed;
		this->lControl.Pressed = &this->state->buttons.LPressed;
		this->rControl.Pressed = &this->state->buttons.RPressed;

		this->aControl.UseBoundingCircle = true;
		this->bControl.UseBoundingCircle = true;

		this->stickControl.LayoutingDisabled = true;
		this->stickAnchorControl.LayoutingDisabled = true;

		this->dpadControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait) 
		{
			if (portrait)
			{
				this->settings->PDPadOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->DPadOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->dpadControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PDPadOffset : this->settings->DPadOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->aControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PAOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->AOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->aControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PAOffset : this->settings->AOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->bControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PBOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->BOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->bControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PBOffset : this->settings->BOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->lControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PLOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->LOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->lControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PLOffset : this->settings->LOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->rControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PROffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->ROffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->rControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PROffset : this->settings->ROffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->startControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PStartOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->StartOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->startControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PStartOffset : this->settings->StartOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->selectControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PSelectOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->SelectOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->selectControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PSelectOffset : this->settings->SelectOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});

		this->turboControl.CustomizationCallback = ref new CustomizationCallback([=](const Vector2 &offset, bool portrait)
		{
			if (portrait)
			{
				this->settings->PTurboOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
			else
			{
				this->settings->TurboOffset = Windows::Foundation::Point(offset.X, offset.Y);
			}
		});
		this->turboControl.GetCustomizationCallback = ref new GetCustomizationCallback([=](Vector2 &offset, bool portrait)
		{
			Windows::Foundation::Point pnt = portrait ? this->settings->PTurboOffset : this->settings->TurboOffset;
			offset.X = pnt.X;
			offset.Y = pnt.Y;
		});


		this->setStickVisibilities();
	}

	void VirtualPad::setStickVisibilities()
	{
		switch (this->settings->ControllerStyle)
		{
		case ControllerStyle::DynamicStick:
			this->stickAnchorControl.Enabled = false;
			this->stickControl.Enabled = false;
			this->dpadControl.Enabled = false;
			break;
		case ControllerStyle::FixedStick:
			this->stickAnchorControl.Enabled = true;
			this->stickControl.Enabled = true;
			this->dpadControl.Enabled = false;
			break;
		default:
		case ControllerStyle::EightWay:
		case ControllerStyle::FourWay:
			this->stickAnchorControl.Enabled = false;
			this->stickControl.Enabled = false;
			this->dpadControl.Enabled = true;
			break;
		}
		this->CalculateRectangles();
	}

	void VirtualPad::Resize(float width, float height)
	{
		this->currentWidth = width;
		this->currentHeight = height;
		this->portrait = width <= height;

		this->CalculateRectangles();
	}

	void VirtualPad::CalculateRectangles()
	{
		if (this->currentWidth <= this->currentHeight)
		{
			this->CalculatePortraitRectangles();
		}
		else
		{
			this->CalculateLandscapeRectangles();
		}
	}

	void VirtualPad::CalculatePortraitRectangles()
	{
		// make sure there is at least some space for the controls even if the controls must overlap the image
		float areaBegin = min(this->renderer->OccupiedHeight, this->currentHeight - 250.0f);

		// use space if shoulder buttons are disabled
		if (!this->lControl.Enabled && !this->rControl.Enabled)
		{
			areaBegin -= (this->currentHeight - areaBegin) * 0.25;
		}

		this->calculateRectangles(
			areaBegin + (this->currentHeight - areaBegin) * 0.25f - this->dpadControl.UnscaledSize.Height * 0.25f,
			areaBegin + (this->currentHeight - areaBegin) * 0.50f,
			this->currentHeight
			);
	}

	void VirtualPad::CalculateLandscapeRectangles()
	{
		this->calculateRectangles(
			this->currentHeight * 0.33f,
			this->currentHeight * 0.66f,
			this->currentHeight
			);
	}

	void VirtualPad::calculateRectangles(float firstLine, float secondLine, float thirdLine)
	{
		float centerX = this->currentWidth * 0.5f;
		float paddingX = 25.0f;

		// align center of dpad with center of screen
		float dpadY = secondLine - this->dpadControl.UnscaledSize.Height * 0.5f;
		float dpadX = paddingX;
		Vector2 dpadOffset;
		this->dpadControl.GetCustomizationCallback(dpadOffset, this->portrait);

		this->setRectangle(this->dpadControl, dpadX, dpadY, dpadOffset);

		float stickAnchorY = dpadY + this->dpadControl.TargetRect.Height * 0.5f - this->stickAnchorControl.UnscaledSize.Height * 0.5f;
		float stickAnchorX = dpadX + this->dpadControl.UnscaledSize.Width * 0.5f - this->stickAnchorControl.UnscaledSize.Width * 0.5f;
		float stickY = dpadY + this->dpadControl.TargetRect.Height * 0.5f - this->stickControl.UnscaledSize.Height * 0.5f;
		float stickX = dpadX + this->dpadControl.UnscaledSize.Width * 0.5f - this->stickControl.UnscaledSize.Width * 0.5f;

		this->setRectangle(this->stickAnchorControl, stickAnchorX, stickAnchorY, dpadOffset);
		this->setRectangle(this->stickControl, stickX, stickY, dpadOffset);

		// put L/R buttons mid of the remaining space
		float lrY = firstLine - max(this->lControl.UnscaledSize.Height, this->rControl.UnscaledSize.Height) * 0.5;
		float lX = this->lControl.UnscaledSize.Width * -0.14;
		float rX = this->currentWidth - this->rControl.UnscaledSize.Width * 0.86;
		Vector2 lOffset, rOffset;
		this->lControl.GetCustomizationCallback(lOffset, this->portrait);
		this->rControl.GetCustomizationCallback(rOffset, this->portrait);

		this->setRectangle(this->lControl, lX, lrY, lOffset);
		this->setRectangle(this->rControl, rX, lrY, rOffset);

		// put a/b buttons right of dpad
		// a on top of center-line
		float aY = secondLine - this->aControl.UnscaledSize.Height;
		float aX = this->currentWidth - paddingX - this->aControl.UnscaledSize.Width;
		Vector2 aOffset;
		this->aControl.GetCustomizationCallback(aOffset, this->portrait);

		// b right on the center line
		float bY = secondLine;
		float bX = aX - this->bControl.UnscaledSize.Width;
		Vector2 bOffset;
		this->bControl.GetCustomizationCallback(bOffset, this->portrait);

		this->setRectangle(this->aControl, aX, aY, aOffset);
		this->setRectangle(this->bControl, bX, bY, bOffset);

		// bottom center for start / select
		float startSelectY = thirdLine - 1.5f * max(this->startControl.UnscaledSize.Height, this->selectControl.UnscaledSize.Height);
		float selectX = centerX - 1.7f * this->selectControl.UnscaledSize.Width;
		float turboX = centerX - 0.5f * this->turboControl.UnscaledSize.Width;
		float startX = centerX + 0.7f * this->startControl.UnscaledSize.Width;
		Vector2 startOffset, selectOffset, turboOffset;
		this->startControl.GetCustomizationCallback(startOffset, this->portrait);
		this->selectControl.GetCustomizationCallback(selectOffset, this->portrait);
		this->turboControl.GetCustomizationCallback(turboOffset, this->portrait);

		this->setRectangle(this->selectControl, selectX, startSelectY, selectOffset);
		this->setRectangle(this->startControl, startX, startSelectY, startOffset);
		this->setRectangle(this->turboControl, turboX, startSelectY, turboOffset);

		this->scaleRectangles();
	}

	void VirtualPad::setRectangle(PadControl &control, float x, float y, Vector2 &offset)
	{
		control.TargetRect.X = x;
		control.TargetRect.Y = y;
		control.TargetRect.X += offset.X;
		control.TargetRect.Y += offset.Y;
		control.TargetRect.Width = control.UnscaledSize.Width;
		control.TargetRect.Height = control.UnscaledSize.Height;
	}


	void VirtualPad::scaleRectangles()
	{
		float scale = this->settings->VirtualControllerScale;
		if (abs(100.0f - scale) < 0.01f)
		{
			return;
		}
		scale *= 0.01f;
		for (int i = 0; i < ARRAYSIZE(this->controls); i++)
		{
			Rectangle &rect = this->controls[i]->TargetRect;
			float scaledWidth = rect.Width * scale;
			float scaledHeight = rect.Height * scale;
			rect.X += (rect.Width - scaledWidth) * 0.5f;
			rect.Y += (rect.Height - scaledHeight) * 0.5f;
			rect.Width = scaledWidth;
			rect.Height = scaledHeight;
		}
	}

	void VirtualPad::Render(GameTime ^gameTime)
	{
		if (!this->settings->ShowVirtualController)
		{
			return;
		}
		auto accentColor = this->settings->CurrentAccentColor;

		Color renderColor(0.8f + 0.2f * (accentColor.R / 255.0f), 0.8f + 0.2f * (accentColor.G / 255.0f), 0.8f + 0.2f * (accentColor.B / 255.0f), this->settings->VirtualControllerOpacity * 0.01f);

		XMMATRIX matrix = XMMatrixIdentity();
		this->spriteBatch->Begin(matrix);

		ComPtr<ID3D11Texture2D> tex;

		if (this->layoutMode)
		{
			this->pixelTexture.As(&tex);
			this->spriteBatch->Draw(
				this->dpadControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->aControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->bControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->lControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->rControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->startControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->selectControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);

			this->spriteBatch->Draw(
				this->turboControl.TargetRect,
				this->pixelSRV.Get(),
				tex.Get(), PIXEL_HIGHLIGHT_COLOR);
		}

		for (int i = 0; i < ARRAYSIZE(this->controls); i++)
		{
			PadControl *control = this->controls[i];
			if (!control->Enabled)
			{
				continue;
			}

			Rectangle *target;
			if (control->UseAlternativeRectangle)
			{
				target = &control->AltTargetRect;
			}
			else {
				target = &control->TargetRect;
			}

			control->Texture.As(&tex);
			if (control->Pressed != nullptr && *(control->Pressed))
			{
				Rectangle tmpRect = *target;
				tmpRect.X -= 0.13f * tmpRect.Width;
				tmpRect.Y -= 0.13f * tmpRect.Height;
				tmpRect.Width *= 1.26f;
				tmpRect.Height *= 1.26f;
				Color tmpColor = renderColor;
				tmpColor.A *= 0.8f;

				this->spriteBatch->Draw(
					tmpRect,
					control->SRV.Get(),
					tex.Get(), tmpColor);
			}
			else {
				this->spriteBatch->Draw(
					*target,
					control->SRV.Get(),
					tex.Get(), renderColor);
			}

		}

		this->spriteBatch->End();
	}

	bool VirtualPad::IsConnected()
	{
		return this->settings->ShowVirtualController;
	}

	ControllerState ^VirtualPad::GetCurrentState()
	{
		return this->state;
	}

	uint8_t VirtualPad::GetDirectionFromCenter(const Vector2 &center, const TouchPointer &ptr)
	{
		uint8_t result = 0;
		Vector2 unitX(1.0f, 0.0f);
		Vector2 unitY(0.0f, 1.0f);
		Vector2 delta = ptr.position - center;
		if (abs(delta.X) >= this->settings->StickDeadzone)
		{
			result |= delta.X > 0 ? CollisionType::Right : CollisionType::Left;
		}
		if (abs(delta.Y) >= this->settings->StickDeadzone) {
			result |= delta.Y > 0 ? CollisionType::Down : CollisionType::Up;
		}
		return result;
	}

	void VirtualPad::Update()
	{
		ButtonStates tmpState;
		
		if (this->layoutMode)
		{
			for (int i = 0; i < this->activePtrs; i++)
			{
				TouchPointer &ptr = this->touchPositions[i];

				for (int j = 0; j < ARRAYSIZE(this->controls); j++)
				{
					PadControl *control = this->controls[j];
					if (control->LayoutingDisabled)
					{
						continue;
					}

					if (ptr.fresh && this->grabbedControl == nullptr)
					{
						if (control->Contains(ptr.position, 0.0f))
						{
							this->joystickGrabbedPtrId = ptr.id;
							this->grabbedControl = control;
							this->lastGrabbedPosition = ptr.position;
							break;
						}
					}
				}
				if (this->joystickGrabbedPtrId == ptr.id && this->grabbedControl != nullptr)
				{
					Vector2 delta = ptr.position - this->lastGrabbedPosition;
					Vector2 currentOffset;
					this->grabbedControl->GetCustomizationCallback(currentOffset, this->portrait);
					this->grabbedControl->CustomizationCallback(currentOffset + delta, this->portrait);
					this->lastGrabbedPosition = ptr.position;
					this->CalculateRectangles();
					break;
				}
				
				ptr.fresh = false;
			}
		}
		else
		{
			float dpadTouchOffset = DPAD_TOUCH_INPUT_OFFSET * min(100.0f, max(0.0f, 200.0f - this->settings->VirtualControllerScale)) * 0.01f;
			float touchOffset = TOUCH_INPUT_OFFSET * min(100.0f, max(0.0f, 200.0f - this->settings->VirtualControllerScale)) * 0.01f;

			for (int i = 0; i < this->activePtrs; i++)
			{
				TouchPointer &ptr = this->touchPositions[i];
				bool buttonHit = false;
				uint8_t dpad = 0;

				switch (this->settings->ControllerStyle)
				{
				case ControllerStyle::FourWay:
					dpad = this->dpadControl.TargetRect.Get4WayCollisionDirection(ptr.position, dpadTouchOffset);
					break;
				case ControllerStyle::EightWay:
					dpad = this->dpadControl.TargetRect.GetCollisionDirection(ptr.position, dpadTouchOffset);
					break;
				}

				if (dpad > 0)
				{
					tmpState.LeftPressed = dpad & CollisionType::Left;
					tmpState.RightPressed = dpad & CollisionType::Right;
					tmpState.UpPressed = dpad & CollisionType::Up;
					tmpState.DownPressed = dpad & CollisionType::Down;
				}
				else if (ptr.id != this->joystickGrabbedPtrId)
				{
					if (this->startControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.StartPressed = true;
					}
					if (this->selectControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.SelectPressed = true;
					}
					if (this->turboControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.TurboPressed = true;
					}
					if (this->aControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.APressed = true;
					}
					if (this->bControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.BPressed = true;
					}
					if (this->lControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.LPressed = true;
					}
					if (this->rControl.Contains(ptr.position, touchOffset))
					{
						buttonHit = tmpState.RPressed = true;
					}
				}

				if (!buttonHit && (this->settings->ControllerStyle == ControllerStyle::DynamicStick ||
					this->settings->ControllerStyle == ControllerStyle::FixedStick))
				{
					// handle stick with lowest priority because the touch area overlaps other components

					bool dynamic = false;
					switch (this->settings->ControllerStyle)
					{
					case ControllerStyle::DynamicStick:
						dynamic = true;
					case ControllerStyle::FixedStick:
						if (this->joystickGrabbedPtrId < 0 && ptr.fresh)
						{
							Rectangle stickTouchArea;

							if (dynamic)
							{
								stickTouchArea = Rectangle(
									0, 0, this->currentWidth * 0.5f, this->currentHeight
									);
							}
							else {
								stickTouchArea = Rectangle(
									this->stickControl.TargetRect.X - this->stickControl.TargetRect.Width * 0.5f,
									this->stickControl.TargetRect.Y - this->stickControl.TargetRect.Height * 0.5f,
									this->stickControl.TargetRect.Width * 2.0f,
									this->stickControl.TargetRect.Height * 2.0f
									);
							}

							if (stickTouchArea.Contains(ptr.position, 0.0f))
							{
								this->joystickGrabbedPtrId = ptr.id;
								this->stickControl.UseAlternativeRectangle = true;

								if (dynamic)
								{
									this->stickControl.Enabled = true;
									this->stickAnchorControl.Enabled = true;
									this->stickAnchorControl.UseAlternativeRectangle = true;
									this->stickAnchorControl.AltTargetRect.X = ptr.position.X - this->stickAnchorControl.TargetRect.Width * 0.5f;
									this->stickAnchorControl.AltTargetRect.Y = ptr.position.Y - this->stickAnchorControl.TargetRect.Height * 0.5f;
									this->stickAnchorControl.AltTargetRect.Width = this->stickAnchorControl.TargetRect.Width;
									this->stickAnchorControl.AltTargetRect.Height = this->stickAnchorControl.TargetRect.Height;
								}
							}
						}
						if (ptr.id == this->joystickGrabbedPtrId)
						{
							float halfWidth = this->stickControl.TargetRect.Width * 0.5f;
							float halfHeight = this->stickControl.TargetRect.Height * 0.5f;
							this->stickControl.AltTargetRect.X = ptr.position.X - halfWidth;
							this->stickControl.AltTargetRect.Y = ptr.position.Y - halfHeight;
							this->stickControl.AltTargetRect.Width = this->stickControl.TargetRect.Width;
							this->stickControl.AltTargetRect.Height = this->stickControl.TargetRect.Height;

							if (!dynamic)
							{
								dpad = this->GetDirectionFromCenter(
									Vector2(this->stickControl.TargetRect.X + halfWidth, this->stickControl.TargetRect.Y + halfHeight),
									ptr
									);
							}
							else
							{
								dpad = this->GetDirectionFromCenter(
									Vector2(this->stickAnchorControl.AltTargetRect.X + this->stickAnchorControl.AltTargetRect.Width * 0.5f,
										this->stickAnchorControl.AltTargetRect.Y + this->stickAnchorControl.AltTargetRect.Height * 0.5f),
									ptr
									);
							}
						}

						break;
					}

					if (dpad > 0)
					{
						tmpState.LeftPressed = dpad & CollisionType::Left;
						tmpState.RightPressed = dpad & CollisionType::Right;
						tmpState.UpPressed = dpad & CollisionType::Up;
						tmpState.DownPressed = dpad & CollisionType::Down;
					}
				}
				ptr.fresh = false;
			}
		}		
		this->state->buttons = tmpState;
	}

	void VirtualPad::DisposeController()
	{

	}

}
