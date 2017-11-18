#include "pch.h"
#include "EmulatorComponent.h"
#include "VBASystem.h"
#include <ppltasks.h>

using namespace concurrency;

#if _DEBUG
#include <sstream>
#endif

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Core;
using namespace Windows::System::Threading;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Microsoft::WRL;
using namespace std;

CRITICAL_SECTION pauseSync;
HANDLE swapEvent;
HANDLE updateEvent;

namespace EmulatorComponent
{
	EmulatorComponent ^EmulatorComponent::current = nullptr;

	EmulatorComponent::EmulatorComponent(
		ISettings ^settings,
		SwapChainPanel ^panel,
		CoreDispatcher ^dispatcher,
		DeviceType deviceType)
		: panel(panel), gameTime(ref new GameTime()),
		stopThread(false), threadInitialized(false),
		settings(settings), deviceType(deviceType)
	{
		current = this;
		this->dispatcher = dispatcher;

		this->controller = ref new Controller(this->deviceType != DeviceType::Mobile);
		this->system = ref new VBASystem(this->controller);
		this->renderer = ref new Renderer(panel, this->system->DisplayWidth, this->system->DisplayHeight);
		this->system->SetRenderer(this->renderer);
		this->vpad = ref new VirtualPad();
		this->controller->AddInputChannel(this->vpad);
		this->renderer->AddComponent(this->vpad);
		this->renderEventToken = CompositionTarget::Rendering += ref new EventHandler<Object^>(this, &EmulatorComponent::OnRendering);
		this->InitializeThread();
	}

	EmulatorComponent::~EmulatorComponent()
	{
		CompositionTarget::Rendering -= this->renderEventToken;
		this->ShutdownThread();
		CompositionTarget::Rendering::remove(this->renderEventToken);
	}

	IAsyncAction ^EmulatorComponent::LoadConfigAsync(Platform::String ^content)
	{
		return create_async([=]() {
			create_task(this->system->ParseAdditionalConfigAsync(content)).wait();
		});
	}

	IAsyncAction ^EmulatorComponent::LoadROMAsync(IROMData ^rom)
	{
		this->renderer->FirstROMLoaded();
		return create_async([=]()
		{
			return create_task(this->system->LoadROMAsync(rom));
		});
	}


	IAsyncAction ^EmulatorComponent::StopROMAsync()
	{
		return this->system->StopROMAsync();
	}

	IAsyncAction ^EmulatorComponent::SaveSRAMAsync()
	{
		return this->system->SaveSRAMAsync();
	}

	ByteWrapper ^EmulatorComponent::GetSRAMData()
	{
		return this->system->GetSRAMData();
	}

	ByteWrapper ^EmulatorComponent::GetSaveStateData()
	{
		return this->system->GetSaveStateData();
	}


	ByteWrapper ^EmulatorComponent::GetSnapshot(int *pitch)
	{
		if (!this->ROMLoaded)
		{
			return nullptr;
		}
		bool paused = this->Pause();

		Array<uint8_t> ^arr = this->renderer->GetSnapshot(pitch);
		ByteWrapper ^bytes = ref new ByteWrapper(arr);

		if (paused)
		{
			this->Unpause();
		}

		return bytes;
	}

	IAsyncAction ^EmulatorComponent::LoadSaveStateAsync(ByteWrapper ^data)
	{
		return this->system->LoadStateAsync(data);
	}

	void EmulatorComponent::InitializeThread()
	{
		if (this->threadInitialized)
		{
			return;
		}
		InitializeCriticalSectionEx(&pauseSync, NULL, NULL);
		this->threadEndEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
		swapEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
		updateEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
		this->stopThread = false;

		// pause thread at first
		EnterCriticalSection(&pauseSync);

		this->updateThread = ThreadPool::RunAsync(ref new WorkItemHandler([this](IAsyncAction ^action) {
#if _DEBUG
			OutputDebugStringW(L"Thread started.\n");
#endif
			this->UpdateAsync();
			SetEvent(this->threadEndEvent);
#if _DEBUG
			OutputDebugStringW(L"Thread ended.\n");
#endif
		}), WorkItemPriority::High, WorkItemOptions::None);

		this->threadInitialized = true;
	}

	void EmulatorComponent::ShutdownThread()
	{
		if (!this->threadInitialized)
		{
			return;
		}

		this->stopThread = true;

		// let emulator run to end of thread
		LeaveCriticalSection(&pauseSync);
		ResetEvent(swapEvent);
		SetEvent(updateEvent);

		// wait for end
		WaitForSingleObjectEx(this->threadEndEvent, INFINITE, false);

		// close all handles
		CloseHandle(this->threadEndEvent);
		CloseHandle(swapEvent);
		CloseHandle(updateEvent);
		DeleteCriticalSection(&pauseSync);
		this->updateThread = nullptr;

		this->threadInitialized = false;
	}

	ISettings ^EmulatorComponent::Settings::get()
	{
		return this->settings;
	}

	uint32_t EmulatorComponent::RenderedWidth::get()
	{
		return this->system->CurrentRenderAreaWidth;
	}

	uint32_t EmulatorComponent::RenderedHeight::get()
	{
		return this->system->CurrentRenderAreaHeight;
	}

	bool EmulatorComponent::ROMLoaded::get()
	{
		return this->system->ROMLoaded;
	}

	bool EmulatorComponent::IsPaused::get()
	{
		return !this->system->IsEmulating;
	}

	ICheatCodeValidator ^EmulatorComponent::CheatValidator::get()
	{
		return this->system->CheatManager->CheatValidator;
	}

	ICheatManager ^EmulatorComponent::CheatManager::get()
	{
		return this->system->CheatManager;
	}

	CoreDispatcher ^EmulatorComponent::Dispatcher::get()
	{
		return this->dispatcher;
	}

	EmulatorComponent ^EmulatorComponent::Current::get()
	{
		return EmulatorComponent::current;
	}

	Renderer ^EmulatorComponent::RenderComponent::get()
	{
		return this->renderer;
	}

	VirtualPad ^EmulatorComponent::VPad::get()
	{
		return this->vpad;
	}

	ISaveProvider ^EmulatorComponent::SaveProvider::get()
	{
		return this->saveProvider;
	}

	void EmulatorComponent::SaveProvider::set(ISaveProvider ^value)
	{
		this->saveProvider = value;
	}

	ISaveInfo ^EmulatorComponent::SaveInfo::get()
	{
		return this->system->SaveInfo;
	}

	SwapChainPanel ^EmulatorComponent::Panel::get()
	{
		return this->panel;
	}

	::EmulatorComponent::Controller ^EmulatorComponent::GameController::get()
	{
		return this->controller;
	}

	ITouchInputHandler ^EmulatorComponent::TouchHandler::get()
	{
		return this->vpad;
	}

	void EmulatorComponent::UpdateAsync()
	{
		while (!this->stopThread)
		{
			EnterCriticalSection(&pauseSync);

			this->system->Update();

			LeaveCriticalSection(&pauseSync);
		}
	}

	void EmulatorComponent::OnRendering(Object^ sender, Object^ args)
	{
		RenderingEventArgs ^renderingArgs = (RenderingEventArgs ^)args;
		this->gameTime->Update(renderingArgs->RenderingTime);

		this->controller->Update();

		if (this->framesSkipped < this->system->FrameSkip)
		{
			this->framesSkipped++;
		}
		else {
			this->framesSkipped = 0;

			if (this->system->IsEmulating)
			{
				this->autosaveCounter.Update(this->gameTime->ElapsedSeconds);

				WaitForSingleObjectEx(swapEvent, INFINITE, false);
				this->renderer->SwapBuffers();
				this->system->SwapBuffers();
				SetEvent(updateEvent);
			}

			this->renderer->Render(this->gameTime);
		}
	}

	bool EmulatorComponent::Pause()
	{
		if (this->system->IsEmulating) {
#if _DEBUG
			OutputDebugStringW(L"PAUSE\n");
#endif
			EnterCriticalSection(&pauseSync);
			this->system->IsEmulating = false;
			return true;
		}
		return false;
	}

	bool EmulatorComponent::Unpause()
	{
		if (!this->system->IsEmulating && this->system->ROMLoaded) {
#if _DEBUG
			OutputDebugStringW(L"UNPAUSE\n");
#endif
			this->system->IsEmulating = true;
			LeaveCriticalSection(&pauseSync);
			return true;
		}
		return false;
	}

	void EmulatorComponent::Suspend()
	{
#if _DEBUG
		OutputDebugStringW(L"Suspend: stopping emulator.\n");
#endif
		this->Pause();
		this->renderer->Suspend();
	}

	void EmulatorComponent::Resume(bool isOnGamePage)
	{
#if _DEBUG
		OutputDebugStringW(L"Resume: starting emulator.\n");
#endif
		if (isOnGamePage)
		{
			this->Unpause();
		}
	}

	void EmulatorComponent::Minimized()
	{
#if _DEBUG
		OutputDebugStringW(L"Window minimized.\n");
#endif
		this->Pause();
		this->system->StopSound();
	}

	void EmulatorComponent::Maximized(bool isOnGamePage)
	{
#if _DEBUG
		OutputDebugStringW(L"Window maximized.\n");
#endif
		this->system->InitSound();
		if (isOnGamePage)
		{
			this->Unpause();
		}
	}
}
