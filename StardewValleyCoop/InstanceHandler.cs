global using ProtoInstanceHandle = System.UInt32;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using static StardewValleyCoop.InstanceHandlerConfig;
using static StardewValleyCoop.InstanceHandler.NativeMethods;
using System.Text;
using System.Text.RegularExpressions;

namespace StardewValleyCoop
{


internal class InstanceHandler
    {

		private readonly InstanceHandlerConfig _config;
		private readonly string _dllFolderPath;
		private readonly ILogger _logger;
		public InstanceHandler(InstanceHandlerConfig config, string dllFolderPath, ILogger logger)
        {
			_config = config;
			_dllFolderPath = dllFolderPath;
			_logger = logger;

		}

		public void StartHandling()
		{
			var WS_BORDER = 0x00800000L;
			var WS_SYSMENU = 0x00080000L;
			var WS_DLGFRAME = 0x00400000L;
			var WS_CAPTION = 0x00C00000L;
			var WS_THICKFRAME = 0x00040000L;
			var WS_MINIMIZE = 0x20000000L;
			var WS_MAXIMIZE = 0x01000000L;

			var SWP_NOZORDER = 0x0004;
			var SWP_NOMOVE = 0x0002;
			var SWP_NOSIZE = 0x0001;
			var SWP_NOACTIVATE = 0x0010;
			var SWP_DRAWFRAME = 0x0020;
			var SWP_SHOWWINDOW = 0x0040;

			//TODO: Check _dllFolderPath exists and contains all needed dlls.
			//TODO: Set process core affinity on configuration
			//TODO: Include SetWindowPosHook if position information in instance config
			ProcessStartInfo startInfo = new(Path.Join(_config.gameRootPath, _config.exeName));
			startInfo.UseShellExecute = true;
			startInfo.CreateNoWindow = false;
			startInfo.WindowStyle = ProcessWindowStyle.Normal;

			Process? process = Process.Start(startInfo);

			if (process == null) return;
			_logger.LogInformation("Process: {pid}, {name} started", process.Id, process.ProcessName);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Setting affinity for {pid} to {affinity}", 
					process.Id, _config.optionals.processorAffinity);

				process.ProcessorAffinity = (IntPtr)_config.optionals.processorAffinity;
			}
			if (_config.windowTitleRegex != "")
			{
				Regex regex = new(_config.windowTitleRegex);

				while (process.MainWindowHandle == IntPtr.Zero && !process.HasExited)
                {
					_logger.LogInformation("Still attempting to find window handle: {MainWindowHandle}",
						process.MainWindowHandle);

					Thread.Sleep(1000);
                }
				GCHandle? windowThreadHandleNullable = null;
				try
				{
					while ((windowThreadHandleNullable = ProcessThreadWindows.FindVisibleWindowHandleWithTitleRegex(process.Id, regex)) == null)
					{
						Thread.Sleep(1000);
					}
				}
                catch (InvalidOperationException)
				{

                    throw;
                }

				GCHandle windowThreadHandle = (GCHandle) windowThreadHandleNullable;

                var oldStyles = ProcessThreadWindows.NativeMethods.GetWindowLongPtr(windowThreadHandle, -16);
                int oldStylesInt = (int) GCHandle.ToIntPtr(oldStyles);
                var newStyles = oldStylesInt & ~(WS_BORDER | WS_SYSMENU | WS_DLGFRAME | WS_CAPTION | WS_THICKFRAME | WS_MINIMIZE | WS_MAXIMIZE);


                var styleHandle = ProcessThreadWindows.NativeMethods.SetWindowLongPtr(windowThreadHandle, -16, GCHandle.FromIntPtr((IntPtr) newStyles));
                var _ = ProcessThreadWindows.NativeMethods.SetWindowPos(windowThreadHandle, 0,
                    _config.optionals.windowPositionSettings.posX,
                    _config.optionals.windowPositionSettings.posY,
                    _config.optionals.windowPositionSettings.width,
                    _config.optionals.windowPositionSettings.height,
                    SWP_NOZORDER);
 
            }

			Thread.Sleep(5000);

			//Thread.Sleep(5000);
			using (AnonymousPipeServerStream pipeServer = new(
				PipeDirection.In,
				HandleInheritability.Inheritable))
            {
                int status = 0;
                SafeHandle handle = pipeServer.ClientSafePipeHandle;

                status = SetStdHandle(-11, handle); // set stdout
                                                    // Check status as needed
                status = SetStdHandle(-12, handle); // set stderr
                                                    // Check status as needed

                var loggingTask = Task.Run(() =>
                {
                    StreamReader reader = new(pipeServer);
                    try
                    {
                        while (!reader.EndOfStream)
                        {
                            if (reader.Peek() != -1)
                            {
                                string? line = reader.ReadLine();
                                _logger.LogInformation("ProtoInputDLLs STD OUT/ERR: {line}", line);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError($"Oh no something went wrong: {ex}");
                    }

                });

                ProtoInstanceHandle instanceHandle = EasyHookInjectRuntime((uint)process.Id, _dllFolderPath);
			

				if (_config.optionals.setupState) SetupState(instanceHandle, _config.instanceIndex);

			
				if (_config.optionals.useOpenXInput) SetUseOpenXinput(instanceHandle, true);

				if (_config.optionals.messagesToSend.mouseWheelMessages ||
					_config.optionals.messagesToSend.mouseButtonMessages ||
					_config.optionals.messagesToSend.mouseMoveMessages ||
					_config.optionals.messagesToSend.keyboardPressMessages)
				{
					SetupMessagesToSend(
						instanceHandle,
						_config.optionals.messagesToSend.mouseWheelMessages,
						_config.optionals.messagesToSend.mouseButtonMessages,
						_config.optionals.messagesToSend.mouseMoveMessages,
						_config.optionals.messagesToSend.keyboardPressMessages);
				}

				if (_config.optionals.useController) SetControllerIndex(instanceHandle, _config.optionals.controllerIndex);

				if (_config.optionals.resizeWindow)
				{
					SetSetWindowPosSettings(instanceHandle,
					_config.optionals.windowPositionSettings.posX,
					_config.optionals.windowPositionSettings.posY,
					_config.optionals.windowPositionSettings.width,
					_config.optionals.windowPositionSettings.height);

					_config.hooks.Add(ProtoHookIDs.SetWindowPosHookID);
				}

				foreach (ProtoHookIDs hookID in _config.hooks)
				{
					_logger.LogInformation("Installing Hook {HookID}", hookID);
					InstallHook(instanceHandle, hookID);
				}

				foreach (ProtoMessageFilterIDs filterID in _config.filters)
				{
					_logger.LogInformation("Installing Filter {HookID}", filterID);
					EnableMessageFilter(instanceHandle, filterID);
				}


				// Is blocking?
				if (_config.optionals.useFocusLoop) StartFocusMessageLoop(instanceHandle);



			}

		}

		internal static class NativeMethods
		{
			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern uint EasyHookInjectRuntime(uint pid, string dllFolderPath);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void InstallHook(ProtoInstanceHandle instanceHandle, ProtoHookIDs hookID);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void UninstallHook(ProtoInstanceHandle instanceHandle, ProtoHookIDs hookID);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void EnableMessageFilter(ProtoInstanceHandle instanceHandle, ProtoMessageFilterIDs filterID);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void DisableMessageFilter(ProtoInstanceHandle instanceHandle, ProtoMessageFilterIDs filterID);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void EnableMessageBlock(ProtoInstanceHandle instanceHandle, uint messageID);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void DisableMessageBlock(ProtoInstanceHandle instanceHandle, uint messageID);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void WakeUpProcess(ProtoInstanceHandle instanceHandle);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void UpdateMainWindowHandle(ProtoInstanceHandle instanceHandle, UInt64 hwnd = 0);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetupState(ProtoInstanceHandle instanceHandle, int instanceIndex);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetupMessagesToSend(ProtoInstanceHandle instanceHandle,
																	  bool sendMouseWheelMessages = true, bool sendMouseButtonMessages = true, bool sendMouseMoveMessages = true, bool sendKeyboardPressMessages = true);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void StartFocusMessageLoop(ProtoInstanceHandle instanceHandle, int milliseconds = 5,
																		bool wm_activate = true, bool wm_activateapp = true, bool wm_ncactivate = true, bool wm_setfocus = true, bool wm_mouseactivate = true);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void StopFocusMessageLoop(ProtoInstanceHandle instanceHandle);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetDrawFakeCursor(ProtoInstanceHandle instanceHandle, bool enable);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetExternalFreezeFakeInput(ProtoInstanceHandle instanceHandle, bool enableFreeze);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void AddSelectedMouseHandle(ProtoInstanceHandle instanceHandle, uint mouseHandle);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void AddSelectedKeyboardHandle(ProtoInstanceHandle instanceHandle, uint keyboardHandle);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetControllerIndex(ProtoInstanceHandle instanceHandle, uint controllerIndex, uint controllerIndex2 = 0, uint controllerIndex3 = 0, uint controllerIndex4 = 0);

			// This MUST be called before calling InstallHook on the Xinput hook
			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetUseDinputRedirection(ProtoInstanceHandle instanceHandle, bool useRedirection);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetUseOpenXinput(ProtoInstanceHandle instanceHandle, bool useOpenXinput);

			// Both of these functions require RenameHandlesHookHookID hook
			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void AddHandleToRename(ProtoInstanceHandle instanceHandle, string name);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void AddNamedPipeToRename(ProtoInstanceHandle instanceHandle, string name);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetDinputDeviceGUID(ProtoInstanceHandle instanceHandle,
				ulong Data1,
				ushort Data2,
				ushort Data3,
				byte Data4a,
				byte Data4b,
				byte Data4c,
				byte Data4d,
				byte Data4e,
				byte Data4f,
				byte Data4g,
				byte Data4h);

			// This MUST be called before calling InstallHook on the Dinput order hook
			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void DinputHookAlsoHooksGetDeviceState(ProtoInstanceHandle instanceHandle, bool enable);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetSetWindowPosSettings(ProtoInstanceHandle instanceHandle, int posx, int posy, int width, int height);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetCreateSingleHIDName(ProtoInstanceHandle instanceHandle, string name);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetCursorClipOptions(ProtoInstanceHandle instanceHandle, bool useFakeClipCursor);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void AllowFakeCursorOutOfBounds(ProtoInstanceHandle instanceHandle, bool allowOutOfBounds, bool extendBounds);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetToggleFakeCursorVisibilityShortcut(ProtoInstanceHandle instanceHandle, bool enabled, uint vkey);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetRawInputBypass(ProtoInstanceHandle instanceHandle, bool enabled);

			[DllImport("ProtoInput\\ProtoInputLoader64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			internal static extern void SetShowCursorWhenImageUpdated(ProtoInstanceHandle instanceHandle, bool enabled);

			[DllImport("Kernel32.dll", SetLastError = true)]
			internal static extern int SetStdHandle(int device, SafeHandle handle);


		}


	}
}
