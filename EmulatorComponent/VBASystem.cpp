#include "pch.h"
#include "VBASystem.h"
#include "stringhelper.h"
#include "EmulatorComponent.h"
#include "VirtualPad.h"
#include <stdint.h>
#include <ppltasks.h>
#include <collection.h>
#include <vector>
#include <sstream>
#include <string>
#include <xstring>

#include <System.h>
#include <NLS.h>
#include <Port.h>
#include <GBA.h>
#include <GB.h>
#include <Globals.h>
#include <RTC.h>
#include <robuffer.h>
#include <Util.h>
#include <Gb_Apu.h>
#include <Sound.h>
#include <gbSound.h>
#include <gbMemory.h>
#include <gbCheats.h>
#include <Cheats.h>
#include <SoundDriver.h>

#define STORE_IN_ARRAY(arr,data,size,offset)		\
memcpy(arr + offset, data, size);					\
offset += size;

#define RESTORE_FROM_ARRAY(arr,dst,size,offset)		\
memcpy(dst, arr + offset, size);					\
offset += size;

using namespace std;
using namespace Platform;
using namespace Platform::Collections;
using namespace concurrency;
using namespace Windows::Foundation;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Windows::Foundation::Collections;

#define DISPLAY_WIDTH	240
#define DISPLAY_HEIGHT	161

extern CRITICAL_SECTION pauseSync;
extern HANDLE swapEvent;
extern HANDLE updateEvent;

extern SoundDriver *soundDriver;
extern long  soundSampleRate;
extern int gbaSaveType;
extern int gbBattery;
extern int gbRomType;
extern u8 *gbRam;
extern int gbRamSizeMask;
extern u8 *gbMemoryMap[16];
extern u8 *gbMemory;
extern int gbTAMA5ramSize;
extern u8 *gbTAMA5ram;

extern Gb_Apu *gb_apu;
extern gb_apu_state_ss state;
extern variable_desc saveGameStruct[116];
extern variable_desc eepromSaveData[8];
extern variable_desc flashSaveData3[6];
extern variable_desc gba_state[32];
extern RTCCLOCKDATA rtcClockData;
extern bool stopState;
extern int IRQTicks;
extern int dummy_state[16];
extern bool intState;

extern u8* gbRom;
extern bool useBios;
extern bool inBios;
extern variable_desc gbSaveGameStruct[78];
extern u16 IFF;
extern int gbSgbMode;
extern variable_desc gbSgbSaveStructV3[11];
extern u8 *gbSgbBorder;
extern u8 *gbSgbBorderChar;
extern u8 gbSgbPacket[112];
extern u16 gbSgbSCPPalette[2048];
extern u8 gbSgbATF[360];
extern u8 gbSgbATFList[16200];
extern mapperMBC1 gbDataMBC1;
extern mapperMBC2 gbDataMBC2;
extern mapperMBC3 gbDataMBC3;
extern mapperMBC5 gbDataMBC5;
extern mapperHuC1 gbDataHuC1;
extern mapperHuC3 gbDataHuC3;
extern mapperTAMA5 gbDataTAMA5;
extern u8 *gbTAMA5ram;
extern int gbTAMA5ramSize;
extern mapperMMM01 gbDataMMM01;
extern u16 gbPalette[128];
extern u8 *gbMemory;
extern int gbRamSize;
extern u8 *gbRam;
extern int gbCgbMode;
extern u8 *gbVram;
extern u8 *gbWram;
extern variable_desc gb_state[20];
extern int gbCheatNumber;
extern gbCheat gbCheatList[100];
extern int gbLcdModeDelayed;
extern int gbLcdTicksDelayed;
extern int gbLcdLYIncrementTicksDelayed;
extern u8 gbSpritesTicks[300];
extern bool gbTimerModeChange;
extern bool gbTimerOnChange;
extern int gbHardware;
extern bool gbBlackScreen;
extern u8 oldRegister_WY;
extern int gbWindowLine;
extern int inUseRegister_WY;
extern bool gbScreenOn;
extern int gbSgbMask;
extern u8 gbSCYLine[300], register_SCY, gbSCXLine[300], register_SCX;
extern u8 gbBgpLine[300];
extern u8 gbBgp[4];
extern u8 gbObp0Line[300], gbObp0[4], gbObp1Line[300], gbObp1[4];
extern u8 register_SVBK, register_VBK;
extern int gbBorderOn;
extern int gbSpeed, gbLine99Ticks;

extern void soundSetVolume(float);
extern void reset_apu();
extern void write_SGCNT0_H(int data);
extern void apply_muting();
extern void CPUUpdateWindow0();
extern void CPUUpdateWindow1();
extern void sramWrite(u32, u8);
extern void CPUReadHelper(void);

int emulating;
size_t gbaPitch;
int turboSkip = 5;
extern int romSize;

bool inSystemDrawScreen = false;
bool romJustSwitched = false;

EmulatorComponent::Controller ^currentController;

namespace EmulatorComponent
{
	VBASaveInfo::VBASaveInfo()
	{ }

	VBASaveInfo::~VBASaveInfo()
	{ }

	Platform::String ^VBASaveInfo::SRAMExtension::get()
	{
		return ".sav";
	}

	Platform::String ^VBASaveInfo::SaveStateExtension::get()
	{
		return "{0}.sgm";
	}

	ISaveInfo ^VBASystem::SaveInfo::get()
	{
		return this->info;
	}

	uint32_t VBASystem::DisplayWidth::get()
	{
		return DISPLAY_WIDTH;
	}

	uint32_t VBASystem::DisplayHeight::get()
	{
		return DISPLAY_HEIGHT;
	}

	bool VBASystem::ROMLoaded::get()
	{
		return this->romLoaded;
	}

	void VBASystem::ROMLoaded::set(bool value)
	{
		this->romLoaded = value;
	}

	ICheatCodeValidator ^VBASystem::CheatValidator::get()
	{
		return this;
	}

	ICheatManager ^VBASystem::CheatManager::get()
	{
		return this;
	}

	uint32_t VBASystem::CurrentRenderAreaWidth::get()
	{
		if (this->gbaROMLoaded)
		{
			return 240;
		}
		return 160;
	}

	uint32_t VBASystem::CurrentRenderAreaHeight::get()
	{
		if (this->gbaROMLoaded)
		{
			return 160;
		}
		return 144;
	}

	bool VBASystem::IsEmulating::get()
	{
		EnterCriticalSection(&this->emulatingLock);
		bool result = emulating == 1;
		LeaveCriticalSection(&this->emulatingLock);
		return result;
	}

	void VBASystem::IsEmulating::set(bool value)
	{
		EnterCriticalSection(&this->emulatingLock);
		emulating = value ? 1 : 0;
		LeaveCriticalSection(&this->emulatingLock);

		this->setSoundVolume();
	}

	int VBASystem::FrameSkip::get()
	{
		return systemFrameSkip;
	}

	void VBASystem::FrameSkip::set(int value)
	{
		systemFrameSkip = value;
	}

	int VBASystem::TurboFrameSkip::get()
	{
		return turboSkip;
	}

	void VBASystem::TurboFrameSkip::set(int value)
	{
		turboSkip = value;
	}

	VBASystem::VBASystem(Controller ^controller)
		: gbaROMLoaded(true), controller(controller), romLoaded(false),
		info(ref new VBASaveInfo()),
		emulator(EmulatorComponent::Current)
	{
		InitializeCriticalSectionEx(&this->emulatingLock, NULL, NULL);

		systemColorDepth = 32;
		systemRedShift = 19;
		systemBlueShift = 3;
		systemGreenShift = 11;

		systemFrameSkip = this->emulator->Settings->FrameSkip;
		turboSkip = this->emulator->Settings->TurboFrameSkip;

		utilUpdateSystemColorMaps();

		currentController = controller;

		this->emulator->Settings->PropertyChanged += ref new Windows::UI::Xaml::Data::PropertyChangedEventHandler(
			[=](Object ^sender, Windows::UI::Xaml::Data::PropertyChangedEventArgs ^args)
		{
			this->SettingChanged(args->PropertyName);
		});
	}

	VBASystem::~VBASystem()
	{
		DeleteCriticalSection(&this->emulatingLock);
	}

	void VBASystem::setSoundVolume()
	{
		float volume = EmulatorComponent::Current->Settings->SoundVolume * 0.01f;
		soundSetVolume(volume);
	}

	void VBASystem::SettingChanged(Platform::String ^settingName)
	{
		if (settingName->Equals(L"EnableSound") ||
			settingName->Equals(L"SyncAudio"))
		{
			soundInit();
		}
		else if (settingName->Equals(L"FrameSkip"))
		{
			systemFrameSkip = this->emulator->Settings->FrameSkip;
			turboSkip = this->emulator->Settings->FrameSkip + this->emulator->Settings->TurboFrameSkip;
		}
		else if (settingName->Equals(L"TurboFrameSkip"))
		{
			turboSkip = this->emulator->Settings->FrameSkip + this->emulator->Settings->TurboFrameSkip;
		}
		else if (settingName->Equals(L"SoundVolume"))
		{
			this->setSoundVolume();
		}
	}

	void VBASystem::InitSound()
	{
		if (soundDriver)
		{
			soundDriver->init(soundSampleRate);
			if (EmulatorComponent::Current->Settings->EnableSound)//EmulatorSettings::Current->SoundEnabled)
			{
				this->setSoundVolume();
			}
			else
			{
				soundSetVolume(0.0f);
			}
		}
	}

	void VBASystem::StopSound()
	{
		if (soundDriver)
		{
			soundDriver->close();
		}
	}

	IAsyncAction ^VBASystem::LoadROMAsync(IROMData ^rom)
	{
		bool gba = false;
		String ^extension = rom->FileExtension;
		if (extension == nullptr)
		{
			throw ref new Exception(-1, L"Invalid rom file.");
		}

		const wchar_t *wstr = extension->Begin();
		if (extension->Length() == 3)
		{
			gba = ((wstr[0] == 'g' || wstr[0] == 'G') &&
				(wstr[1] == 'b' || wstr[1] == 'B') &&
				(wstr[2] == 'a' || wstr[2] == 'A'));
		}

		this->cheatData = nullptr;

		return gba ?
			this->LoadGBAROMAsync(rom) :
			this->LoadGBROMAsync(rom);
	}

	IAsyncAction ^VBASystem::LoadGBAROMAsync(IROMData ^data)
	{
		return create_async([=]()
		{
			return create_task([=]()
			{
				uint8_t *romBytes = new uint8_t[data->Data->Length];
				Array<uint8_t> ^bytes = data->Data;
				int test = data->Data->Length;
				for (int i = 0; i < bytes->Length; i++)
				{
					romBytes[i] = bytes[i];
				}

				int size = 0x2000000;

				if (rom != NULL) {
					CPUCleanUp();
				}

				systemSaveUpdateCounter = SYSTEM_SAVE_NOT_UPDATED;

				rom = (u8 *)malloc(0x2000000);
				if (rom == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"ROM");
				}
				workRAM = (u8 *)calloc(1, 0x40000);
				if (workRAM == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"WRAM");
				}

				u8 *whereToLoad = rom;
				if (cpuIsMultiBoot)
					whereToLoad = workRAM;

				int read = size = data->Size < size ? data->Size : size;
				memcpy_s(whereToLoad, read, romBytes, read);

				u16 *temp = (u16 *)(rom + ((size + 1)&~1));
				int i;
				for (i = (size + 1)&~1; i < 0x2000000; i += 2) {
					WRITE16LE(temp, (i >> 1) & 0xFFFF);
					temp++;
				}

				bios = (u8 *)calloc(1, 0x4000);
				if (bios == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"BIOS");
					CPUCleanUp();
				}
				internalRAM = (u8 *)calloc(1, 0x8000);
				if (internalRAM == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"IRAM");
					CPUCleanUp();
				}
				paletteRAM = (u8 *)calloc(1, 0x400);
				if (paletteRAM == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"PRAM");
					CPUCleanUp();
				}
				vram = (u8 *)calloc(1, 0x20000);
				if (vram == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"VRAM");
					CPUCleanUp();
				}
				oam = (u8 *)calloc(1, 0x400);
				if (oam == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"OAM");
					CPUCleanUp();
				}

				/*pix = (u8 *)calloc(1, 4 * 241 * 162);
				if(pix == NULL) {
				systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
				"PIX");
				CPUCleanUp();
				}
				extern size_t gbaPitch;
				gbaPitch = 964;*/

				ioMem = (u8 *)calloc(1, 0x400);
				if (ioMem == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"IO");
					CPUCleanUp();
				}

				memset(flashSaveMemory, 0xff, sizeof(flashSaveMemory));
				memset(eepromData, 255, sizeof(eepromData));

				extern void CPUUpdateRenderBuffers(bool);
				CPUUpdateRenderBuffers(true);

				romSize = read;

				if (romBytes)
				{
					delete[] romBytes;
				}

				// read from vba-over.ini
				Map<String ^, ROMConfig> ^configs = this->romConfigs;

				if (configs != nullptr)
				{
					char buffer[5];
					strncpy_s(buffer, (const char *)&rom[0xac], 4);
					buffer[4] = 0;

					string codeA = string(buffer);
					String ^code = ref new String(wstring(codeA.begin(), codeA.end()).c_str());
#if _DEBUG
					stringstream ss;
					ss << "Game code: ";
					ss << codeA << "\n";
					OutputDebugStringA(ss.str().c_str());
#endif
					if (configs->HasKey(code))
					{
						ROMConfig config = configs->Lookup(code);
						if (config.flashSize != -1)
						{
							flashSetSize(config.flashSize);
						}
						if (config.mirroringEnabled != -1)
						{
							doMirroring(config.mirroringEnabled != 0);
						}
						if (config.rtcEnabled != -1)
						{
							rtcEnable(config.rtcEnabled != 0);
						}
						if (config.saveType != -1)
						{
							cpuSaveType = config.saveType;
						}
					}
				}

				skipBios = true;

				this->emulatedSystem = GBASystem;

				soundInit();

				CPUInit(nullptr, false);
				CPUReset();

				if (!this->gbaROMLoaded)
				{
					// switched from gb to gba
					this->gbaROMLoaded = true;
					emulator->VPad->EnableShoulderButtons(true);
					this->renderer->GameSystemResolutionChanged();
					if (inSystemDrawScreen)
					{
						romJustSwitched = true;
					}
				}
				this->ROMLoaded = true;
			}).then([=]()
			{
				return LoadSRAMAsync();
			}).then([=]()
			{
				emulator->Unpause();
				this->IsEmulating = true;
			}).then([=]()
			{
				return ApplyCheatsAsync();
			}).then([=](task<void> t)
			{
				try
				{
					t.get();
				}
				catch (COMException ^ex)
				{
					this->ROMLoaded = false;
//#if _DEBUG
					Platform::String ^str = ex->Message;
					wstring wstr(str->Begin(), str->End());
					OutputDebugStringW(wstr.c_str());
					//EngineLog(LOG_LEVEL::Error, wstr);
//#endif
				}
			});
		});
	}

	IAsyncAction ^VBASystem::LoadGBROMAsync(IROMData ^romData)
	{
		return create_async([=]()
		{
			//EmulatorGame *emulator = EmulatorGame::GetInstance();

			return create_task([=]()
			{
				Array<uint8_t> ^bytes = romData->Data;
				uint8_t *romBytes = new uint8_t[romData->Data->Length];
				for (int i = 0; i < bytes->Length; i++)
				{
					romBytes[i] = bytes[i];
				}

				int size = romData->Data->Length;

				if (rom != NULL) {
					CPUCleanUp();
				}

				systemSaveUpdateCounter = SYSTEM_SAVE_NOT_UPDATED;

				extern u8 *gbRom;
				extern int gbRomSize;
				extern bool gbBatteryError;
				extern int gbHardware;
				extern int gbBorderOn;
				extern int gbCgbMode;

				for (int i = 0; i < 24;)
				{
					systemGbPalette[i++] = (0x1f) | (0x1f << 5) | (0x1f << 10);
					systemGbPalette[i++] = (0x15) | (0x15 << 5) | (0x15 << 10);
					systemGbPalette[i++] = (0x0c) | (0x0c << 5) | (0x0c << 10);
					systemGbPalette[i++] = 0;
				}

				gbRom = (u8 *)malloc(size);
				if (gbRom == NULL) {
					systemMessage(MSG_OUT_OF_MEMORY, N_("Failed to allocate memory for %s"),
						"ROM");
					CPUCleanUp();
				}

				memcpy_s(gbRom, size, romBytes, size);

				if (romBytes)
				{
					delete[] romBytes;
				}

				gbRomSize = size;
				gbBatteryError = false;

				if (bios != NULL) {
					free(bios);
					bios = NULL;
				}
				bios = (u8 *)calloc(1, 0x100);

				gbUpdateSizes();
				gbGetHardwareType();

				gbReset();
				this->emulatedSystem = GBSystem;
				gbBorderOn = false;

				soundInit();
				gbSoundReset();

				if (this->gbaROMLoaded)
				{
					// switched from gba to gbc
					this->gbaROMLoaded = false;
					this->renderer->GameSystemResolutionChanged();
					emulator->VPad->EnableShoulderButtons(false);

					if (inSystemDrawScreen)
					{
						romJustSwitched = true;
					}
				}
				this->ROMLoaded = true;

				return;
			}).then([=]()
			{
				return create_task(LoadSRAMAsync());
			}).then([=]()
			{
				emulator->Unpause();
				this->IsEmulating = true;
			}).then([=]()
			{
				return ApplyCheatsAsync();
			}).then([=](task<void> t)
			{
				try
				{
					t.get();
				}
				catch (COMException ^ex)
				{
					this->ROMLoaded = false;
#if _DEBUG
					Platform::String ^str = ex->Message;
					wstring wstr(str->Begin(), str->End());
					OutputDebugStringW(wstr.c_str());
					//EngineLog(LOG_LEVEL::Error, wstr);
#endif
				}
			});
		});
	}

	IAsyncAction ^VBASystem::SaveSRAMAsync()
	{
		if (!this->romLoaded)
		{
			return create_async([]() {});
		}

		if (this->emulator == nullptr || this->emulator->SaveProvider == nullptr)
		{
			return create_async([]() {});
		}

		return this->gbaROMLoaded ?
			this->SaveGBASRAMAsync() :
			this->SaveGBSRAMAsync();
	}

	IAsyncAction ^VBASystem::SaveGBASRAMAsync()
	{
		ByteWrapper ^data = this->GetGBASRAMData();
		if (data == nullptr)
		{
			return create_async([]() {});
		}
		return this->emulator->SaveProvider->SaveSRAMAsync(data);
	}

	IAsyncAction ^VBASystem::SaveGBSRAMAsync()
	{
		ByteWrapper ^data = this->GetGBSRAMData();
		if (data == nullptr)
		{
			return create_async([]() {});
		}
		return this->emulator->SaveProvider->SaveSRAMAsync(data);
	}

	IAsyncAction ^VBASystem::LoadSRAMAsync()
	{
		if (!this->ROMLoaded) {
			return create_async([]() {});
		}

		return this->gbaROMLoaded ?
			this->LoadGBASRAMAsync() :
			this->LoadGBSRAMAsync();
	}

	IAsyncAction ^VBASystem::LoadGBASRAMAsync()
	{
		ISaveProvider ^saveProvider = emulator->SaveProvider;

		return create_async([=]() {
			ByteWrapper ^data = create_task(
				saveProvider->LoadSRAMAsync()
				).get();
			if (data == nullptr)
			{
				// no sram existing yet
				return;
			}

			systemSaveUpdateCounter = SYSTEM_SAVE_NOT_UPDATED;

			if (data->length == 512 || data->length == 0x2000)
			{
				memcpy_s(eepromData, data->length, data->data, data->length);
			}
			else
			{
				if (data->length == 0x20000)
				{
					memcpy_s(flashSaveMemory, 0x20000, data->data, 0x20000);
					flashSetSize(0x20000);
				}
				else if (data->length >= 0x10000)
				{
					memcpy_s(flashSaveMemory, 0x10000, data->data, 0x10000);
					flashSetSize(0x10000);
				}
			}
			data = nullptr;
		});
	}

	IAsyncAction ^VBASystem::LoadGBSRAMAsync()
	{
		ISaveProvider ^saveProvider = emulator->SaveProvider;

		return create_async([=]()
		{
			ByteWrapper ^data = create_task(
				saveProvider->LoadSRAMAsync()
				).get();
			if (data == nullptr)
			{
				// no sram existing yet
				return;
			}

			if (gbBattery)
			{
				switch (gbRomType)
				{
				case 0x03:
					// MBC1
					if (gbRam)
					{
						memcpy_s(gbRam, gbRamSizeMask + 1, data->data, gbRamSizeMask + 1);
					}
					break;
				case 0x06:
					// MBC2
					if (gbRam)
					{
						memcpy_s(gbMemoryMap[0x0a], 512, data->data, 512);
					}
					break;
				case 0x0d:
					// MMM01
					if (gbRam)
					{
						memcpy_s(gbRam, gbRamSizeMask + 1, data->data, gbRamSizeMask + 1);
					}
					break;
				case 0x0f:
				case 0x10:
					// MBC3
					try {
						if (gbRam)
						{
							memcpy_s(gbRam, gbRamSizeMask + 1, data->data, gbRamSizeMask + 1);
							memcpy_s(&gbDataMBC3.mapperSeconds, sizeof(int) * 10 + sizeof(time_t), data->data + gbRamSizeMask + 1, sizeof(int) * 10 + sizeof(time_t));
						}
						else
						{
							memcpy_s(&gbDataMBC3.mapperSeconds, sizeof(int) * 10 + sizeof(time_t), data->data, sizeof(int) * 10 + sizeof(time_t));
						}
					}
					catch (...)
					{
						time(&gbDataMBC3.mapperLastTime);
						struct tm *lt;
						lt = localtime(&gbDataMBC3.mapperLastTime);
						gbDataMBC3.mapperSeconds = lt->tm_sec;
						gbDataMBC3.mapperMinutes = lt->tm_min;
						gbDataMBC3.mapperHours = lt->tm_hour;
						gbDataMBC3.mapperDays = lt->tm_yday & 255;
						gbDataMBC3.mapperControl = (gbDataMBC3.mapperControl & 0xfe) |
							(lt->tm_yday > 255 ? 1 : 0);
					}
					break;
				case 0x13:
				case 0xfc:
					// MBC3 - 2
					if (gbRam)
					{
						memcpy_s(gbRam, gbRamSizeMask + 1, data->data, gbRamSizeMask + 1);
						memcpy_s(&gbDataMBC3.mapperSeconds, sizeof(int) * 10 + sizeof(time_t), data->data + gbRamSizeMask + 1, sizeof(int) * 10 + sizeof(time_t));
					}
					else
					{
						memcpy_s(&gbDataMBC3.mapperSeconds, sizeof(int) * 10 + sizeof(time_t), data->data, sizeof(int) * 10 + sizeof(time_t));
					}
					break;
				case 0x1b:
				case 0x1e:
					// MBC5
					if (gbRam)
					{
						memcpy_s(gbRam, gbRamSizeMask + 1, data->data, gbRamSizeMask + 1);
					}
					break;
				case 0x22:
					// MBC7
					if (gbRam)
					{
						memcpy_s(&gbMemory[0xa000], 256, data->data, 256);
					}
					break;
				case 0xfd:
					try
					{
						if (gbRam)
						{
							memcpy_s(gbRam, gbRamSizeMask + 1, data->data, gbRamSizeMask + 1);
							memcpy_s(gbTAMA5ram, gbTAMA5ramSize, data->data + gbRamSizeMask + 1, gbTAMA5ramSize);
							memcpy_s(&gbDataTAMA5.mapperSeconds, sizeof(int) * 14 + sizeof(time_t), data->data + gbRamSizeMask + 1 + gbTAMA5ramSize, sizeof(int) * 14 + sizeof(time_t));
						}
						else
						{
							memcpy_s(gbTAMA5ram, gbTAMA5ramSize, data->data, gbTAMA5ramSize);
							memcpy_s(&gbDataTAMA5.mapperSeconds, sizeof(int) * 14 + sizeof(time_t), data->data + gbTAMA5ramSize, sizeof(int) * 14 + sizeof(time_t));
						}
					}
					catch (...)
					{
						u8 gbDaysinMonth[12] = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
						time(&gbDataTAMA5.mapperLastTime);
						struct tm *lt;
						lt = localtime(&gbDataTAMA5.mapperLastTime);
						gbDataTAMA5.mapperSeconds = lt->tm_sec;
						gbDataTAMA5.mapperMinutes = lt->tm_min;
						gbDataTAMA5.mapperHours = lt->tm_hour;
						gbDataTAMA5.mapperDays = 1;
						gbDataTAMA5.mapperMonths = 1;
						gbDataTAMA5.mapperYears = 1970;
						int days = lt->tm_yday + 365 * 3;
						while (days)
						{
							gbDataTAMA5.mapperDays++;
							days--;
							if (gbDataTAMA5.mapperDays > gbDaysinMonth[gbDataTAMA5.mapperMonths - 1])
							{
								gbDataTAMA5.mapperDays = 1;
								gbDataTAMA5.mapperMonths++;
								if (gbDataTAMA5.mapperMonths > 12)
								{
									gbDataTAMA5.mapperMonths = 1;
									gbDataTAMA5.mapperYears++;
									if ((gbDataTAMA5.mapperYears & 3) == 0)
										gbDaysinMonth[1] = 29;
									else
										gbDaysinMonth[1] = 28;
								}
							}
						}
						gbDataTAMA5.mapperControl = (gbDataTAMA5.mapperControl & 0xfe) |
							(lt->tm_yday > 255 ? 1 : 0);
					}
					break;
				}
			}
		});
	}

	IAsyncAction ^VBASystem::ApplyCheatsAsync()
	{
		return create_async([]() {});
	}

	IAsyncAction ^VBASystem::StopROMAsync()
	{
#if _DEBUG
		OutputDebugStringW(L"StopROMAsync\n");
#endif

		if (!this->romLoaded)
		{
#if _DEBUG
			OutputDebugStringW(L"Can't stop emulation: no rom loaded.\n");
#endif
			return create_async([]() {});
		}
		return create_async([=]()
		{
			return create_task([=]()
			{
				this->emulator->Pause();
				return this->SaveSRAMAsync();
			}).then([=]() {
				this->ROMLoaded = false;
			}).then([=](task<void> t)
			{
				try {
					t.get();
				}
				catch (Platform::Exception ^ex)
				{
#if _DEBUG
					Platform::String ^msg = ex->Message;
					wstring wmsg(msg->Begin(), msg->End());
					OutputDebugStringW((wstring(L"StopROMAsync: ") + wmsg + L"\n").c_str());
#endif
				}
			});
		});
	}

	IAsyncAction ^VBASystem::LoadStateAsync(ByteWrapper ^bytes)
	{
		if (!this->ROMLoaded)
		{
			return create_async([]() {});
		}
		return this->gbaROMLoaded ?
			this->LoadGBAStateAsync(bytes) :
			this->LoadGBStateAsync(bytes);
	}

	IAsyncAction ^VBASystem::LoadGBAStateAsync(ByteWrapper ^bytes)
	{
		bool paused = this->emulator->Pause();

		u8 *data = bytes->data;
		int readOffset = 0;

		int version;
		u8 romname[17];
		romname[16] = 0;
		bool ub;

		RESTORE_FROM_ARRAY(data, &version, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, romname, 16, readOffset);
		RESTORE_FROM_ARRAY(data, &ub, sizeof(bool), readOffset);
		RESTORE_FROM_ARRAY(data, &reg[0], sizeof(reg), readOffset);
		int i = 0;
		for (; i < ARRAYSIZE(saveGameStruct); i++)
		{
			if (saveGameStruct[i].size > 0)
			{
				RESTORE_FROM_ARRAY(data, saveGameStruct[i].address, saveGameStruct[i].size, readOffset);
			}
		}
		RESTORE_FROM_ARRAY(data, &stopState, sizeof(bool), readOffset);
		RESTORE_FROM_ARRAY(data, &IRQTicks, sizeof(int), readOffset);
		if (IRQTicks > 0)
		{
			intState = true;
		}
		else
		{
			intState = false;
			IRQTicks = 0;
		}
		RESTORE_FROM_ARRAY(data, internalRAM, 0x8000, readOffset);
		RESTORE_FROM_ARRAY(data, paletteRAM, 0x400, readOffset);
		RESTORE_FROM_ARRAY(data, workRAM, 0x40000, readOffset);
		RESTORE_FROM_ARRAY(data, vram, 0x20000, readOffset);
		RESTORE_FROM_ARRAY(data, oam, 0x400, readOffset);
		RESTORE_FROM_ARRAY(data, ioMem, 0x400, readOffset);

		if (skipSaveGameBattery)
		{
			// Skip EEPROM
			for (i = 0; i < ARRAYSIZE(eepromSaveData); i++)
			{
				readOffset += eepromSaveData[i].size;
			}
			readOffset += sizeof(int);
			readOffset += 0x2000;

			// Skip Flash
			for (i = 0; i < ARRAYSIZE(flashSaveData3); i++)
			{
				readOffset += flashSaveData3[i].size;
			}
		}
		else
		{
			// Read EEPROM
			for (i = 0; i < ARRAYSIZE(eepromSaveData); i++)
			{
				if (eepromSaveData[i].size > 0)
				{
					RESTORE_FROM_ARRAY(data, eepromSaveData[i].address, eepromSaveData[i].size, readOffset);
				}
			}
			RESTORE_FROM_ARRAY(data, &eepromSize, sizeof(int), readOffset);
			RESTORE_FROM_ARRAY(data, eepromData, 0x2000, readOffset);

			// Read Flash
			for (i = 0; i < ARRAYSIZE(flashSaveData3); i++)
			{
				if (flashSaveData3[i].size > 0)
				{
					RESTORE_FROM_ARRAY(data, flashSaveData3[i].address, flashSaveData3[i].size, readOffset);
				}
			}
		}

		// Sound
		reset_apu();
		gb_apu->save_state(&state.apu);
		for (i = 0; i < ARRAYSIZE(gba_state); i++)
		{
			if (gba_state[i].size > 0)
			{
				RESTORE_FROM_ARRAY(data, gba_state[i].address, gba_state[i].size, readOffset);
			}
		}
		gb_apu->load_state(state.apu);
		write_SGCNT0_H(READ16LE(&ioMem[SGCNT0_H]) & 0x770F);
		apply_muting();

		RESTORE_FROM_ARRAY(data, &rtcClockData, sizeof(rtcClockData), readOffset);

		layerEnable = layerSettings & DISPCNT;
		CPUUpdateRender();
		CPUUpdateRenderBuffers(true);
		CPUUpdateWindow0();
		CPUUpdateWindow1();

		gbaSaveType = 0;
		switch (saveType) {
		case 0:
			cpuSaveGameFunc = flashSaveDecide;
			break;
		case 1:
			cpuSaveGameFunc = sramWrite;
			gbaSaveType = 1;
			break;
		case 2:
			cpuSaveGameFunc = flashWrite;
			gbaSaveType = 2;
			break;
		case 3:
			break;
		case 5:
			gbaSaveType = 5;
			break;
		default:
			systemMessage(MSG_UNSUPPORTED_SAVE_TYPE,
				N_("Unsupported save type %d"), saveType);
			break;
		}
		if (eepromInUse)
			gbaSaveType = 3;

		CPUReadHelper();

		this->ReapplyCheats();

		return create_async([=]()
		{
			return create_task(this->ApplyCheatsAsync()).then([=]() {
				if (paused) 
				{
					this->emulator->Unpause();
				}
			});
		});
	}

	IAsyncAction ^VBASystem::LoadGBStateAsync(ByteWrapper ^bytes)
	{
		bool paused = this->emulator->Pause();

		u8 *data = bytes->data;
		int readOffset = 0;

		int marker = 0x12345678;

		int version;
		RESTORE_FROM_ARRAY(data, &version, sizeof(int), readOffset);

		u8 romname[20];
		RESTORE_FROM_ARRAY(data, romname, 15, readOffset);

		int ub, ib;
		RESTORE_FROM_ARRAY(data, &ub, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &ib, sizeof(int), readOffset);
		gbReset();
		inBios = ib ? true : false;

		int i = 0;
		for (; i < ARRAYSIZE(gbSaveGameStruct); i++)
		{
			if (gbSaveGameStruct[i].size > 0)
			{
				RESTORE_FROM_ARRAY(data, gbSaveGameStruct[i].address, gbSaveGameStruct[i].size, readOffset);
			}
		}

		// Correct crash when loading color gameboy save in regular gameboy type.
		if (!gbCgbMode)
		{
			if (gbVram != NULL) {
				free(gbVram);
				gbVram = NULL;
			}
			if (gbWram != NULL) {
				free(gbWram);
				gbWram = NULL;
			}
		}
		else
		{
			if (gbVram == NULL)
				gbVram = (u8 *)malloc(0x4000);
			if (gbWram == NULL)
				gbWram = (u8 *)malloc(0x8000);
			memset(gbVram, 0, 0x4000);
			memset(gbPalette, 0, 2 * 128);
		}

		RESTORE_FROM_ARRAY(data, &IFF, sizeof(u16), readOffset);

		if (gbSgbMode)
		{
			i = 0;
			for (; i < ARRAYSIZE(gbSgbSaveStructV3); i++)
			{
				if (gbSgbSaveStructV3[i].size > 0)
				{
					RESTORE_FROM_ARRAY(data, gbSgbSaveStructV3[i].address, gbSgbSaveStructV3[i].size, readOffset);
				}
			}
			RESTORE_FROM_ARRAY(data, gbSgbBorder, 2048, readOffset);
			RESTORE_FROM_ARRAY(data, gbSgbBorderChar, 32 * 256, readOffset);

			RESTORE_FROM_ARRAY(data, gbSgbPacket, 16 * 7, readOffset);
			RESTORE_FROM_ARRAY(data, gbSgbSCPPalette, 4 * 512 * sizeof(u16), readOffset);
			RESTORE_FROM_ARRAY(data, gbSgbATF, 20 * 18, readOffset);
			RESTORE_FROM_ARRAY(data, gbSgbATFList, 45 * 20 * 18, readOffset);
		}
		else
		{
			gbSgbMask = 0;
		}

		RESTORE_FROM_ARRAY(data, &gbDataMBC1, sizeof(gbDataMBC1), readOffset);
		RESTORE_FROM_ARRAY(data, &gbDataMBC2, sizeof(gbDataMBC2), readOffset);
		RESTORE_FROM_ARRAY(data, &gbDataMBC3, sizeof(gbDataMBC3), readOffset);
		RESTORE_FROM_ARRAY(data, &gbDataMBC5, sizeof(gbDataMBC5), readOffset);
		RESTORE_FROM_ARRAY(data, &gbDataHuC1, sizeof(gbDataHuC1), readOffset);
		RESTORE_FROM_ARRAY(data, &gbDataHuC3, sizeof(gbDataHuC3), readOffset);
		RESTORE_FROM_ARRAY(data, &gbDataTAMA5, sizeof(gbDataTAMA5), readOffset);
		if (gbTAMA5ram != NULL)
		{
			if (skipSaveGameBattery)
			{
				readOffset += gbTAMA5ramSize;
			}
			else
			{
				RESTORE_FROM_ARRAY(data, gbTAMA5ram, gbTAMA5ramSize, readOffset);
			}
		}
		RESTORE_FROM_ARRAY(data, &gbDataMMM01, sizeof(gbDataMMM01), readOffset);
		RESTORE_FROM_ARRAY(data, gbPalette, 128 * sizeof(u16), readOffset);
		RESTORE_FROM_ARRAY(data, &gbMemory[0x8000], 0x8000, readOffset);

		if (gbRamSize && gbRam)
		{
			int ramSize;
			RESTORE_FROM_ARRAY(data, &ramSize, sizeof(int), readOffset);
			if (skipSaveGameBattery)
			{
				readOffset += (gbRamSize > ramSize) ? ramSize : gbRamSize;
			}
			else
			{
				RESTORE_FROM_ARRAY(data, gbRam, (gbRamSize>ramSize) ? ramSize : gbRamSize, readOffset);
			}
			if (ramSize > gbRamSize)
			{
				readOffset += ramSize - gbRamSize;
			}
		}

		memset(gbSCYLine, register_SCY, sizeof(gbSCYLine));
		memset(gbSCXLine, register_SCX, sizeof(gbSCXLine));
		memset(gbBgpLine, (gbBgp[0] | (gbBgp[1] << 2) | (gbBgp[2] << 4) |
			(gbBgp[3] << 6)), sizeof(gbBgpLine));
		memset(gbObp0Line, (gbObp0[0] | (gbObp0[1] << 2) | (gbObp0[2] << 4) |
			(gbObp0[3] << 6)), sizeof(gbObp0Line));
		memset(gbObp1Line, (gbObp1[0] | (gbObp1[1] << 2) | (gbObp1[2] << 4) |
			(gbObp1[3] << 6)), sizeof(gbObp1Line));
		memset(gbSpritesTicks, 0x0, sizeof(gbSpritesTicks));

		if (inBios)
		{
			gbMemoryMap[0x00] = &gbMemory[0x0000];
			memcpy((u8 *)(gbMemory), (u8 *)(gbRom), 0x1000);
			memcpy((u8 *)(gbMemory), (u8 *)(bios), 0x100);
		}
		else
		{
			gbMemoryMap[0x00] = &gbRom[0x0000];
		}
		gbMemoryMap[0x01] = &gbRom[0x1000];
		gbMemoryMap[0x02] = &gbRom[0x2000];
		gbMemoryMap[0x03] = &gbRom[0x3000];
		gbMemoryMap[0x04] = &gbRom[0x4000];
		gbMemoryMap[0x05] = &gbRom[0x5000];
		gbMemoryMap[0x06] = &gbRom[0x6000];
		gbMemoryMap[0x07] = &gbRom[0x7000];
		gbMemoryMap[0x08] = &gbMemory[0x8000];
		gbMemoryMap[0x09] = &gbMemory[0x9000];
		gbMemoryMap[0x0a] = &gbMemory[0xa000];
		gbMemoryMap[0x0b] = &gbMemory[0xb000];
		gbMemoryMap[0x0c] = &gbMemory[0xc000];
		gbMemoryMap[0x0d] = &gbMemory[0xd000];
		gbMemoryMap[0x0e] = &gbMemory[0xe000];
		gbMemoryMap[0x0f] = &gbMemory[0xf000];

		switch (gbRomType)
		{
		case 0x00:
		case 0x01:
		case 0x02:
		case 0x03:
			// MBC 1
			memoryUpdateMapMBC1();
			break;
		case 0x05:
		case 0x06:
			// MBC2
			memoryUpdateMapMBC2();
			break;
		case 0x0b:
		case 0x0c:
		case 0x0d:
			// MMM01
			memoryUpdateMapMMM01();
			break;
		case 0x0f:
		case 0x10:
		case 0x11:
		case 0x12:
		case 0x13:
			// MBC 3
			memoryUpdateMapMBC3();
			break;
		case 0x19:
		case 0x1a:
		case 0x1b:
			// MBC5
			memoryUpdateMapMBC5();
			break;
		case 0x1c:
		case 0x1d:
		case 0x1e:
			// MBC 5 Rumble
			memoryUpdateMapMBC5();
			break;
		case 0x22:
			// MBC 7
			memoryUpdateMapMBC7();
			break;
		case 0x56:
			// GS3
			memoryUpdateMapGS3();
			break;
		case 0xfd:
			// TAMA5
			memoryUpdateMapTAMA5();
			break;
		case 0xfe:
			// HuC3
			memoryUpdateMapHuC3();
			break;
		case 0xff:
			// HuC1
			memoryUpdateMapHuC1();
			break;
		}

		if (gbCgbMode)
		{
			RESTORE_FROM_ARRAY(data, gbVram, 0x4000, readOffset);
			RESTORE_FROM_ARRAY(data, gbWram, 0x8000, readOffset);

			int value = register_SVBK;
			if (value == 0)
				value = 1;

			gbMemoryMap[0x08] = &gbVram[register_VBK * 0x2000];
			gbMemoryMap[0x09] = &gbVram[register_VBK * 0x2000 + 0x1000];
			gbMemoryMap[0x0d] = &gbWram[value * 0x1000];
		}

		gbSoundReadGame2();
		i = 0;
		for (; i < ARRAYSIZE(gb_state); i++)
		{
			if (gb_state[i].size > 0)
			{
				RESTORE_FROM_ARRAY(data, gb_state[i].address, gb_state[i].size, readOffset);
			}
		}
		gbSoundReadGame3();

		if (gbCgbMode && gbSgbMode) {
			gbSgbMode = 0;
		}

		if (gbBorderOn && !gbSgbMask) {
			gbSgbRenderBorder();
		}

		// systemDrawScreen(); // Deadlock!

		int numberCheats;
		RESTORE_FROM_ARRAY(data, &numberCheats, sizeof(int), readOffset);
		if (skipSaveGameCheats)
		{
			readOffset += numberCheats * sizeof(gbCheat);
		}
		else
		{
			gbCheatNumber = numberCheats;
			if (gbCheatNumber > 0)
			{
				RESTORE_FROM_ARRAY(data, &gbCheatList[0], sizeof(gbCheat) * gbCheatNumber, readOffset);
			}
		}
		gbCheatUpdateMap();

		int spriteTicks;
		int timerModeChange;
		int timerOnChange;
		int blackScreen;
		int oldRegister;
		int screenOn;
		RESTORE_FROM_ARRAY(data, &gbLcdModeDelayed, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &gbLcdTicksDelayed, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &gbLcdLYIncrementTicksDelayed, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &spriteTicks, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &timerModeChange, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &timerOnChange, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &gbHardware, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &blackScreen, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &oldRegister, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &gbWindowLine, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &inUseRegister_WY, sizeof(int), readOffset);
		RESTORE_FROM_ARRAY(data, &screenOn, sizeof(int), readOffset);
		gbSpritesTicks[299] = spriteTicks;
		gbTimerModeChange = timerModeChange ? true : false;
		gbTimerOnChange = timerOnChange ? true : false;
		gbBlackScreen = blackScreen ? true : false;
		oldRegister_WY = oldRegister;
		gbScreenOn = screenOn;


		if (gbSpeed)
			gbLine99Ticks *= 2;

		systemSaveUpdateCounter = SYSTEM_SAVE_NOT_UPDATED;

		this->ReapplyCheats();

		return create_async([=]()
		{
			return create_task(this->ApplyCheatsAsync()).then([=]() {
				if (paused)
				{
					this->emulator->Unpause();
				}
			});
		});
	}

	ByteWrapper ^VBASystem::GetSaveStateData()
	{
		if (!this->ROMLoaded)
		{
			return nullptr;
		}

		return this->gbaROMLoaded ?
			this->GetGBASaveStateData() :
			this->GetGBSaveStateData();
	}

	ByteWrapper ^VBASystem::GetGBASaveStateData()
	{
		bool paused = emulator->Pause();

		// calculate required size
		int stateSize = 0;
		stateSize += sizeof(int);
		stateSize += 16;
		stateSize += sizeof(bool);
		stateSize += sizeof(reg);
		int i = 0;
		for (; i < ARRAYSIZE(saveGameStruct); i++)
		{
			if (saveGameStruct[i].size > 0)
			{
				stateSize += saveGameStruct[i].size;
			}
		}
		stateSize += sizeof(bool);
		stateSize += sizeof(int);
		stateSize += 0x8000;
		stateSize += 0x400;
		stateSize += 0x40000;
		stateSize += 0x20000;
		stateSize += 0x400;
		stateSize += 0x400;
		for (i = 0; i < ARRAYSIZE(eepromSaveData); i++)
		{
			if (eepromSaveData[i].size > 0)
			{
				stateSize += eepromSaveData[i].size;
			}
		}
		stateSize += sizeof(int);
		stateSize += 0x2000;
		for (i = 0; i < ARRAYSIZE(flashSaveData3); i++)
		{
			if (flashSaveData3[i].size > 0)
			{
				stateSize += flashSaveData3[i].size;
			}
		}
		gb_apu->save_state(&state.apu);
		memset(dummy_state, 0, sizeof dummy_state);
		for (i = 0; i < ARRAYSIZE(gba_state); i++)
		{
			if (gba_state[i].size > 0)
			{
				stateSize += gba_state[i].size;
			}
		}
		stateSize += sizeof(rtcClockData);

		// have size - fill array now
		uint8 *bytes = new uint8[stateSize];
		ByteWrapper ^dataWrapper = ref new ByteWrapper(bytes, stateSize, false);
		int alreadyWritten = 0;

		int version = SAVE_GAME_VERSION;
		STORE_IN_ARRAY(bytes, &version, sizeof(int), alreadyWritten);
		STORE_IN_ARRAY(bytes, &rom[0xa0], 16, alreadyWritten);
		STORE_IN_ARRAY(bytes, &useBios, sizeof(bool), alreadyWritten);
		STORE_IN_ARRAY(bytes, &reg[0], sizeof(reg), alreadyWritten);

		for (i = 0; i < ARRAYSIZE(saveGameStruct); i++)
		{
			if (saveGameStruct[i].size > 0)
			{
				STORE_IN_ARRAY(bytes, saveGameStruct[i].address, saveGameStruct[i].size, alreadyWritten);
			}
		}
		STORE_IN_ARRAY(bytes, &stopState, sizeof(bool), alreadyWritten);
		STORE_IN_ARRAY(bytes, &IRQTicks, sizeof(int), alreadyWritten);
		STORE_IN_ARRAY(bytes, internalRAM, 0x8000, alreadyWritten);
		STORE_IN_ARRAY(bytes, paletteRAM, 0x400, alreadyWritten);
		STORE_IN_ARRAY(bytes, workRAM, 0x40000, alreadyWritten);
		STORE_IN_ARRAY(bytes, vram, 0x20000, alreadyWritten);
		STORE_IN_ARRAY(bytes, oam, 0x400, alreadyWritten);
		STORE_IN_ARRAY(bytes, ioMem, 0x400, alreadyWritten);

		// EEPROM
		for (i = 0; i < ARRAYSIZE(eepromSaveData); i++)
		{
			if (eepromSaveData[i].size > 0)
			{
				STORE_IN_ARRAY(bytes, eepromSaveData[i].address, eepromSaveData[i].size, alreadyWritten);
			}
		}
		STORE_IN_ARRAY(bytes, &eepromSize, sizeof(int), alreadyWritten);
		STORE_IN_ARRAY(bytes, eepromData, 0x2000, alreadyWritten);

		// Flash
		for (i = 0; i < ARRAYSIZE(flashSaveData3); i++)
		{
			if (flashSaveData3[i].size > 0)
			{
				STORE_IN_ARRAY(bytes, flashSaveData3[i].address, flashSaveData3[i].size, alreadyWritten);
			}
		}

		// Sound
		for (i = 0; i < ARRAYSIZE(gba_state); i++)
		{
			if (gba_state[i].size > 0)
			{
				STORE_IN_ARRAY(bytes, gba_state[i].address, gba_state[i].size, alreadyWritten);
			}
		}
		STORE_IN_ARRAY(bytes, &rtcClockData, sizeof(rtcClockData), alreadyWritten);

		if (paused)
		{
			this->emulator->Unpause();
		}

		return dataWrapper;
	}

	ByteWrapper ^VBASystem::GetGBSaveStateData()
	{
		bool paused = this->emulator->Pause();

		int stateSize = 0;
		stateSize += sizeof(int);
		stateSize += 15;

		stateSize += sizeof(int);
		stateSize += sizeof(int);

		int i = 0;
		for (; i < ARRAYSIZE(gbSaveGameStruct); i++)
		{
			if (gbSaveGameStruct[i].size > 0)
			{
				stateSize += gbSaveGameStruct[i].size;
			}
		}

		stateSize += sizeof(u16);

		if (gbSgbMode)
		{			
			for (i = 0; i < ARRAYSIZE(gbSgbSaveStructV3); i++)
			{
				if (gbSgbSaveStructV3[i].size > 0)
				{
					stateSize += gbSgbSaveStructV3[i].size;
				}
			}
			stateSize += 2048;
			stateSize += 32 * 256;
			stateSize += 16 * 7;
			stateSize += 4 * 512 * sizeof(u16);
			stateSize += 20 * 18;
			stateSize += 45 * 20 * 18;
		}

		stateSize += sizeof(gbDataMBC1);
		stateSize += sizeof(gbDataMBC2);
		stateSize += sizeof(gbDataMBC3);
		stateSize += sizeof(gbDataMBC5);
		stateSize += sizeof(gbDataHuC1);
		stateSize += sizeof(gbDataHuC3);
		stateSize += sizeof(gbDataTAMA5);

		if (gbTAMA5ram != NULL)
		{
			stateSize += gbTAMA5ramSize;
		}
		stateSize += sizeof(gbDataMMM01);
		stateSize += 128 * sizeof(u16);
		stateSize += 0x8000;

		if (gbRamSize && gbRam)
		{
			stateSize += sizeof(int);
			stateSize += gbRamSize;
		}

		if (gbCgbMode)
		{
			stateSize += 0x4000;
			stateSize += 0x8000;
		}

		// Sound
		gbSoundSaveGame2();		
		for (i = 0; i < ARRAYSIZE(gb_state); i++)
		{
			if (gb_state[i].size > 0)
			{
				stateSize += gb_state[i].size;
			}
		}

		// Cheats
		stateSize += sizeof(int);
		if (gbCheatNumber > 0)
		{
			stateSize += sizeof(gbCheat)*gbCheatNumber;
		}

		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);
		stateSize += sizeof(int);

		// fill array 
		u8 *bytes = new u8[stateSize];
		ByteWrapper ^data = ref new ByteWrapper(bytes, stateSize, false);
		int writtenAlready = 0;		

		int marker = 0x12345678;
		int version = 12;
		STORE_IN_ARRAY(bytes, &version, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbRom[0x134], 15, writtenAlready);

		int ub = useBios;
		int ib = inBios;
		STORE_IN_ARRAY(bytes, &ub, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &ib, sizeof(int), writtenAlready);

		for (i = 0; i < ARRAYSIZE(gbSaveGameStruct); i++)
		{
			if (gbSaveGameStruct[i].size > 0)
			{
				STORE_IN_ARRAY(bytes, gbSaveGameStruct[i].address, gbSaveGameStruct[i].size, writtenAlready);
			}
		}

		STORE_IN_ARRAY(bytes, &IFF, sizeof(u16), writtenAlready);

		if (gbSgbMode)
		{
			for (i = 0; i < ARRAYSIZE(gbSgbSaveStructV3); i++)
			{
				if (gbSgbSaveStructV3[i].size > 0)
				{
					STORE_IN_ARRAY(bytes, gbSgbSaveStructV3[i].address, gbSgbSaveStructV3[i].size, writtenAlready);
				}
			}

			STORE_IN_ARRAY(bytes, gbSgbBorder, 2048, writtenAlready);
			STORE_IN_ARRAY(bytes, gbSgbBorderChar, 32 * 256, writtenAlready);
			STORE_IN_ARRAY(bytes, gbSgbPacket, 16 * 7, writtenAlready);
			STORE_IN_ARRAY(bytes, gbSgbSCPPalette, 4 * 512 * sizeof(u16), writtenAlready);
			STORE_IN_ARRAY(bytes, gbSgbATF, 20 * 18, writtenAlready);
			STORE_IN_ARRAY(bytes, gbSgbATFList, 45 * 20 * 18, writtenAlready);
		}

		STORE_IN_ARRAY(bytes, &gbDataMBC1, sizeof(gbDataMBC1), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbDataMBC2, sizeof(gbDataMBC2), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbDataMBC3, sizeof(gbDataMBC3), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbDataMBC5, sizeof(gbDataMBC5), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbDataHuC1, sizeof(gbDataHuC1), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbDataHuC3, sizeof(gbDataHuC3), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbDataTAMA5, sizeof(gbDataTAMA5), writtenAlready);
		if (gbTAMA5ram != NULL)
		{
			STORE_IN_ARRAY(bytes, gbTAMA5ram, gbTAMA5ramSize, writtenAlready);
		}
		STORE_IN_ARRAY(bytes, &gbDataMMM01, sizeof(gbDataMMM01), writtenAlready);
		STORE_IN_ARRAY(bytes, gbPalette, 128 * sizeof(u16), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbMemory[0x8000], 0x8000, writtenAlready);

		if (gbRamSize && gbRam)
		{
			STORE_IN_ARRAY(bytes, &gbRamSize, sizeof(int), writtenAlready);
			STORE_IN_ARRAY(bytes, gbRam, gbRamSize, writtenAlready);
		}

		if (gbCgbMode)
		{
			STORE_IN_ARRAY(bytes, gbVram, 0x4000, writtenAlready);
			STORE_IN_ARRAY(bytes, gbWram, 0x8000, writtenAlready);
		}

		// Sound
		for (i = 0; i < ARRAYSIZE(gb_state); i++)
		{
			if (gb_state[i].size > 0)
			{
				STORE_IN_ARRAY(bytes, gb_state[i].address, gb_state[i].size, writtenAlready);
			}
		}

		// Cheats
		STORE_IN_ARRAY(bytes, &gbCheatNumber, sizeof(int), writtenAlready);
		if (gbCheatNumber > 0)
		{
			STORE_IN_ARRAY(bytes, &gbCheatList[0], sizeof(gbCheat)*gbCheatNumber, writtenAlready);
		}

		int spriteTicks = gbSpritesTicks[299];
		int timerModeChange = gbTimerModeChange;
		int timerOnChange = gbTimerOnChange;
		int blackScreen = gbBlackScreen;
		int oldRegister = oldRegister_WY;
		int screenOn = gbScreenOn;
		STORE_IN_ARRAY(bytes, &gbLcdModeDelayed, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbLcdTicksDelayed, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbLcdLYIncrementTicksDelayed, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &spriteTicks, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &timerModeChange, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &timerOnChange, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbHardware, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &blackScreen, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &oldRegister, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &gbWindowLine, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &inUseRegister_WY, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &screenOn, sizeof(int), writtenAlready);
		STORE_IN_ARRAY(bytes, &marker, sizeof(int), writtenAlready);
		
		if (paused)
		{
			this->emulator->Unpause();
		}

		return data;
	}

	ByteWrapper ^VBASystem::GetSRAMData()
	{
		return this->gbaROMLoaded ?
			this->GetGBASRAMData() :
			this->GetGBSRAMData();
	}

	ByteWrapper ^VBASystem::GetGBASRAMData()
	{
		if (gbaSaveType == 0) {
			if (eepromInUse)
				gbaSaveType = 3;
			else switch (saveType) {
			case 1:
				gbaSaveType = 1;
				break;
			case 2:
				gbaSaveType = 2;
				break;
			}
		}

		if ((gbaSaveType) && (gbaSaveType != 5))
		{
			// only save if Flash/Sram in use or EEprom in use
			if (gbaSaveType != 3)
			{
				if (gbaSaveType == 2)
				{
					return ref new ByteWrapper(flashSaveMemory, flashSize, true);
				}
				else
				{
					return ref new ByteWrapper(flashSaveMemory, 0x10000, true);
				}
			}
			else
			{
				return ref new ByteWrapper(eepromData, eepromSize, true);
			}
		}
		return nullptr;
	}

	ByteWrapper ^VBASystem::GetGBSRAMData()
	{
		ByteWrapper ^bytes = nullptr;
		if (gbBattery)
		{
			switch (gbRomType)
			{
			case 0xff:
			case 0x03:
				// MBC1
				if (gbRam)
				{
					bytes = ref new ByteWrapper(gbRam, gbRamSizeMask + 1, true);
				}
				break;
			case 0x06:
				// MBC2
				if (gbRam)
				{
					bytes = ref new ByteWrapper(gbMemoryMap[0x0a], 512, true);
				}
				break;
			case 0x0d:
				// MMM01
				if (gbRam)
				{
					bytes = ref new ByteWrapper(gbRam, gbRamSizeMask + 1, true);
				}
				break;
			case 0x0f:
			case 0x10:
				// MBC3
				if (gbRam)
				{
					int tmpSize = gbRamSizeMask + 1 + 10 * sizeof(int) + sizeof(time_t);
					u8 *tmp = new u8[tmpSize];
					memcpy_s(tmp, gbRamSizeMask + 1, gbRam, gbRamSizeMask + 1);
					memcpy_s(tmp + gbRamSizeMask + 1, 10 * sizeof(int) + sizeof(time_t), &gbDataMBC3.mapperSeconds, 10 * sizeof(int) + sizeof(time_t));

					bytes = ref new ByteWrapper(tmp, tmpSize, false);
				}
				else
				{
					bytes = ref new ByteWrapper((u8 *)&gbDataMBC3.mapperSeconds, 10 * sizeof(int) + sizeof(time_t), true);
				}
				break;
			case 0x13:
			case 0xfc:
				// MBC3 - 2
				if (gbRam)
				{
					bytes = ref new ByteWrapper(gbRam, gbRamSizeMask + 1, true);
				}
				break;
			case 0x1b:
			case 0x1e:
				// MBC5
				if (gbRam)
				{
					bytes = ref new ByteWrapper(gbRam, gbRamSizeMask + 1, true);
				}
				break;
			case 0x22:
				// MBC7
				if (gbRam)
				{
					bytes = ref new ByteWrapper(&gbMemory[0xa000], 256, true);
				}
				break;
			case 0xfd:
				if (gbRam)
				{
					int tmpSize = gbRamSizeMask + 1 + gbTAMA5ramSize + 14 * sizeof(int) + sizeof(time_t);
					u8 *tmp = new u8[tmpSize];
					memcpy_s(tmp, gbRamSizeMask + 1, gbRam, gbRamSizeMask + 1);
					memcpy_s(tmp + gbRamSizeMask + 1, gbTAMA5ramSize, gbTAMA5ram, gbTAMA5ramSize);
					memcpy_s(tmp + gbRamSizeMask + 1 + gbTAMA5ramSize, 14 * sizeof(int) + sizeof(time_t), &gbDataTAMA5.mapperSeconds, 14 * sizeof(int) + sizeof(time_t));

					bytes = ref new ByteWrapper(tmp, tmpSize, false);
				}
				else
				{
					int tmpSize = gbTAMA5ramSize + 14 * sizeof(int) + sizeof(time_t);
					u8 *tmp = new u8[tmpSize];
					memcpy_s(tmp, gbTAMA5ramSize, gbTAMA5ram, gbTAMA5ramSize);
					memcpy_s(tmp + gbTAMA5ramSize, 14 * sizeof(int) + sizeof(time_t), &gbDataTAMA5.mapperSeconds, 14 * sizeof(int) + sizeof(time_t));

					bytes = ref new ByteWrapper(tmp, tmpSize, false);
				}
				break;
			}
		}
		return bytes;
	}

	IAsyncAction ^VBASystem::ParseAdditionalConfigAsync(String ^content)
	{
		// parse vba-over.ini

		return create_async([=]() {
			if (this->romConfigs != nullptr)
			{
				return;
			}
			create_task([=]()
			{
				Map<Platform::String ^, ROMConfig> ^map = ref new Map<Platform::String ^, ROMConfig>();

				if (content == nullptr)
					return;

				string str(content->Begin(), content->End());
				vector<string> lines;
				split(str, '\n', &lines);

				for (vector<string>::const_iterator i = lines.begin(); i != lines.end(); ++i)
				{
					string line = *i;
					int startBraces = firstIndexOf(line, '[');
					if (startBraces == -1)
					{
						continue;
					}
					int endBraces = firstIndexOf(line, ']');
					if (endBraces == -1)
					{
						continue;
					}
					ROMConfig config;
					config.flashSize = -1;
					config.mirroringEnabled = -1;
					config.rtcEnabled = -1;
					config.saveType = -1;

					string romCode = line.substr(startBraces + 1, endBraces - startBraces - 1);

					for (++i; i != lines.end() && !stringWhitespace(line = *i); ++i)
					{
						int equalsIndex = firstIndexOf(line, '=');
						if (equalsIndex == -1)
						{
							continue;
						}
						if (equalsIndex + 1 >= line.size())
						{
							continue;
						}
						string configName = line.substr(0, equalsIndex);
						string configValue = line.substr(equalsIndex + 1);

						stringstream ss;
						ss << configValue;
						int value;
						ss >> value;

						const char *configNameStr = configName.c_str();
						if (strcmp(configNameStr, "rtcEnabled") == 0)
						{
							config.rtcEnabled = value;
						}
						else if (strcmp(configNameStr, "flashSize") == 0)
						{
							config.flashSize = value;
						}
						else if (strcmp(configNameStr, "saveType") == 0)
						{
							config.saveType = value;
						}
						else if (strcmp(configNameStr, "mirroringEnabled") == 0)
						{
							config.mirroringEnabled = value;
						}
					}
					wstring wRomCode(romCode.begin(), romCode.end());
					map->Insert(ref new String(wRomCode.c_str()), config);

					if (i == lines.end())
					{
						break;
					}
				}

				this->romConfigs = map;


			}).then([=](task<void> t) {
				try {
					t.get();
				}
				catch (Platform::Exception ^ex)
				{
					OutputDebugStringW(std::wstring(ex->Message->Begin(), ex->Message->End()).c_str());
				}
			}).wait();
		});
	}

	void VBASystem::SetRenderer(Renderer ^renderer)
	{
		this->renderer = renderer;
		this->SwapBuffers();
	}

	void VBASystem::SwapBuffers()
	{
		uint32_t rowPitch;
		this->renderer->GetBackbufferPtr((void **)&pix, &rowPitch);
		gbaPitch = rowPitch;
	}

	void VBASystem::Update()
	{
		if (this->saveCounter > 0)
		{
			this->saveCounter++;
			// save after 150 frames have passed without battery save changes
			if (this->saveCounter > 150)
			{
#if _DEBUG
				OutputDebugStringW(L"--------------------------- AUTO SAVE SRAM ---------------------------\n");
#endif
				this->SaveSRAMAsync();
				this->saveCounter = 0;
			}
		}

		if (systemSaveUpdateCounter == SYSTEM_SAVE_UPDATED)
		{
			this->saveCounter = 1;
			systemSaveUpdateCounter = SYSTEM_SAVE_NOT_UPDATED;
		}

		try {
			this->emulatedSystem.emuMain(this->emulatedSystem.emuCount);
		}
		catch (Platform::Exception ^exc)
		{
			OutputDebugStringW(L"Exception in emuMain:\n");
			OutputDebugStringW(exc->Message->Begin());
			OutputDebugStringW(L"\n");
		}
		catch(...)
		{ 
			OutputDebugStringW(L"Unknown exception in emuMain\n");
		}
	}

#define IS_HEX(a) (\
	(a >= '0' && a <= '9') || (a >= 'a' && a <= 'f') || (a >= 'A' && a <= 'F'))

	bool VBASystem::CheckCode(Platform::String ^ codes, IVector<Platform::String ^> ^singleCodes)
	{
		if (this->gbaROMLoaded)
		{
			if (codes == nullptr || codes->IsEmpty())
			{
				return false;
			}

			vector<string> codeParts;
			string code(codes->Begin(), codes->End());

			strreplace(code, '\n', '\r');
			strSplitLines(code, codeParts);

			for (int i = 0; i < codeParts.size(); i++)
			{
				string line = codeParts.at(i);
				StrToUpper(line);
				replaceAll(line, "\t", "");
				replaceAll(line, " ", "");
				for (int i = 0; i < line.length(); i++)
				{
					if (!IS_HEX(line.at(i)))
					{
						if (singleCodes != nullptr)
						{
							singleCodes->Clear();
						}
						return false;
					}
				}
				if (line.length() != 12 && line.length() != 16)
				{
					if (singleCodes != nullptr)
					{
						singleCodes->Clear();
					}
					return false;
				}

				if (singleCodes != nullptr)
				{
					wstringstream wss;
					wstring wline(line.begin(), line.end());
					if (wline.size() == 12)
					{
						wss << wline.substr(0, 8);
						wss << ' ';
						wss << wline.substr(8, 4);
					}
					else if (wline.size() == 16)
					{
						wss << wline;
					}
					singleCodes->Append(ref new Platform::String(wss.str().c_str()));
				}
			}

			return true;
		}
		else
		{
			// gbc
			if (codes == nullptr || codes->IsEmpty())
			{
				return false;
			}

			vector<string> codeParts;
			string code(codes->Begin(), codes->End());

			strreplace(code, '\n', '\r');
			strSplitLines(code, codeParts);

			for (int i = 0; i < codeParts.size(); i++)
			{
				string line = codeParts.at(i);
				StrToUpper(line);
				replaceAll(line, "\t", "");
				replaceAll(line, " ", "");
				replaceAll(line, "-", "");
				for (int i = 0; i < line.length(); i++)
				{
					if (!IS_HEX(line.at(i)))
					{
						if (singleCodes != nullptr)
						{
							singleCodes->Clear();
						}
						return false;
					}
				}
				if (line.length() != 6 && line.length() != 9 && line.length() != 8)
				{
					if (singleCodes != nullptr)
					{
						singleCodes->Clear();
					}
					return false;
				}

				if (singleCodes != nullptr)
				{
					wstringstream wss;
					wstring wline(line.begin(), line.end());
					if (wline.size() == 6)
					{
						wss << wline.substr(0, 3);
						wss << '-';
						wss << wline.substr(3, 3);
					}
					else if (wline.size() == 9)
					{
						wss << wline.substr(0, 3);
						wss << '-';
						wss << wline.substr(3, 3);
						wss << '-';
						wss << wline.substr(6, 3);
					}
					else if (wline.size() == 8)
					{
						wss << wline;
					}
					singleCodes->Append(ref new Platform::String(wss.str().c_str()));
				}
			}

			return true;

		}
		return true;
	}

	void VBASystem::ApplyCheats(IVector<CheatData^>^ cheats)
	{
		if (!this->emulator->ROMLoaded)
		{
			return;
		}
		this->cheatData = cheats;
		if (this->cheatData != nullptr)
		{
			bool paused = this->emulator->Pause();

			if (this->gbaROMLoaded)
			{
				cheatsDeleteAll(false);

				cheatsEnabled = (cheats->Size > 0);

				for (int i = 0; i < cheats->Size; i++)
				{
					auto data = cheats->GetAt(i);
					if (!data->Enabled)
						continue;

					Platform::String ^code = data->CheatCode;
					Platform::String ^desc = data->Description;

					string codeString(code->Begin(), code->End());
					string descString(desc->Begin(), desc->End());

					if (code->Length() == 13)
					{
						// Code Breaker
						cheatsAddCBACode(codeString.c_str(), descString.c_str());
					}
					else if (code->Length() == 16)
					{
						// Gameshark
						cheatsAddGSACode(codeString.c_str(), descString.c_str(), true);
					}
				}
			}
			else {
				// gbc
				cheatsDeleteAll(false);

				cheatsEnabled = (cheats->Size > 0);

				for (int i = 0; i < cheats->Size; i++)
				{
					auto data = cheats->GetAt(i);
					if (!data->Enabled)
						continue;

					Platform::String ^code = data->CheatCode;
					Platform::String ^desc = data->Description;

					string codeString(code->Begin(), code->End());
					string descString(desc->Begin(), desc->End());

					if (code->Length() == 11 || code->Length() == 7)
					{
						// GameGenie
						gbAddGgCheat(codeString.c_str(), descString.c_str());
					}
					else if (code->Length() == 8)
					{
						// Gameshark
						gbAddGsCheat(codeString.c_str(), descString.c_str());
					}
				}
			}

			if (paused)
			{
				this->emulator->Unpause();
			}
		}
	}

	void VBASystem::ReapplyCheats()
	{
		if (this->cheatData != nullptr)
		{
			this->ApplyCheats(this->cheatData);
		}
	}
}

bool systemDrawScreen()
{
	inSystemDrawScreen = true;
	LeaveCriticalSection(&pauseSync);
	SetEvent(swapEvent);
	WaitForSingleObjectEx(updateEvent, INFINITE, false);
	EnterCriticalSection(&pauseSync);
	if (romJustSwitched) {
		inSystemDrawScreen = false;
		romJustSwitched = false;
		return true;
	}
	inSystemDrawScreen = false;
	return false;
}

extern SoundDriver *newXAudio2_Output();

SoundDriver *systemSoundInit()
{
	synchronize = EmulatorComponent::EmulatorComponent::Current->Settings->SyncAudio;

	SoundDriver * drv = 0;
	soundShutdown();

	if (EmulatorComponent::EmulatorComponent::Current->Settings->EnableSound)
	{
		drv = newXAudio2_Output();
	}

	return drv;
}

u32 systemReadJoypad(int gamepad)
{
	u32 res = 0;

	if (!currentController)
		return res;

	bool left = false;
	bool right = false;
	bool up = false;
	bool down = false;
	bool start = false;
	bool select = false;
	bool a = false;
	bool b = false;
	bool l = false;
	bool r = false;


	EmulatorComponent::ControllerState ^state = currentController->GetControllerState();

	if (state->buttons.APressed || a)
		res |= 1;
	if (state->buttons.BPressed || b)
		res |= 2;
	if (state->buttons.SelectPressed || select)
		res |= 4;
	if (state->buttons.StartPressed || start)
		res |= 8;
	if (state->buttons.RightPressed || right)
		res |= 16;
	if (state->buttons.LeftPressed || left)
		res |= 32;
	if (state->buttons.UpPressed || up)
		res |= 64;
	if (state->buttons.DownPressed || down)
		res |= 128;

	// disallow L+R or U+D of being pressed at the same time
	if ((res & 48) == 48)
		res &= ~16;
	if ((res & 192) == 192)
		res &= ~128;

	if (state->buttons.TurboPressed)
	{
		res |= 1024;
	}
	if (state->buttons.RPressed | r)
		res |= 256;
	if (state->buttons.LPressed | l)
		res |= 512;

	return res;
}

u32 systemGetClock() { return (u32)GetTickCount64(); }


bool enableTurboMode = false;

void log(const char *, ...) { }

void winSignal(int, int) { }

void winOutput(const char *s, u32 addr) { }

void(*dbgSignal)(int, int) = winSignal;
void(*dbgOutput)(const char *, u32) = winOutput;

bool systemPauseOnFrame() { return false; }
void systemGbPrint(u8 *, int, int, int, int) { }
void systemScreenCapture(int) { }
// updates the joystick data
bool systemReadJoypads() { return true; }
void systemMessage(int, const char *, ...) { }
void systemSetTitle(const char *) { }
void systemWriteDataToSoundBuffer() { }
void systemSoundShutdown() { }
void systemSoundPause() { }
void systemSoundResume() { }
void systemSoundReset() { }
//SoundDriver *systemSoundInit() { return NULL; }
void systemScreenMessage(const char *) { }
void systemUpdateMotionSensor() { }
int  systemGetSensorX() { return 0; }
int  systemGetSensorY() { return 0; }
bool systemCanChangeSoundQuality() { return false; }
void systemShowSpeed(int) { }
void system10Frames(int) { }
void systemFrame() { }
void systemGbBorderOn() { }
void winlog(const char *, ...) { }
void systemOnWriteDataToSoundBuffer(const u16 * finalWave, int length) { }
void systemOnSoundShutdown() { }
extern void soundShutdown();
void systemGbPrint(unsigned char *, int, int, int, int, int) { }

int RGB_LOW_BITS_MASK = 65793;
bool systemSoundOn;
u16 systemColorMap16[0x10000];
u32 systemColorMap32[0x10000];
u16 systemGbPalette[24];
int systemRedShift;
int systemGreenShift;
int systemBlueShift;
int systemColorDepth;
int systemDebug;
int systemVerbose;
int systemFrameSkip;
int systemSaveUpdateCounter;