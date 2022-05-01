using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StardewValleyCoop
{
    internal class ProcessThreadWindows
    {

		internal delegate bool EnumThreadDelegate(GCHandle hWnd, GCHandle lParam);

		internal static IEnumerable<GCHandle> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<GCHandle>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                NativeMethods.EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }

		internal static string GetTitleFromWindowHandle(GCHandle windowHandle)
        {
			int textLength = NativeMethods.GetWindowTextLength(windowHandle);
			StringBuilder title = new StringBuilder(textLength + 1);
			int result = NativeMethods.GetWindowText(windowHandle, title, title.Capacity);
			return title.ToString();
		}

		internal static GCHandle? FindVisibleWindowHandleWithTitleRegex(int processId, Regex regex)
        {
			foreach (GCHandle windowHandle in EnumerateProcessWindowHandles(processId))
			{
				if (NativeMethods.IsWindowVisible(windowHandle))
				{
					var title = GetTitleFromWindowHandle(windowHandle);
					if (regex.IsMatch(title))
					{
						return windowHandle;
					}
				}
			}
			return null;
		}

		internal class NativeMethods
        {

			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			internal static extern int GetWindowText(GCHandle hWnd, StringBuilder lpString,
				int nMaxCount);
			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			internal static extern int GetWindowTextLength(GCHandle hWnd);


			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool IsWindowVisible(GCHandle handle);

			[DllImport("user32.dll")]
			internal static extern GCHandle SetWindowLongPtr(GCHandle hWnd, int nIndex, GCHandle dwNewLong);

			[DllImport("user32.dll")]
			internal static extern GCHandle SetWindowPos(GCHandle hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);


			[DllImport("user32.dll")]
			internal static extern GCHandle GetWindowLongPtr(GCHandle hWnd, int nIndex);

			[DllImport("user32.dll")]
			internal static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
				IntPtr lParam);
		}
	}
}
