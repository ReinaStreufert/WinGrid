using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinGrid
{
    class ScreenLayer
    {
        public string ScreenDeviceName;
        public int Index;
        public List<Layer> Layers = new List<Layer>();
        public int CurrentLayer = 0;
        public ScreenLayer(string ScreenDeviceName, int Index)
        {
            this.ScreenDeviceName = ScreenDeviceName;
            Layers.Add(new WinGrid.Layer());
            this.Index = Index;
        }
    }
    public class Layer
    {
        public delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        public bool EnumCallback(IntPtr hWnd, int lParam)
        {
            if (WindowUtils.IsWindowVisible(hWnd) && hWnd != Form1.hwnd)
            {
                Rectangle windowRect = WindowUtils.GetWindowRectangle(hWnd, false);
                ActiveWindowPreset windowInfo = new ActiveWindowPreset(hWnd, windowRect.X, windowRect.Y, windowRect.Width, windowRect.Height);
                Windows.Add(windowInfo);
            }
            return true;
        }

        public List<ActiveWindowPreset> Windows = new List<ActiveWindowPreset>();

        public void SaveCurrentState()
        {
            Windows.Clear();
            EnumDesktopWindows(IntPtr.Zero, EnumCallback, IntPtr.Zero);
        }

        public void RestoreState()
        {
            foreach (ActiveWindowPreset windowInfo in Windows)
            {
                WindowUtils.SetWindowPos(windowInfo.hwnd, IntPtr.Zero, windowInfo.X, windowInfo.Y, windowInfo.CX, windowInfo.CY, 0);
            }
        }
    }
    public class ActiveWindowPreset
    {
        public IntPtr hwnd;
        public int X;
        public int Y;
        public int CX;
        public int CY;
        public ActiveWindowPreset(IntPtr hwnd, int X, int Y, int CX, int CY)
        {
            this.hwnd = hwnd;
            this.X = X;
            this.Y = Y;
            this.CX = CX;
            this.CY = CY;
        }
    }
}
