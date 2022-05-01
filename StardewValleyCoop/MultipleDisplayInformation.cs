using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static StardewValleyCoop.MultipleDisplayInformation.NativeMethods;

namespace StardewValleyCoop
{
	public static class MultipleDisplayInformation
	{
		public static List<DisplayInformation> GetAllDisplaysInformation()
		{
			DISPLAY_DEVICE displayDevice = default;
			displayDevice.cb = Marshal.SizeOf(displayDevice);

			List<DisplayInformation> displayInformationList = new();
			for (uint i = 0; EnumDisplayDevices(null, i, ref displayDevice, 0); i++)
			{

				if (displayDevice.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop))
				{

					EnumDisplayDevices(displayDevice.DeviceName, 0, ref displayDevice, 0);

					const int ENUM_CURRENT_SETTINGS = -1;
					DEVMODE devMode = new();
					devMode.dmSize = (short)Marshal.SizeOf(devMode);
					string deviceName = displayDevice.DeviceName[..displayDevice.DeviceName.LastIndexOf('\\')];

					EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode);

					displayInformationList.Add(new DisplayInformation(
						displayDevice.DeviceID,
						deviceName,
						devMode.dmPositionX,
						devMode.dmPositionY,
						devMode.dmPelsWidth,
						devMode.dmPelsHeight));
				}
				displayDevice.cb = Marshal.SizeOf(displayDevice);

			}
			return displayInformationList;
		}

		internal static class NativeMethods
        {
			[DllImport("user32.dll")]
			internal static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

			[StructLayout(LayoutKind.Sequential)]
			internal struct DEVMODE
			{
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
				public string dmDeviceName;
				public short dmSpecVersion;
				public short dmDriverVersion;
				public short dmSize;
				public short dmDriverExtra;
				public int dmFields;
				public int dmPositionX;
				public int dmPositionY;
				public int dmDisplayOrientation;
				public int dmDisplayFixedOutput;
				public short dmColor;
				public short dmDuplex;
				public short dmYResolution;
				public short dmTTOption;
				public short dmCollate;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
				public string dmFormName;
				public short dmLogPixels;
				public int dmBitsPerPel;
				public int dmPelsWidth;
				public int dmPelsHeight;
				public int dmDisplayFlags;
				public int dmDisplayFrequency;
				public int dmICMMethod;
				public int dmICMIntent;
				public int dmMediaType;
				public int dmDitherType;
				public int dmReserved1;
				public int dmReserved2;
				public int dmPanningWidth;
				public int dmPanningHeight;
			}

			[DllImport("user32.dll")]
			internal static extern bool EnumDisplayDevices(string? deviceName, uint devNum, ref DISPLAY_DEVICE displayDevice, uint flags);

			[Flags()]
			internal enum DisplayDeviceStateFlags : int
			{
				/// <summary>The device is part of the desktop.</summary>
				AttachedToDesktop = 0x1,
				MultiDriver = 0x2,
				/// <summary>The device is part of the desktop.</summary>
				PrimaryDevice = 0x4,
				/// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
				MirroringDriver = 0x8,
				/// <summary>The device is VGA compatible.</summary>
				VGACompatible = 0x10,
				/// <summary>The device is removable; it cannot be the primary display.</summary>
				Removable = 0x20,
				/// <summary>The device has more display modes than its output devices support.</summary>
				ModesPruned = 0x8000000,
				Remote = 0x4000000,
				Disconnect = 0x2000000
			}

			internal struct DISPLAY_DEVICE
			{
				[MarshalAs(UnmanagedType.U4)]
				public int cb;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
				public string DeviceName;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
				public string DeviceString;
				[MarshalAs(UnmanagedType.U4)]
				public DisplayDeviceStateFlags StateFlags;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
				public string DeviceID;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
				public string DeviceKey;
			}

		}
	}
}
