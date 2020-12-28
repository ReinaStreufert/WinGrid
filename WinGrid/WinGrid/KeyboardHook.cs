using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinGrid
{
    public static class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Reference<bool> callNextHook = new Reference<bool>(true);
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys key = (Keys)Marshal.ReadInt32(lParam);
                Callback(KeyboardEventType.KeyDown, key, callNextHook);
            }
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                Keys key = (Keys)Marshal.ReadInt32(lParam);
                Callback(KeyboardEventType.KeyUp, key, callNextHook);
            }
            if (callNextHook.Value)
            {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        public static Action<KeyboardEventType, Keys, Reference<bool>> Callback;
        public static bool Running = false;

        public static void Start()
        {
            if (!Running)
            {
                _hookID = SetHook(_proc);
                Running = true;
            }
        }

        public static void Stop()
        {
            if (Running)
            {
                UnhookWindowsHookEx(_hookID);
                Running = false;
            }
        }
    }
    public enum KeyboardEventType : byte
    {
        KeyDown = 0,
        KeyUp = 1
    }
    public class Reference<T> where T : struct
    {
        public Reference(T Value)
        {
            this.Value = Value;
        }
        public T Value;
    }
}
