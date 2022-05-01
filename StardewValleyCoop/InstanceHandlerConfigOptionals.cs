using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyCoop
{
	public class InstanceHandlerConfigOptionals
	{
		public bool setupState = true;
		public bool wakeUpProcess = false;
		public string unattendedLauncherPath = "";
		public bool useController = false;
		public uint controllerIndex = 1;
		public bool useOpenXInput = true;
		public bool resizeWindow = false;
		public bool useFocusLoop = false;
		public int processorAffinity = 0x0001;

		public readonly MessagesToSend messagesToSend = new();
		public InstanceWindowPositionSettings windowPositionSettings = new();
		public InstanceHandlerConfigOptionals()
		{

		}

	}

	public class InstanceWindowPositionSettings
    {
		public string displayName = "";
		public int posX = 0;
		public int posY = 0;
		public int width = 1920;
		public int height = 1080;
	}

	public class MessagesToSend
	{
		public bool mouseWheelMessages = true;
		public bool mouseButtonMessages = true;
		public bool mouseMoveMessages = true;
		public bool keyboardPressMessages = true;

		public MessagesToSend()
        {

        }
	}
}
