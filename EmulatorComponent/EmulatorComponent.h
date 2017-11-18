#pragma once
#include "Renderer.h"
#include "GameTime.h"
#include "IGameSystem.h"
#include "ISettings.h"
#include "Controller.h"
#include "IInputChannel.h"
#include "IROMData.h"
#include "ISaveProvider.h"
#include "VirtualPad.h"
#include "ICheatCodeValidator.h"
#include "AutosaveCounter.h"

namespace EmulatorComponent
{
	public enum class DeviceType : int
	{
		Default,
		Mobile
	};

	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class EmulatorComponent sealed
	{
	private:
		static EmulatorComponent ^current;

		Windows::UI::Xaml::Controls::SwapChainPanel ^panel;
		Windows::UI::Core::CoreDispatcher ^dispatcher;
		Windows::Foundation::EventRegistrationToken renderEventToken;

		VirtualPad ^vpad;

		ISettings ^settings;
		IGameSystem ^system;
		Controller ^controller;
		Renderer ^renderer;
		GameTime ^gameTime;
		AutosaveCounter autosaveCounter;
		int framesSkipped;

		ISaveProvider ^saveProvider;

		DeviceType deviceType;

		Windows::Foundation::IAsyncAction ^updateThread;
		HANDLE threadEndEvent;
		bool stopThread;
		bool threadInitialized;

		void OnRendering(Object^ sender, Object^ args);
		void InitializeThread();
		void ShutdownThread();

		void UpdateAsync();
	internal:
		property VirtualPad ^VPad
		{
			VirtualPad ^get();
		}

	public:
		static property EmulatorComponent ^Current
		{
			EmulatorComponent ^get();
		}

		property Renderer ^RenderComponent
		{
			Renderer ^get();
		}

		property ISaveProvider ^SaveProvider
		{
			ISaveProvider ^get();
			void set(ISaveProvider ^value);
		}

		property ISaveInfo ^SaveInfo
		{
			ISaveInfo ^get();
		}

		property ISettings ^Settings
		{
			ISettings ^get();
		}

		property uint32_t RenderedWidth
		{
			uint32_t get();
		}

		property uint32_t RenderedHeight
		{
			uint32_t get();
		}

		property bool ROMLoaded
		{
			bool get();
		}

		property bool IsPaused
		{
			bool get();
		}

		property ICheatCodeValidator ^CheatValidator
		{
			ICheatCodeValidator ^get();
		}

		property ICheatManager ^CheatManager
		{
			ICheatManager ^get();
		}

		property Windows::UI::Core::CoreDispatcher^ Dispatcher
		{
			Windows::UI::Core::CoreDispatcher ^get();
		}

		property Windows::UI::Xaml::Controls::SwapChainPanel ^Panel
		{
			Windows::UI::Xaml::Controls::SwapChainPanel ^get();
		}

		property Controller ^GameController
		{
			::EmulatorComponent::Controller ^get();
		}

		property ITouchInputHandler ^TouchHandler
		{
			ITouchInputHandler ^get();
		}

		EmulatorComponent(
			ISettings ^settings,
			Windows::UI::Xaml::Controls::SwapChainPanel ^panel,
			Windows::UI::Core::CoreDispatcher ^dispatcher,
			DeviceType deviceType
			);
		virtual ~EmulatorComponent();

		Windows::Foundation::IAsyncAction ^LoadConfigAsync(Platform::String ^content);
		Windows::Foundation::IAsyncAction ^LoadROMAsync(IROMData ^rom);
		Windows::Foundation::IAsyncAction ^StopROMAsync();

		Windows::Foundation::IAsyncAction ^SaveSRAMAsync();
		ByteWrapper ^GetSRAMData();

		Windows::Foundation::IAsyncAction ^LoadSaveStateAsync(ByteWrapper ^data);
		ByteWrapper ^GetSaveStateData();

		ByteWrapper ^GetSnapshot(int *rowPitch);

		bool Pause();
		bool Unpause();

		void Suspend();
		void Resume(bool isOnGamePage);

		void Minimized();
		void Maximized(bool isOnGamePage);
	};
}