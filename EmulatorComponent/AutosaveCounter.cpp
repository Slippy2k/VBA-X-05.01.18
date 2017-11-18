#include "pch.h"
#include "AutosaveCounter.h"
#include "EmulatorComponent.h"

using namespace std;

namespace EmulatorComponent
{
	float AUTOSAVE_INTERVALS[] = {
		1.0f * 60.0f,
		3.0f * 60.0f,
		5.0f * 60.0f,
		10.0f * 60.0f,
		15.0f * 60.0f
	};

	AutosaveCounter::AutosaveCounter()
		: totalElapsedTime(0.0f)
	{

	}

	AutosaveCounter::~AutosaveCounter()
	{ }

	void AutosaveCounter::Update(float elapsedTime)
	{
		auto settings = EmulatorComponent::Current->Settings;

		if (settings->AutoSaveInterval == AutosaveInterval::Off || 
			EmulatorComponent::Current->SaveProvider == nullptr)
		{
			this->totalElapsedTime = 0.0f;
			return;
		}

		float requiredTime = AUTOSAVE_INTERVALS[(int)settings->AutoSaveInterval - 1];

		this->totalElapsedTime += elapsedTime;

		if (this->totalElapsedTime >= requiredTime)
		{
			this->totalElapsedTime = max(0.0f, this->totalElapsedTime - requiredTime);
			
			EmulatorComponent::Current->SaveProvider->TriggerAutosave();
		}
	}
}