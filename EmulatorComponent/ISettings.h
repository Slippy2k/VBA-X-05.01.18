#pragma once

namespace EmulatorComponent
{
	/// <summary>
	/// Contains all available application accent colors.
	/// </summary>
	public enum class ApplicationColor : int
	{
		Accent = 0,
		GBA,
		Green,
		Red,
		Gray
	};

	public enum class AutosaveInterval : int
	{
		Off = 0,
		OneMinute,
		ThreeMinutes,
		FiveMinutes,
		TenMinutes,
		FifteenMinutes
	};

	public enum class AspectRatio : int
	{
		Original = 0,
		Stretch,
		FourToThree,
		FiveToFour,
		One
	};

	public enum class Filter : int
	{
		Nearest = 0,
		Bilinear,
		HQ2x,
		HQ3x,
		HQ4x,
		xBR2,
		xBR3,
		xBR4,
		xBR5
	};

	public enum class ControllerStyle : int
	{
		FourWay = 0,
		EightWay,
		FixedStick,
		DynamicStick
	};

	public interface class ISettings : Windows::UI::Xaml::Data::INotifyPropertyChanged
	{
	public:
		property bool FirstLaunch
		{
			bool get();
			void set(bool value);
		}

		property Windows::UI::Xaml::ApplicationTheme Theme
		{
			Windows::UI::Xaml::ApplicationTheme get();
			void set(Windows::UI::Xaml::ApplicationTheme value);
		}

		property ApplicationColor AccentColor
		{
			ApplicationColor get();
			void set(ApplicationColor value);
		}

		property Windows::UI::Color CurrentAccentColor
		{
			Windows::UI::Color get();
		}

		property bool Fullscreen
		{
			bool get();
			void set(bool value);
		}

		property bool SaveConfirmation
		{
			bool get();
			void set(bool value);
		}

		property bool LoadConfirmation
		{
			bool get();
			void set(bool value);
		}

		property bool ResetConfirmation
		{
			bool get();
			void set(bool value);
		}

		property bool ManualSnapshots
		{
			bool get();
			void set(bool value);
		}

		property AutosaveInterval AutoSaveInterval
		{
			AutosaveInterval get();
			void set(AutosaveInterval value);
		}

		property bool ShowFPS
		{
			bool get();
			void set(bool value);
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

		property ::EmulatorComponent::AspectRatio AspectRatio
		{
			::EmulatorComponent::AspectRatio get();
			void set(::EmulatorComponent::AspectRatio value);
		}

		property ::EmulatorComponent::Filter Filter
		{
			::EmulatorComponent::Filter get();
			void set(::EmulatorComponent::Filter value);
		}

		property int VideoScale
		{
			int get();
			void set(int value);
		}

		property bool ShowVirtualController
		{
			bool get();
			void set(bool value);
		}

		property ::EmulatorComponent::ControllerStyle ControllerStyle
		{
			::EmulatorComponent::ControllerStyle get();
			void set(::EmulatorComponent::ControllerStyle value);
		}

		property int StickDeadzone
		{
			int get();
			void set(int value);
		}

		property int VirtualControllerScale
		{
			int get();
			void set(int value);
		}

		property int VirtualControllerOpacity
		{
			int get();
			void set(int value);
		}

		property Windows::Foundation::Point DPadOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point StartOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point SelectOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point TurboOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point LOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point ROffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point AOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point BOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PDPadOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PStartOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PSelectOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PTurboOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PLOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PROffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PAOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::Foundation::Point PBOffset
		{
			Windows::Foundation::Point get();
			void set(Windows::Foundation::Point value);
		}

		property Windows::System::VirtualKey LeftBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey RightBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey UpBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey DownBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey ABinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey BBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey LBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey RBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey StartBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey SelectBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property Windows::System::VirtualKey TurboBinding
		{
			Windows::System::VirtualKey get();
			void set(Windows::System::VirtualKey value);
		}

		property bool EnableSound
		{
			bool get();
			void set(bool value);
		}

		property bool SyncAudio
		{
			bool get();
			void set(bool value);
		}

		property int SoundVolume
		{
			int get();
			void set(int value);
		}
	};
}
