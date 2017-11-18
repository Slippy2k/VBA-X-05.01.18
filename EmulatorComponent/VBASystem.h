#pragma once
#include "IGameSystem.h"
#include "Renderer.h"
#include "ROMConfig.h"
#include "ICheatCodeValidator.h"
#include "Controller.h"
#include <collection.h>
#include <System.h>

namespace EmulatorComponent
{
	ref class EmulatorComponent;

	ref class VBASaveInfo sealed
		: public ISaveInfo
	{
	internal:
		VBASaveInfo();

	public:
		virtual ~VBASaveInfo();

		property Platform::String ^SRAMExtension
		{
			virtual Platform::String ^get();
		}

		property Platform::String ^SaveStateExtension
		{
			virtual Platform::String ^get();
		}
	};

	ref class VBASystem
		: public IGameSystem
	{
	private:
		EmulatorComponent ^emulator;
		EmulatedSystem emulatedSystem;
		bool gbaROMLoaded;
		bool romLoaded;

		Renderer ^renderer;
		Controller ^controller;
		ISaveInfo ^info;
		int saveCounter;

		Windows::Foundation::Collections::IVector<CheatData ^> ^cheatData;

		CRITICAL_SECTION emulatingLock;

		Platform::Collections::Map<Platform::String ^, ROMConfig> ^romConfigs;

		void setSoundVolume();
		void SettingChanged(Platform::String ^settingName);

		Windows::Foundation::IAsyncAction ^LoadGBAROMAsync(IROMData ^file);
		Windows::Foundation::IAsyncAction ^LoadGBROMAsync(IROMData ^file);

		Windows::Foundation::IAsyncAction ^LoadGBASRAMAsync();
		Windows::Foundation::IAsyncAction ^LoadGBSRAMAsync();
		Windows::Foundation::IAsyncAction ^SaveGBASRAMAsync();
		Windows::Foundation::IAsyncAction ^SaveGBSRAMAsync();
		ByteWrapper ^GetGBASRAMData();
		ByteWrapper ^GetGBSRAMData();

		Windows::Foundation::IAsyncAction ^LoadGBAStateAsync(ByteWrapper ^bytes);
		Windows::Foundation::IAsyncAction ^LoadGBStateAsync(ByteWrapper ^bytes);
		ByteWrapper ^GetGBASaveStateData();
		ByteWrapper ^GetGBSaveStateData();
	internal:
		VBASystem(Controller ^controller);

	public:
		virtual ~VBASystem();

		property ISaveInfo ^SaveInfo
		{
			virtual ISaveInfo ^get();
		}

		property uint32_t DisplayWidth
		{
			virtual uint32_t get();
		}

		property uint32_t DisplayHeight
		{
			virtual uint32_t get();
		}

		property uint32_t CurrentRenderAreaWidth
		{
			virtual uint32_t get();
		}

		property uint32_t CurrentRenderAreaHeight
		{
			virtual uint32_t get();
		}

		property bool IsEmulating
		{
			virtual bool get();
			virtual void set(bool value);
		}

		property int FrameSkip
		{
			virtual int get();
			virtual void set(int value);
		}

		property int TurboFrameSkip
		{
			virtual int get();
			virtual void set(int value);
		}

		property bool ROMLoaded
		{
			virtual bool get();
		private:
			void set(bool value);
		}

		property ICheatCodeValidator ^CheatValidator
		{
			virtual ICheatCodeValidator ^get();
		}

		property ICheatManager ^CheatManager
		{
			virtual ICheatManager ^get();
		}

		virtual Windows::Foundation::IAsyncAction ^LoadROMAsync(IROMData ^rom);
		virtual Windows::Foundation::IAsyncAction ^ParseAdditionalConfigAsync(Platform::String ^content);
		virtual Windows::Foundation::IAsyncAction ^ApplyCheatsAsync();
		virtual Windows::Foundation::IAsyncAction ^StopROMAsync();

		virtual Windows::Foundation::IAsyncAction ^LoadSRAMAsync();
		virtual Windows::Foundation::IAsyncAction ^SaveSRAMAsync();
		virtual ByteWrapper ^GetSRAMData();

		virtual Windows::Foundation::IAsyncAction ^LoadStateAsync(ByteWrapper ^bytes);
		virtual ByteWrapper ^GetSaveStateData();

		virtual void InitSound();
		virtual void StopSound();

		virtual void SetRenderer(Renderer ^renderer);
		virtual void SwapBuffers();
		virtual void Update();

		// Inherited via ICheatCodeValidator
		virtual bool CheckCode(Platform::String ^ code, Windows::Foundation::Collections::IVector<Platform::String ^> ^singleCodes);

		// Inherited via IGameSystem
		virtual void ApplyCheats(Windows::Foundation::Collections::IVector<CheatData^>^ cheats);

		// Inherited via IGameSystem
		virtual void ReapplyCheats();
};
}