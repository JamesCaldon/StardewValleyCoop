using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace StardewValleyCoop
{
    public class InstanceHandlerConfig
    {
		public enum ProtoHookIDs : uint
		{
			RegisterRawInputHookID = 0,
			GetRawInputDataHookID,
			MessageFilterHookID,
			GetCursorPosHookID,
			SetCursorPosHookID,
			GetKeyStateHookID,
			GetAsyncKeyStateHookID,
			GetKeyboardStateHookID,
			CursorVisibilityStateHookID,
			ClipCursorHookID,
			FocusHooksHookID,
			RenameHandlesHookID,
			XinputHookID,
			DinputOrderHookID,
			SetWindowPosHookID,
			BlockRawInputHookID,
			FindWindowHookID,
			CreateSingleHIDHookID,
			WindowStyleHookID
		};

		public enum ProtoMessageFilterIDs : uint
		{
			RawInputFilterID = 0,
			MouseMoveFilterID,
			MouseActivateFilterID,
			WindowActivateFilterID,
			WindowActivateAppFilterID,
			MouseWheelFilterID,
			MouseButtonFilterID,
			KeyboardButtonFilterID
		};

		public enum ProtoInjectionTypeIDs
        {
			RemoteLoadLibraryInjectRuntime = 0,
			EasyHookInjectRuntime,
			EasyHookStealthInjectRuntime,
			EasyHookInjectStartup
        }

		// Required:
		public string gameRootPath;
		public string exeName;
		public string windowTitleRegex;
		public int instanceIndex;
		public ProtoInjectionTypeIDs injectionType;


		// Optional:
		public InstanceHandlerConfigOptionals optionals = new();
		public HashSet<ProtoHookIDs> hooks = new();
		public HashSet<ProtoMessageFilterIDs> filters = new();
		private ILogger logger = new LoggerFactory().CreateLogger(typeof(InstanceHandlerConfig));

        internal ILogger Logger { get => logger; set => logger = value; }

        public InstanceHandlerConfig(
			string gameRootPath, 
			string exeName, 
			string windowTitleRegex,
			int instanceIndex,
			ILogger logger,
			ProtoInjectionTypeIDs injectionType = ProtoInjectionTypeIDs.EasyHookInjectRuntime)
        {
			this.gameRootPath = gameRootPath;
			this.exeName = exeName;
            this.windowTitleRegex = windowTitleRegex;
            this.instanceIndex = instanceIndex;
            Logger = logger;
            this.injectionType = injectionType;



			// Default Hooks
			hooks.Add(ProtoHookIDs.RegisterRawInputHookID);
			hooks.Add(ProtoHookIDs.GetRawInputDataHookID);
            hooks.Add(ProtoHookIDs.MessageFilterHookID);
            hooks.Add(ProtoHookIDs.GetCursorPosHookID);
            hooks.Add(ProtoHookIDs.SetCursorPosHookID);
            hooks.Add(ProtoHookIDs.GetKeyStateHookID);
            hooks.Add(ProtoHookIDs.GetAsyncKeyStateHookID);
            hooks.Add(ProtoHookIDs.GetKeyboardStateHookID);
            hooks.Add(ProtoHookIDs.CursorVisibilityStateHookID);
            hooks.Add(ProtoHookIDs.FocusHooksHookID);
            hooks.Add(ProtoHookIDs.XinputHookID);
            //hooks.Add(ProtoHookIDs.SetWindowPosHookID);
            //FindWindowHookID
            hooks.Add(ProtoHookIDs.WindowStyleHookID);

            // Default Filters
            filters.Add(ProtoMessageFilterIDs.RawInputFilterID);
            filters.Add(ProtoMessageFilterIDs.MouseMoveFilterID);
            filters.Add(ProtoMessageFilterIDs.MouseActivateFilterID);
            filters.Add(ProtoMessageFilterIDs.WindowActivateFilterID);
			filters.Add(ProtoMessageFilterIDs.WindowActivateAppFilterID);
            filters.Add(ProtoMessageFilterIDs.MouseWheelFilterID);
            filters.Add(ProtoMessageFilterIDs.MouseButtonFilterID);
            filters.Add(ProtoMessageFilterIDs.KeyboardButtonFilterID);

        }


    }
}
