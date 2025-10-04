using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using Serilog;

namespace AionDesktopAssistant.Services
{
    public class WindowManagementService
    {
        private static readonly ILogger _logger = Log.ForContext<WindowManagementService>();

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

        private int _windowOperationCount = 0;
        private int _errorCount = 0;

        public WindowManagementService()
        {
            _logger.Information("🪟 WindowManagementService initialized");
            LogSystemInformation();
        }

        private void LogSystemInformation()
        {
            try
            {
                var currentWindow = GetCurrentWindowTitle();
                var openWindowsCount = GetOpenWindows().Count;
                _logger.Debug("🖥️ System state - Current window: '{CurrentWindow}', Open windows: {OpenWindowsCount}",
                    currentWindow, openWindowsCount);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "⚠️ Failed to log system information during initialization");
            }
        }

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

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
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Enumerating open windows...");

            try
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
                                    _logger.Debug("📋 Found window: '{Title}' ({ProcessName}) - Handle: {Handle:X}",
                                        windowTitle, process.ProcessName, hWnd.ToInt64());
                                }
                                catch (Exception ex)
                                {
                                    _logger.Debug("⚠️ Failed to get process info for window '{Title}': {Error}",
                                        windowTitle, ex.Message);
                                }
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                stopwatch.Stop();

                var result = windows
                    .Where(w => !string.IsNullOrEmpty(w.Title))
                    .OrderBy(w => w.Title)
                    .Select(w => $"{w.Title} ({w.ProcessName})")
                    .ToList();

                _logger.Information("✅ Window enumeration completed in {ElapsedMs}ms - Found {WindowCount} windows",
                    stopwatch.ElapsedMilliseconds, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to enumerate windows after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to enumerate windows: {ex.Message}", ex);
            }
        }

        public bool SwitchToWindow(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🎯 Attempting to switch to window: '{WindowTitle}'", windowTitle);

            try
            {
                var targetWindow = FindWindow(windowTitle);
                if (targetWindow != IntPtr.Zero)
                {
                    var isMinimized = IsIconic(targetWindow);
                    _logger.Debug("🔍 Target window found - Handle: {Handle:X}, IsMinimized: {IsMinimized}",
                        targetWindow.ToInt64(), isMinimized);

                    if (isMinimized)
                    {
                        _logger.Debug("📈 Restoring minimized window");
                        ShowWindow(targetWindow, SW_RESTORE);
                    }

                    _logger.Debug("⬆️ Bringing window to foreground");
                    SetForegroundWindow(targetWindow);

                    _windowOperationCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Successfully switched to window '{WindowTitle}' in {ElapsedMs}ms",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return true;
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ Window not found: '{WindowTitle}' (searched in {ElapsedMs}ms)",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to switch to window '{WindowTitle}' after {ElapsedMs}ms",
                    windowTitle, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to switch to window: {ex.Message}", ex);
            }
        }

        public bool SwitchToWindowByProcessName(string processName)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Searching for process: '{ProcessName}'", processName);

            try
            {
                var processes = Process.GetProcessesByName(processName);
                _logger.Debug("📊 Found {ProcessCount} processes with name '{ProcessName}'", processes.Length, processName);

                if (processes.Length > 0)
                {
                    var process = processes[0];
                    _logger.Debug("🎯 Using first process - PID: {ProcessId}, MainWindow: {WindowHandle:X}",
                        process.Id, process.MainWindowHandle.ToInt64());

                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        stopwatch.Stop();
                        var result = SwitchToWindowByHandle(process.MainWindowHandle);
                        _logger.Information("✅ Process window switch completed in {ElapsedMs}ms - Success: {Success}",
                            stopwatch.ElapsedMilliseconds, result);
                        return result;
                    }
                    else
                    {
                        stopwatch.Stop();
                        _logger.Warning("⚠️ Process '{ProcessName}' has no main window handle (searched in {ElapsedMs}ms)",
                            processName, stopwatch.ElapsedMilliseconds);
                        return false;
                    }
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ No processes found with name '{ProcessName}' (searched in {ElapsedMs}ms)",
                        processName, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to switch to process '{ProcessName}' after {ElapsedMs}ms",
                    processName, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to switch to process: {ex.Message}", ex);
            }
        }

        public bool SwitchToWindowByHandle(IntPtr windowHandle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🎯 Switching to window by handle: {WindowHandle:X}", windowHandle.ToInt64());

            try
            {
                var isMinimized = IsIconic(windowHandle);
                _logger.Debug("📊 Window state - IsMinimized: {IsMinimized}", isMinimized);

                if (isMinimized)
                {
                    _logger.Debug("📈 Restoring minimized window");
                    ShowWindow(windowHandle, SW_RESTORE);
                }

                _logger.Debug("⬆️ Setting window as foreground");
                SetForegroundWindow(windowHandle);

                _windowOperationCount++;
                stopwatch.Stop();

                _logger.Information("✅ Successfully switched to window by handle {WindowHandle:X} in {ElapsedMs}ms",
                    windowHandle.ToInt64(), stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to switch to window by handle {WindowHandle:X} after {ElapsedMs}ms",
                    windowHandle.ToInt64(), stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to switch to window by handle: {ex.Message}", ex);
            }
        }

        public string GetCurrentWindowTitle()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Getting current window title...");

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
                        var windowTitle = title.ToString();

                        stopwatch.Stop();
                        _logger.Information("✅ Current window title retrieved in {ElapsedMs}ms: '{WindowTitle}' (Handle: {Handle:X})",
                            stopwatch.ElapsedMilliseconds, windowTitle, foregroundWindow.ToInt64());
                        return windowTitle;
                    }
                    else
                    {
                        stopwatch.Stop();
                        _logger.Debug("📋 Current window has no title (Handle: {Handle:X}, checked in {ElapsedMs}ms)",
                            foregroundWindow.ToInt64(), stopwatch.ElapsedMilliseconds);
                        return string.Empty;
                    }
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ No foreground window found (checked in {ElapsedMs}ms)", stopwatch.ElapsedMilliseconds);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to get current window title after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to get current window title: {ex.Message}", ex);
            }
        }

        public IntPtr GetCurrentWindowHandle()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var handle = GetForegroundWindow();
                stopwatch.Stop();
                _logger.Debug("🎯 Current window handle retrieved in {ElapsedMs}ms: {Handle:X}",
                    stopwatch.ElapsedMilliseconds, handle.ToInt64());
                return handle;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to get current window handle after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public bool MinimizeWindow(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("📉 Attempting to minimize window: '{WindowTitle}'", windowTitle);

            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    _logger.Debug("🎯 Window found, minimizing - Handle: {Handle:X}", windowHandle.ToInt64());
                    ShowWindow(windowHandle, SW_MINIMIZE);

                    _windowOperationCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Window '{WindowTitle}' minimized successfully in {ElapsedMs}ms",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return true;
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ Window not found for minimizing: '{WindowTitle}' (searched in {ElapsedMs}ms)",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to minimize window '{WindowTitle}' after {ElapsedMs}ms",
                    windowTitle, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to minimize window: {ex.Message}", ex);
            }
        }

        public bool MaximizeWindow(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("📈 Attempting to maximize window: '{WindowTitle}'", windowTitle);

            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    _logger.Debug("🎯 Window found, maximizing - Handle: {Handle:X}", windowHandle.ToInt64());
                    ShowWindow(windowHandle, SW_SHOWMAXIMIZED);

                    _windowOperationCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Window '{WindowTitle}' maximized successfully in {ElapsedMs}ms",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return true;
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ Window not found for maximizing: '{WindowTitle}' (searched in {ElapsedMs}ms)",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to maximize window '{WindowTitle}' after {ElapsedMs}ms",
                    windowTitle, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to maximize window: {ex.Message}", ex);
            }
        }

        public bool RestoreWindow(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔄 Attempting to restore window: '{WindowTitle}'", windowTitle);

            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    _logger.Debug("🎯 Window found, restoring - Handle: {Handle:X}", windowHandle.ToInt64());
                    ShowWindow(windowHandle, SW_RESTORE);

                    _windowOperationCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Window '{WindowTitle}' restored successfully in {ElapsedMs}ms",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return true;
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ Window not found for restoring: '{WindowTitle}' (searched in {ElapsedMs}ms)",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to restore window '{WindowTitle}' after {ElapsedMs}ms",
                    windowTitle, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to restore window: {ex.Message}", ex);
            }
        }

        public bool CloseWindowByTitle(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("❌ Attempting to close window: '{WindowTitle}'", windowTitle);

            try
            {
                var windowHandle = FindWindow(windowTitle);
                if (windowHandle != IntPtr.Zero)
                {
                    _logger.Debug("🎯 Window found, closing - Handle: {Handle:X}", windowHandle.ToInt64());
                    CloseWindow(windowHandle);

                    _windowOperationCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Window '{WindowTitle}' closed successfully in {ElapsedMs}ms",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return true;
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ Window not found for closing: '{WindowTitle}' (searched in {ElapsedMs}ms)",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to close window '{WindowTitle}' after {ElapsedMs}ms",
                    windowTitle, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to close window: {ex.Message}", ex);
            }
        }

        public void ShowDesktop()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖥️ Attempting to show desktop...");

            try
            {
                _logger.Debug("🔍 Looking for shell window...");
                var shellWindow = FindWindow("Shell_TrayWnd");
                if (shellWindow != IntPtr.Zero)
                {
                    _logger.Debug("🎯 Shell window found, setting as foreground - Handle: {Handle:X}", shellWindow.ToInt64());
                    SetForegroundWindow(shellWindow);
                }

                _logger.Debug("⌨️ Using Windows+D keyboard shortcut as fallback");
                var keyboardService = new KeyboardAutomationService();
                keyboardService.PressKeyCombo(System.Windows.Forms.Keys.LWin, System.Windows.Forms.Keys.D);

                _windowOperationCount++;
                stopwatch.Stop();

                _logger.Information("✅ Show desktop completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to show desktop after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to show desktop: {ex.Message}", ex);
            }
        }

        public void CycleThroughWindows()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔄 Cycling through windows using Alt+Tab...");

            try
            {
                var keyboardService = new KeyboardAutomationService();
                keyboardService.AltTab();

                _windowOperationCount++;
                stopwatch.Stop();

                _logger.Information("✅ Window cycling completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to cycle through windows after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to cycle through windows: {ex.Message}", ex);
            }
        }

        public WindowInfo GetWindowInfo(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Getting detailed info for window: '{WindowTitle}'", windowTitle);

            try
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
                                    _logger.Debug("📋 Window info collected: '{Title}' - PID: {ProcessId}, Handle: {Handle:X}",
                                        windowInfo.Title, processId, hWnd.ToInt64());
                                }
                                catch (Exception ex)
                                {
                                    _logger.Debug("⚠️ Failed to get process info for window '{Title}': {Error}",
                                        title.ToString(), ex.Message);
                                }
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                stopwatch.Stop();

                var result = windows.FirstOrDefault();
                if (result != null)
                {
                    _logger.Information("✅ Window info retrieved in {ElapsedMs}ms: '{Title}' ({ProcessName}) - Minimized: {IsMinimized}, Maximized: {IsMaximized}",
                        stopwatch.ElapsedMilliseconds, result.Title, result.ProcessName, result.IsMinimized, result.IsMaximized);
                }
                else
                {
                    _logger.Warning("⚠️ No window info found for '{WindowTitle}' (searched in {ElapsedMs}ms)",
                        windowTitle, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to get window info for '{WindowTitle}' after {ElapsedMs}ms",
                    windowTitle, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private IntPtr FindWindow(string windowTitle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Searching for window: '{WindowTitle}'", windowTitle);

            try
            {
                IntPtr foundWindow = IntPtr.Zero;
                int windowsChecked = 0;

                EnumWindows((hWnd, lParam) =>
                {
                    windowsChecked++;
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
                                _logger.Debug("🎯 Window found after checking {WindowsChecked} windows - Handle: {Handle:X}, Title: '{Title}'",
                                    windowsChecked, hWnd.ToInt64(), title.ToString());
                                return false; // Stop enumeration
                            }
                        }
                    }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                stopwatch.Stop();

                if (foundWindow != IntPtr.Zero)
                {
                    _logger.Debug("✅ Window search completed in {ElapsedMs}ms - Found at handle {Handle:X}",
                        stopwatch.ElapsedMilliseconds, foundWindow.ToInt64());
                }
                else
                {
                    _logger.Debug("❌ Window search completed in {ElapsedMs}ms - Not found after checking {WindowsChecked} windows",
                        stopwatch.ElapsedMilliseconds, windowsChecked);
                }

                return foundWindow;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Window search failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private IntPtr FindWindow(string className, string windowName)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Searching for window by class and name - Class: '{ClassName}', Name: '{WindowName}'",
                className, windowName);

            try
            {
                var result = FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, windowName);
                stopwatch.Stop();

                _logger.Debug("🔍 Window search by class completed in {ElapsedMs}ms - Handle: {Handle:X}",
                    stopwatch.ElapsedMilliseconds, result.ToInt64());

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Window search by class failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public void LogStatistics()
        {
            _logger.Information("📊 WindowManagementService statistics - Operations: {OperationCount}, Errors: {ErrorCount}",
                _windowOperationCount, _errorCount);
        }
    }
}