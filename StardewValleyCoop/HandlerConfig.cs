using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValleyCoop.InstanceHandlerConfig;

namespace StardewValleyCoop
{
    public class HandlerConfig : Config
    {
        public string name = "";
        public string[] gameRootPaths = new string[] { "" };
        public string exeName = "";
        public string windowTitleRegex = "";
        public ProtoInjectionTypeIDs injectionType = ProtoInjectionTypeIDs.EasyHookInjectRuntime;
        public int numberOfPlayers = 3;

        public bool useControllers = true;
        public bool resizeWindows = true;
        public string[] useMonitors = Array.Empty<string>();
        public bool makeBorderless = true;
        public bool useFocusLoop = true;
		public int[] processorAffinities = new int[] {};

    }
}
