#pragma once
#include <d3d11_1.h>
#include "GameTime.h"
#include "Size.h"
#include "Rectangle.h"
#include "IRenderComponent.h"
#include "FPSCounter.h"
#include "Color.h"
#include "ISettings.h"

using namespace Microsoft::WRL;

namespace EmulatorComponent
{
	class DXSpriteBatch;

	public delegate void FrameRateCallback(float fps);

	public ref class Renderer sealed
	{
	private:
		Windows::UI::Xaml::Controls::SwapChainPanel ^panel;
		Windows::Foundation::EventRegistrationToken resizeToken;

		D3D_FEATURE_LEVEL featureLevel;
		ComPtr<ID3D11Device1> device;
		ComPtr<ID3D11DeviceContext1> context;
		ComPtr<IDXGISwapChain2> swapchain;

		ComPtr<ID3D11Texture2D> backbuffer;
		ComPtr<ID3D11RenderTargetView> backbufferRTV;

		ComPtr<ID3D11Texture2D> renderTextures[2];
		ComPtr<ID3D11ShaderResourceView> renderTargetSRVs[2];
		uint8_t frontBuffer;
		uint32_t currentPitch;
		void *fbPtr;

		Rectangle splashBounds;
		ComPtr<ID3D11Resource> splashTexture;
		ComPtr<ID3D11ShaderResourceView> splashSRV;

		D3D11_VIEWPORT viewport;

		bool blurring;
		DXSpriteBatch *spriteBatch;
		std::vector<IRenderComponent ^> components;

		float currentWidth;
		float currentHeight;
		Rectangle targetRect;

		bool firstROM;
		FPSCounter ^fps;
		ISettings ^settings;

		void SetSize(float width, float height);
		void calculateTargetRectangle();

		void LoadSettings();
		void Initialize(uint32_t gameWidth, uint32_t gameHeight);
		void InitializeSizeDependentResources();
		void InitializeSpriteBatch(uint32_t gameWidth, uint32_t gameHeight);
		void CreateRenderBuffers(uint32_t gameWidth, uint32_t gameHeight);

		void OnSizeChanged(Platform::Object ^sender, Windows::UI::Xaml::SizeChangedEventArgs ^e);
		void OnDeviceList();
	internal:
		Renderer(Windows::UI::Xaml::Controls::SwapChainPanel ^panel, uint32_t gameWidth, uint32_t gameHeight);

		property ID3D11Device1 *Device
		{
			ID3D11Device1 *get();
		}

		property DXSpriteBatch *SpriteBatch
		{
			DXSpriteBatch *get();
		}

		property float OccupiedHeight
		{
			float get();
		}

		bool SupportsHigherShaders();

		void FirstROMLoaded();
		void AddComponent(IRenderComponent ^component);
		void GameSystemResolutionChanged();
		void Resize(float width, float height);
		void SwapBuffers();
		void GetBackbufferPtr(void **ptr, uint32_t *rowPitch);
		Platform::Array<uint8_t> ^GetSnapshot(int *rowPitch);
		void Render(GameTime ^gameTime);
		void Suspend();
	public:
		event FrameRateCallback ^FrameRateChanged;

		property bool AllowsComplexShaders
		{
			bool get();
		}

		property bool Blur
		{
			bool get();
			void set(bool value);
		}

		virtual ~Renderer();
	};
}