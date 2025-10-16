using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace TwitchWatcher.WPF.Themes
{
    public class WindowThemeHelper
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; 

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableDarkTitleBar(Window window)
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();
            int useDark = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }
    }
}
