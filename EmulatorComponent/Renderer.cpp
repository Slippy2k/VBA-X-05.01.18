#include "pch.h"
#include "Renderer.h"
#include <windows.ui.xaml.media.dxinterop.h>
#include "EmulatorComponent.h"
#include "DXSpriteBatch.h"
#include "TextureLoader.h"

using namespace Platform;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Core;
using namespace Microsoft::WRL;

#define MAX_FRAME_LATENCY	2
#define DEFAULT_WIDTH	512
#define DEFAULT_HEIGHT	512

#define SPLASH_TEXTURE_PATH L"Assets/Misc/GBA_splash.dds"

namespace EmulatorComponent
{
	ID3D11Device1 *Renderer::Device::get()
	{
		return this->device.Get();
	}

	DXSpriteBatch *Renderer::SpriteBatch::get() 
	{
		return this->spriteBatch;
	}

	float Renderer::OccupiedHeight::get()
	{
		return this->targetRect.Height;
	}

	Renderer::Renderer(SwapChainPanel ^panel, uint32_t gameWidth, uint32_t gameHeight)
		: panel(panel), fbPtr(nullptr), currentPitch(0)
	{
		this->fps = ref new FPSCounter();
		this->resizeToken = panel->SizeChanged += ref new SizeChangedEventHandler(this, &Renderer::OnSizeChanged);
		this->settings = EmulatorComponent::Current->Settings;

		this->SetSize(panel->ActualWidth, panel->ActualHeight);

		this->Initialize(gameWidth, gameHeight);
		this->InitializeSizeDependentResources();
		this->InitializeSpriteBatch(gameWidth, gameHeight);

		this->LoadSettings();
	}

	Renderer::~Renderer()
	{
		this->components.clear();

		if (this->fbPtr && this->currentPitch > 0)
		{
			this->context->Unmap(this->renderTextures[(this->frontBuffer + 1) % 2].Get(), 0);
			this->currentPitch = 0;
			this->fbPtr = nullptr;
		}
		if (this->spriteBatch)
		{
			delete this->spriteBatch;
			this->spriteBatch = nullptr;
		}
	}

	void Renderer::LoadSettings()
	{
		this->settings = EmulatorComponent::Current->Settings;

		if (this->spriteBatch)
		{
			SamplingMode samplingMode = (SamplingMode)((int)settings->Filter);

			this->spriteBatch->SetTextureSampling(samplingMode);
		}

		settings->PropertyChanged += ref new Windows::UI::Xaml::Data::PropertyChangedEventHandler([=](Object ^sender, Windows::UI::Xaml::Data::PropertyChangedEventArgs ^args)
		{
			if (args->PropertyName->Equals("Filter"))
			{
				SamplingMode samplingMode = (SamplingMode)((int)settings->Filter);

				this->spriteBatch->SetTextureSampling(samplingMode);
			}
			else if (args->PropertyName->Equals("AspectRatio") || 
					(args->PropertyName->Equals("VideoScale")))
			{
				this->calculateTargetRectangle();
			}
		});
	}

	void Renderer::Initialize(uint32_t gameWidth, uint32_t gameHeight)
	{
		ComPtr<ID3D11Device> device;
		ComPtr<ID3D11DeviceContext> context;

		D3D_FEATURE_LEVEL levels[] = {
			D3D_FEATURE_LEVEL_11_0,
			D3D_FEATURE_LEVEL_10_1,
			D3D_FEATURE_LEVEL_10_0,
			D3D_FEATURE_LEVEL_9_3,
			D3D_FEATURE_LEVEL_9_2,
			D3D_FEATURE_LEVEL_9_1
		};

		uint32_t flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#ifndef ARM_APP
#if (_DEBUG)
		//flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif
#endif

		if (FAILED(D3D11CreateDevice(
			nullptr,
			D3D_DRIVER_TYPE_HARDWARE,
			nullptr,
			flags,
			levels, ARRAYSIZE(levels),
			D3D11_SDK_VERSION,
			device.GetAddressOf(),
			&this->featureLevel,
			context.GetAddressOf()
			)))
		{
			throw std::exception("D3D11CreateDevice failed.");
		}
		if (FAILED(device.As(&this->device)))
		{
			throw std::exception("ID3D11Device::As failed.");
		}
		if (FAILED(context.As(&this->context)))
		{
			throw std::exception("ID3D11DeviceContext::As failed.");
		}

		/*ComPtr<IDXGIDevice> dxgiDevice;
		if (FAILED(this->device.As(&dxgiDevice)))
		{
		throw std::exception("ID3D11Device1::As failed.");
		}*/

		this->CreateRenderBuffers(gameWidth, gameHeight);

		LoadTextureFromFile(this->device.Get(), SPLASH_TEXTURE_PATH, this->splashTexture.GetAddressOf(), this->splashSRV.GetAddressOf(), this->splashBounds);
	}

	void Renderer::InitializeSpriteBatch(uint32_t gameWidth, uint32_t gameHeight)
	{
		this->spriteBatch = new DXSpriteBatch(this->device.Get(), this->context.Get(), this->featureLevel, this->currentWidth, this->currentHeight);
		this->spriteBatch->SetGameTextureSize(gameWidth, gameHeight);
	}


	void Renderer::InitializeSizeDependentResources()
	{
		// remove all render targets before resizing or recreating any buffer
		this->context->OMSetRenderTargets(0, nullptr, nullptr);
		this->backbufferRTV = nullptr;
		this->backbuffer = nullptr;

		if (this->swapchain)
		{

			if (FAILED(this->swapchain->ResizeBuffers(
				2,
				static_cast<uint32_t>(this->currentWidth),
				static_cast<uint32_t>(this->currentHeight),
				DXGI_FORMAT_R8G8B8A8_UNORM,
				0
				)))
			{
				throw std::exception("IDXGISwapChain1::ResizeBuffers failed.");
			}
		}
		else {
			DXGI_SWAP_CHAIN_DESC1 swapChainDesc = { 0 };
			swapChainDesc.Width = static_cast<UINT>(this->currentWidth);
			swapChainDesc.Height = static_cast<UINT>(this->currentHeight);
			swapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
			swapChainDesc.Stereo = false;
			swapChainDesc.SampleDesc.Count = 1;
			swapChainDesc.SampleDesc.Quality = 0;
			swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
			swapChainDesc.BufferCount = 2;
			swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
			swapChainDesc.Flags = 0;
			swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_UNSPECIFIED;

			ComPtr<IDXGIDevice1> dxgiDevice;
			if (FAILED(this->device.As(&dxgiDevice)))
			{
				throw std::exception("ID3D11Device1::As failed.");
			}
			ComPtr<IDXGIAdapter> dxgiAdapter;
			if (FAILED(dxgiDevice->GetAdapter(&dxgiAdapter)))
			{
				throw std::exception("IDXGIDevice1::GetAdapter failed.");
			}
			ComPtr<IDXGIFactory2> dxgiFactory;
			if (FAILED(dxgiAdapter->GetParent(IID_PPV_ARGS(&dxgiFactory))))
			{
				throw std::exception("IDXGIAdapter::GetParent failed.");
			}
			ComPtr<IDXGISwapChain1> swapChain;
			if (FAILED(dxgiFactory->CreateSwapChainForComposition(
				this->device.Get(),
				&swapChainDesc,
				nullptr,
				&swapChain
				)))
			{
				throw std::exception("IDXGIFActory2::CreateSwapChainForComposition failed.");
			}
			swapChain.As(&this->swapchain);

			if (FAILED(dxgiDevice->SetMaximumFrameLatency(MAX_FRAME_LATENCY)))
			{
				throw std::exception("IDXGIDevice1::SetMaximumFrameLatency failed.");
			}
			// bind the swap chain to the panel
			EmulatorComponent::Current->Dispatcher->RunAsync(CoreDispatcherPriority::Normal, ref new DispatchedHandler([=]()
			{
				ComPtr<ISwapChainPanelNative> panelNative;
				if (FAILED(reinterpret_cast<IUnknown *>(this->panel)->QueryInterface(IID_PPV_ARGS(&panelNative))))
				{
					throw std::exception("QueryInterface failed.");
				}
				if (FAILED(panelNative->SetSwapChain(this->swapchain.Get())))
				{
					throw std::exception("ISwapChainPanelNative::SetSwapChain failed.");
				}
			}, CallbackContext::Any));
		}

		if (FAILED(this->swapchain->GetBuffer(0, IID_PPV_ARGS(&this->backbuffer))))
		{
			throw std::exception("IDXGISwapChain2::GetBuffer failed.");
		}

		if (FAILED(this->device->CreateRenderTargetView(this->backbuffer.Get(), nullptr, this->backbufferRTV.GetAddressOf())))
		{
			throw std::exception("ID3D11Device1::CreateRenderTargetView failed.");
		}

		if(this->spriteBatch) {
			this->spriteBatch->OnResize(this->currentWidth, this->currentHeight);
		}

		this->viewport.Width = this->currentWidth;
		this->viewport.Height = this->currentHeight;
		this->viewport.TopLeftX = 0;
		this->viewport.TopLeftY = 0;

		this->context->OMSetRenderTargets(1, this->backbufferRTV.GetAddressOf(), nullptr);
		this->context->RSSetViewports(1, &this->viewport);
	}

	void Renderer::OnSizeChanged(Platform::Object ^sender, Windows::UI::Xaml::SizeChangedEventArgs ^e)
	{
		double scale = 1.0;

		this->Resize((float)(e->NewSize.Width * scale), (float)(e->NewSize.Height * scale));
	}

	void Renderer::Resize(float width, float height)
	{
		if (width != this->currentWidth || height != this->currentHeight)
		{
			this->SetSize(width, height);
			for (int i = 0; i < this->components.size(); i++)
			{
				this->components.at(i)->Resize(width, height);
			}
		}
		this->InitializeSizeDependentResources();
	}

	void Renderer::CreateRenderBuffers(uint32_t gameWidth, uint32_t gameHeight)
	{
		// create new ones
		D3D11_TEXTURE2D_DESC texDesc;
		ZeroMemory(&texDesc, sizeof(D3D11_TEXTURE2D_DESC));

		texDesc.Width = gameWidth;
		texDesc.Height = gameHeight;
		texDesc.ArraySize = 1;
		texDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
		texDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
		texDesc.Format = DXGI_FORMAT_B8G8R8X8_UNORM;
		texDesc.Usage = D3D11_USAGE_DYNAMIC;
		texDesc.SampleDesc.Count = 1;
		texDesc.SampleDesc.Quality = 0;
		texDesc.MipLevels = 1;

		for (int i = 0; i < ARRAYSIZE(this->renderTextures); i++)
		{
			if (FAILED(this->device->CreateTexture2D(&texDesc, nullptr, this->renderTextures[i].GetAddressOf())))
			{
				throw new std::exception("ID3D11Device1::CreateTexture2D failed.");
			}
			if (FAILED(this->device->CreateShaderResourceView(this->renderTextures[i].Get(), nullptr, this->renderTargetSRVs[i].GetAddressOf())))
			{
				throw new std::exception("ID3D11Device1::CreateShaderResourceView failed.");
			}
		}

		this->SwapBuffers();
	}

	void Renderer::SwapBuffers()
	{
		uint8_t backbuffer = this->frontBuffer;
		this->frontBuffer = (this->frontBuffer + 1) % 2;
		if (this->fbPtr && this->currentPitch > 0)
		{
			// unbind current buffer
			this->context->Unmap(this->renderTextures[this->frontBuffer].Get(), 0);
			this->currentPitch = 0;
			this->fbPtr = nullptr;
		}
		D3D11_MAPPED_SUBRESOURCE mapping;
		ZeroMemory(&mapping, sizeof(D3D11_MAPPED_SUBRESOURCE));
		if (FAILED(this->context->Map(this->renderTextures[backbuffer].Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &mapping)))
		{
			throw new std::exception("ID3D11DeviceContext1::Map failed.");
		}
		this->currentPitch = mapping.RowPitch;
		this->fbPtr = mapping.pData;
	}

	void Renderer::GetBackbufferPtr(void **ptr, uint32_t *rowPitch)
	{
		*ptr = this->fbPtr;
		*rowPitch = this->currentPitch;
	}

	Array<uint8_t> ^Renderer::GetSnapshot(int *rowPitch)
	{
		uint32_t gameWidth = EmulatorComponent::Current->RenderedWidth;
		uint32_t gameHeight = EmulatorComponent::Current->RenderedHeight;
		Platform::Array<unsigned char> ^buffer = ref new Platform::Array<unsigned char>((int)(gameWidth * gameHeight) * 4);
		uint8_t *backbufferPtr = (uint8_t *) this->fbPtr + this->currentPitch;

		*rowPitch = gameWidth * 4;
		for (int i = 0; i < gameHeight; i++)
		{
			for (int j = 0; j < gameWidth * 4; j += 4)
			{
				// red
				buffer[*rowPitch * i + j] = *(backbufferPtr + this->currentPitch * i + j + 2);

				// green
				buffer[*rowPitch * i + j + 1] = *(backbufferPtr + this->currentPitch * i + j + 1);

				// blue
				buffer[*rowPitch * i + j + 2] = *(backbufferPtr + this->currentPitch * i + j + 0);

				// alpha
				buffer[*rowPitch * i + j + 3] = 0xff;
			}
		}

		return buffer;
	}

	bool Renderer::AllowsComplexShaders::get()
	{
		return this->SupportsHigherShaders();
	}

	bool Renderer::Blur::get()
	{
		return this->blurring;
	}

	void Renderer::Blur::set(bool value)
	{
		this->blurring = value;
	}

	bool Renderer::SupportsHigherShaders()
	{
		return !(this->featureLevel == D3D_FEATURE_LEVEL::D3D_FEATURE_LEVEL_9_1);
	}

	void Renderer::FirstROMLoaded()
	{
		this->firstROM = true;
	}

	void Renderer::AddComponent(IRenderComponent ^component)
	{
		component->Renderer = this;
		this->components.push_back(component);
	}

	void Renderer::GameSystemResolutionChanged()
	{
		this->calculateTargetRectangle();
		for (int i = 0; i < this->components.size(); i++)
		{
			this->components.at(i)->Resize(this->currentWidth, this->currentHeight);
		}
	}

	void Renderer::SetSize(float width, float height)
	{
		if (width <= 0 || height <= 0)
		{
			this->currentWidth = DEFAULT_WIDTH;
			this->currentHeight = DEFAULT_HEIGHT;
		}
		else {
			this->currentWidth = (float)ceil(width);// +1;
			this->currentHeight = (float)ceil(height);// +1;
		}

		this->calculateTargetRectangle();
	}

	void Renderer::calculateTargetRectangle()
	{
		float viewAspect = this->currentWidth / this->currentHeight;

		auto calcFromAspect = [=](float aspect)
		{
			float width, height;

			if (viewAspect >= aspect)
			{
				// height limiting
				width = this->currentHeight * aspect;
				height = this->currentHeight;
			}
			else {
				// width limiting
				height = this->currentWidth / aspect;
				width = this->currentWidth;
			}

			float widthOffset = (this->currentWidth - width) / 2.0f;

			this->targetRect = Rectangle(
				widthOffset, 0,
				width,
				height
				);
		};

		float targetAspect = 0.0f;
		switch (settings->AspectRatio)
		{
		case ::EmulatorComponent::AspectRatio::Stretch:
			targetAspect = viewAspect;
			break;
		case ::EmulatorComponent::AspectRatio::One:
			targetAspect = 1.0f;
			break;
		case ::EmulatorComponent::AspectRatio::FiveToFour:
			targetAspect = 5.0f / 4.0f;
			break;
		case ::EmulatorComponent::AspectRatio::FourToThree:
			targetAspect = 4.0f / 3.0f;
			break;
		default:
		case ::EmulatorComponent::AspectRatio::Original:
			targetAspect = (float)EmulatorComponent::Current->RenderedWidth /
				(float)EmulatorComponent::Current->RenderedHeight;
			break;
		}
		calcFromAspect(targetAspect);

		if (this->currentWidth > this->currentHeight)
		{
			float scale = settings->VideoScale * 0.01f;
			float scaledWidth = this->targetRect.Width * scale;
			float scaledHeight = this->targetRect.Height * scale;
			float deltaWidth = this->targetRect.Width - scaledWidth;

			this->targetRect.X += (int)roundf(deltaWidth * 0.5f);
			this->targetRect.Width = (unsigned int)roundf(scaledWidth);
			this->targetRect.Height = (unsigned int)roundf(scaledHeight);
		}
	}

	void Renderer::Render(GameTime ^gameTime)
	{
		this->fps->Update(gameTime);
		if (this->fps->Changed)
		{
			this->FrameRateChanged((float) this->fps->FPS);
		}

		uint32_t sourceWidth = EmulatorComponent::Current->RenderedWidth;
		uint32_t sourceHeight = EmulatorComponent::Current->RenderedHeight;

		Rectangle sourceRect(0, 1, sourceWidth - 1, sourceHeight - 1);

		this->context->OMSetRenderTargets(
			1,
			this->backbufferRTV.GetAddressOf(),
			nullptr
			);

		Color renderColor(1.0f, 1.0f, 1.0f, 1.0f);
		
		if (this->settings->Theme == Windows::UI::Xaml::ApplicationTheme::Dark)
		{
			const float clearColor[] = { 0.0f, 0.0f, 0.0f, 1.0f };
			this->context->ClearRenderTargetView(this->backbufferRTV.Get(), clearColor);
		}
		else {
			const float clearColor[] = { 1.0f, 1.0f, 1.0f, 1.0f };
			this->context->ClearRenderTargetView(this->backbufferRTV.Get(), clearColor);
		}

		auto textureSRV = this->renderTargetSRVs[this->frontBuffer];
		auto texture = this->renderTextures[this->frontBuffer];

		XMMATRIX matrix = XMMatrixIdentity();

		SamplingMode oldMode;
		if (this->firstROM)
		{
			if (this->blurring)
			{
				oldMode = this->spriteBatch->GetTextureSampling();
				this->spriteBatch->SetTextureSampling(SamplingMode::Linear);
			}
			this->spriteBatch->Begin(matrix);
			this->spriteBatch->Draw(this->targetRect, &sourceRect, textureSRV.Get(), texture.Get(), renderColor);
			this->spriteBatch->End();
			if (this->blurring)
			{
				this->spriteBatch->SetTextureSampling(oldMode);
			}
		}
		else if(this->splashSRV && this->splashTexture && this->splashBounds.Width != 0 && this->splashBounds.Height != 0)
		{
			// show vba-x logo
			auto accentColor = this->settings->CurrentAccentColor;

			oldMode = this->spriteBatch->GetTextureSampling();
			this->spriteBatch->SetTextureSampling(SamplingMode::Linear);

			float width = this->targetRect.Width * 0.5f;
			Rectangle splashRectangle(
				this->targetRect.X + this->targetRect.Width * 0.25f,
				this->targetRect.Y + this->targetRect.Height * 0.25f,
				width,
				width / ((float) this->splashBounds.Width / (float) this->splashBounds.Height)
				);

			ComPtr<ID3D11Texture2D> tex;
			this->splashTexture.As(&tex);
			this->spriteBatch->Begin(matrix);
			this->spriteBatch->Draw(splashRectangle, &this->splashBounds, this->splashSRV.Get(), tex.Get(), Color(accentColor.R / 255.0f, accentColor.G / 255.0f, accentColor.B / 255.0f, 0.25f));
			this->spriteBatch->End();

			this->spriteBatch->SetTextureSampling(oldMode);
		}

		oldMode = this->spriteBatch->GetTextureSampling();
		this->spriteBatch->SetTextureSampling(SamplingMode::Linear);
		this->spriteBatch->Begin(matrix);
		for (int i = 0; i < this->components.size(); i++)
		{
			this->components.at(i)->Render(gameTime);
		}
		this->spriteBatch->End();
		this->spriteBatch->SetTextureSampling(oldMode);

		this->swapchain->Present(0, 0);
	}

	void Renderer::Suspend()
	{
		ComPtr<IDXGIDevice3> dev;
		this->device.As(&dev);
		if (dev)
		{
			dev->Trim();
		}
	}

	void Renderer::OnDeviceList()
	{
		// not implemented YET
	}
}

