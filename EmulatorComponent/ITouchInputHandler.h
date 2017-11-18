#pragma once

namespace EmulatorComponent
{
	public interface class ITouchInputHandler
	{
		void StartCustomizing();
		void CommitCustomizing();
		void CancelCustomizing();
		void ResetCustomization();

		void PointerPressed(Windows::UI::Input::PointerPoint ^point);
		void PointerMoved(Windows::UI::Input::PointerPoint ^point);
		void PointerReleased(Windows::UI::Input::PointerPoint ^point);
	};
}