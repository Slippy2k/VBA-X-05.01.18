#pragma once
#include <d3d11_1.h>
#include "Rectangle.h"
#include "IRenderComponent.h"
#include "ISettings.h"
#include "ITouchInputHandler.h"
#include "IInputChannel.h"
#include "Vector2.h"

using namespace Microsoft::WRL;

namespace EmulatorComponent
{
	class DXSpriteBatch;
	ref class Renderer;

	delegate void CustomizationCallback(const Vector2 &offset, bool portrait);
	delegate void GetCustomizationCallback(Vector2 &offset, bool portrait);

	struct PadControl
	{
		Rectangle Bounds;
		Rectangle TargetRect;
		Rectangle AltTargetRect;
		Size UnscaledSize;
		ComPtr<ID3D11Resource> Texture;
		ComPtr<ID3D11ShaderResourceView> SRV;
		CustomizationCallback ^CustomizationCallback;
		GetCustomizationCallback ^GetCustomizationCallback;
		Vector2 OriginalOffset;
		Vector2 OriginalPOffset;
		bool Enabled;
		bool *Pressed;
		bool UseAlternativeRectangle;
		bool UseBoundingCircle;
		bool LayoutingDisabled;

		bool Contains(Vector2 point, float offset);
	};

	struct TouchPointer
	{
		unsigned int id;
		Vector2 position;
		bool fresh;
	};

	ref class VirtualPad sealed : IRenderComponent, ITouchInputHandler, IInputChannel
	{
	private:
		ISettings ^settings;
		Renderer ^renderer;
		DXSpriteBatch *spriteBatch;
		ComPtr<ID3D11Device1> device;
		ControllerState ^state;

		ComPtr<ID3D11Resource> pixelTexture;
		ComPtr<ID3D11ShaderResourceView> pixelSRV;

		PadControl dpadControl;
		PadControl aControl;
		PadControl bControl;
		PadControl lControl;
		PadControl rControl;
		PadControl startControl;
		PadControl selectControl;
		PadControl turboControl;
		PadControl stickAnchorControl;
		PadControl stickControl;

		PadControl *controls[10];

		float currentWidth;
		float currentHeight;
		bool portrait;

		int joystickGrabbedPtrId;
		PadControl *grabbedControl;
		Vector2 lastGrabbedPosition;
		uint8_t GetDirectionFromCenter(const Vector2 &center, const TouchPointer &ptr);

		Windows::Foundation::EventRegistrationToken settingsToken;

		TouchPointer touchPositions[10];
		int activePtrs;

		void releasePtrId(uint32_t id);

		bool layoutMode;

		void LoadControls();
		void CalculateRectangles();
		void CalculatePortraitRectangles();
		void CalculateLandscapeRectangles();
		void setStickVisibilities();
		void calculateRectangles(float firstLine, float secondLine, float thirdLine);
		void setRectangle(PadControl &control, float x, float y, Vector2 &offset);
		void scaleRectangles();

		void OnPropertyChanged(Platform::Object ^sender, Windows::UI::Xaml::Data::PropertyChangedEventArgs ^e);
	public:
		property Renderer ^Renderer
		{
			virtual void set(::EmulatorComponent::Renderer ^value);
		}

		VirtualPad();
		virtual ~VirtualPad();

		void EnableShoulderButtons(bool enable);

		virtual void StartCustomizing();
		virtual void CommitCustomizing();
		virtual void CancelCustomizing();
		virtual void ResetCustomization();

		virtual void PointerPressed(Windows::UI::Input::PointerPoint ^point);
		virtual void PointerMoved(Windows::UI::Input::PointerPoint ^point);
		virtual void PointerReleased(Windows::UI::Input::PointerPoint ^point);

		virtual void Resize(float width, float height);

		virtual void Render(GameTime ^gameTime);


		virtual bool IsConnected();
		virtual ControllerState ^GetCurrentState();
		virtual void Update();

		virtual void DisposeController();
	};
}