using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace AionDesktopAssistant.Services
{
    public class WindowManagementService
    {
        // Windows API constants
        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWMINNOACTIVE = 7;
        private const int SW_SHOWNA = 8;
        private const int SW_RESTORE = 9;

        // Windows API declarations
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool CloseWindow(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; } = string.Empty;
            public uint ProcessId { get; set; }
            public string ProcessName { get; set; } = string.Empty;
            public bool IsVisible { get; set; }
            public bool IsMinimized { get; set; }
            public bool IsMaximized { get; set; }
        }

        public List<string> GetOpenWindows()
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        GetWindowText(hWnd, title, title.Capacity);

                        string windowTitle = title.ToString();
                        if (!string.IsNullOrWhiteSpace(windowTitle) && windowTitle != "Program Manager")
                        {
                            GetWindowThreadProcessId(hWnd, out uint processId);

                            try
                            {
                                var process = Process.GetProcessById((int)processId);
                                var windowInfo = new WindowInfo
                                {
                                    Handle = hWnd,
                                    Title = windowTitle,
                                    ProcessId = processId,
                                    ProcessName = process.ProcessName,
                                    IsVisible = IsWindowVisible(hWnd),
                                    IsMinimized = IsIconic(hWnd),
                                    IsMaximized = IsZoomed(hWnd)
                                };

                                windows.Add(windowInfo);
                            }
                            catch (Exception)
                            {
                                // Process might have closed, ignore
                            }
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows
                .Where(w => !string.IsNullOrEmpty(w.Title))
                .OrderBy(w => w.Title)
                .Select(w => $"{w.Title} ({w.ProcessName})")
                .ToList();
        }

        public bool SwitchToWindow(string windowTitle)
        {
            try
            {
                var targetWindow = FindWindow(windowTitle);
                if (targetWindow != IntPtr.Zero)
                {
                    // If window is minimized, restore it first
                    if (IsIconic(targetWindow))
                    {
                        ShowWindow(targetWindow, SW_RESTORE);
                    }

                    // Bring window to foreground
                    SetForegroundWindow(targetWindow);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to switch to window: {ex.Message}", ex);
            }
        }

        public bool SwitchToWindowByProcessName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    var process = processes[0];
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return SwitchToWindowByHandle(process.MainWindowHandle);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to switch to process: {ex.Message}", ex);
            }
        }

        public bool SwitchToWindowByHandle(IntPtr windowHandle)
        {
            try
            {
                if (IsIconic(windowHandle))
                {
                    ShowWindow(windowHandle, SW_RESTORE);
                }

                SetForegroundWindow(windowHandle);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to switch to window by handle: {ex.Message}", ex);
            }
        }

        public string GetCurrentWindowTitle()
        {
            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero)
                {
                    int length = GetWindowTextLength(foregroundWindow);
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        GetWindowText(foregroundWindow, title, title.Capacity);
                        return title.ToString();
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current window title: {ex.Message}", ex);
            }
        }

        public IntPtr GetCurrentWindowHandle()
        {
            return GetForegroundWindow();
        }

        public bool MinimizeWindow(string windowTitle)
        {
            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    ShowWindow(windowHandle, SW_MINIMIZE);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to minimize window: {ex.Message}", ex);
            }
        }

        public bool MaximizeWindow(string windowTitle)
        {
            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    ShowWindow(windowHandle, SW_SHOWMAXIMIZED);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to maximize window: {ex.Message}", ex);
            }
        }

        public bool RestoreWindow(string windowTitle)
        {
            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    ShowWindow(windowHandle, SW_RESTORE);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore window: {ex.Message}", ex);
            }
        }

        public bool CloseWindowByTitle(string windowTitle)
        {
            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    CloseWindow(windowHandle);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to close window: {ex.Message}", ex);
            }
        }

        public void ShowDesktop()
        {
            try
            {
                // Find the shell window and minimize all windows
                var shellWindow = FindWindow("Shell_TrayWnd");
                if (shellWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(shellWindow);
                }

                // Alternative method using Windows key + D
                var keyboardService = new KeyboardAutomationService();
                keyboardService.PressKeyCombo(System.Windows.Forms.Keys.LWin, System.Windows.Forms.Keys.D);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to show desktop: {ex.Message}", ex);
            }
        }

        public void CycleThroughWindows()
        {
            try
            {
                var keyboardService = new KeyboardAutomationService();
                keyboardService.AltTab();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to cycle through windows: {ex.Message}", ex);
            }
        }

        public WindowInfo GetWindowInfo(string windowTitle)
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        GetWindowText(hWnd, title, title.Capacity);

                        if (title.ToString().Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            GetWindowThreadProcessId(hWnd, out uint processId);

                            try
                            {
                                var process = Process.GetProcessById((int)processId);
                                var windowInfo = new WindowInfo
                                {
                                    Handle = hWnd,
                                    Title = title.ToString(),
                                    ProcessId = processId,
                                    ProcessName = process.ProcessName,
                                    IsVisible = IsWindowVisible(hWnd),
                                    IsMinimized = IsIconic(hWnd),
                                    IsMaximized = IsZoomed(hWnd)
                                };

                                windows.Add(windowInfo);
                            }
                            catch (Exception)
                            {
                                // Process might have closed, ignore
                            }
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows.FirstOrDefault();
        }

        private IntPtr FindWindow(string windowTitle)
        {
            IntPtr foundWindow = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        GetWindowText(hWnd, title, title.Capacity);

                        if (title.ToString().Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            foundWindow = hWnd;
                            return false; // Stop enumeration
                        }
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);

            return foundWindow;
        }

        private IntPtr FindWindow(string className, string windowName)
        {
            return FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, windowName);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    }
}