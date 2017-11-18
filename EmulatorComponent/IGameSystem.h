#pragma once
#include <stdint.h>
#include "Rectangle.h"
#include "IROMData.h"
#include "ISaveInfo.h"
#include "ISaveProvider.h"
#include "ICheatCodeValidator.h"
#include "ICheatManager.h"

namespace EmulatorComponent
{
	ref class Renderer;

	interface class IGameSystem
		: ICheatCodeValidator, ICheatManager
	{
		property ISaveInfo ^SaveInfo
		{
			ISaveInfo ^get();
		}

		property uint32_t DisplayWidth
		{
			uint32_t get();
		}

		property uint32_t DisplayHeight
		{
			uint32_t get();
		}

		property uint32_t CurrentRenderAreaWidth
		{
			uint32_t get();
		}

		property uint32_t CurrentRenderAreaHeight
		{
			uint32_t get();
		}

		property int FrameSkip
		{
			int get();
			void set(int value);
		}

		property int TurboFrameSkip
		{
			int get();
			void set(int value);
		}

		property bool IsEmulating
		{
			bool get();
			void set(bool value);
		}

		property bool ROMLoaded
		{
			bool get();
		}

		property ICheatManager ^CheatManager
		{
			ICheatManager ^get();
		}

		Windows::Foundation::IAsyncAction ^ParseAdditionalConfigAsync(Platform::String ^content);
		Windows::Foundation::IAsyncAction ^LoadROMAsync(IROMData ^rom);
		Windows::Foundation::IAsyncAction ^ApplyCheatsAsync();
		Windows::Foundation::IAsyncAction ^StopROMAsync();

		Windows::Foundation::IAsyncAction ^LoadSRAMAsync();
		Windows::Foundation::IAsyncAction ^SaveSRAMAsync();
		ByteWrapper ^GetSRAMData();

		Windows::Foundation::IAsyncAction ^LoadStateAsync(ByteWrapper ^bytes);
		ByteWrapper ^GetSaveStateData();

		void InitSound();
		void StopSound();

		void SetRenderer(Renderer ^renderer);
		void SwapBuffers();
		void Update();
	};
}