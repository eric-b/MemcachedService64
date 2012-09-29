using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MemcachedService64
{
    static class NativeMethods
    {
        
       

        #region IsWow64Process
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public extern static IntPtr GetModuleHandle(string moduleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);
        #endregion


        #region PeekEscapeKey
        // Source: http://stackoverflow.com/questions/10342392/intercept-esc-without-removing-other-key-presses-from-buffer

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct InputRecord
        {
            internal short eventType;
            internal KeyEventRecord keyEvent;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct KeyEventRecord
        {
            internal bool keyDown;
            internal short repeatCount;
            internal short virtualKeyCode;
            internal short virtualScanCode;
            internal char uChar;
            internal int controlKeyState;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool PeekConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        static Nullable<InputRecord> PeekConsoleEvent()
        {
            InputRecord ir;
            int num;
            if (PeekConsoleInput(GetStdHandle(-10), out ir, 1, out num) && num != 0)
                return ir;
            return null;
        }
        static InputRecord ReadConsoleInput()
        {
            InputRecord ir;
            int num;
            ReadConsoleInput(GetStdHandle(-10), out ir, 1, out num);
            return ir;
        }

        public static bool PeekEscapeKey()
        {
            for (; ; )
            {
                var ev = PeekConsoleEvent();
                if (ev == null)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (ev.Value.eventType == 1 && ev.Value.keyEvent.keyDown && ev.Value.keyEvent.virtualKeyCode == 27)
                {
                    ReadConsoleInput();
                    return true;
                }
                if (ev.Value.eventType == 1 && ev.Value.keyEvent.keyDown)
                    return false;
                ReadConsoleInput();
            }
        }
        #endregion
    }
}
