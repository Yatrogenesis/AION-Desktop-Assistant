using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class MouseAutomationService
    {
        private static readonly ILogger _logger = Log.ForContext<MouseAutomationService>();

        // Windows API constants
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;
        private const int MOUSEEVENTF_WHEEL = 0x800;
        private const int MOUSEEVENTF_MOVE = 0x01;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        private int _actionCount = 0;
        private int _errorCount = 0;

        public MouseAutomationService()
        {
            _logger.Information("🖱️ MouseAutomationService initialized");
            var screenBounds = Screen.PrimaryScreen.Bounds;
            _logger.Debug("📏 Screen bounds: {Width}x{Height}", screenBounds.Width, screenBounds.Height);
        }

        // Windows API declarations
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public Point GetCurrentMousePosition()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("📍 Getting current mouse position...");

            try
            {
                GetCursorPos(out POINT point);
                var position = new Point(point.X, point.Y);
                stopwatch.Stop();

                _logger.Debug("✅ Mouse position retrieved in {ElapsedMs}ms: ({X}, {Y})",
                    stopwatch.ElapsedMilliseconds, position.X, position.Y);

                return position;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to get mouse position after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public void MoveMouse(int x, int y)
        {
            var stopwatch = Stopwatch.StartNew();
            var currentPos = GetCurrentMousePosition();
            _logger.Debug("🎯 Moving mouse from ({FromX}, {FromY}) to ({ToX}, {ToY})",
                currentPos.X, currentPos.Y, x, y);

            try
            {
                if (!IsPointOnScreen(x, y))
                {
                    _logger.Warning("⚠️ Target position ({X}, {Y}) is outside screen bounds", x, y);
                }

                SetCursorPos(x, y);
                Thread.Sleep(50); // Small delay for stability
                _actionCount++;
                stopwatch.Stop();

                var distance = Math.Sqrt(Math.Pow(x - currentPos.X, 2) + Math.Pow(y - currentPos.Y, 2));
                _logger.Information("✅ Mouse moved in {ElapsedMs}ms to ({X}, {Y}) - Distance: {Distance:F1}px",
                    stopwatch.ElapsedMilliseconds, x, y, distance);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to move mouse to ({X}, {Y}) after {ElapsedMs}ms", x, y, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to move mouse to ({x}, {y}): {ex.Message}", ex);
            }
        }

        public void MoveMouseSmooth(int fromX, int fromY, int toX, int toY, int steps = 20)
        {
            var stopwatch = Stopwatch.StartNew();
            var distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));

            _logger.Debug("🌊 Starting smooth mouse movement from ({FromX}, {FromY}) to ({ToX}, {ToY}) in {Steps} steps - Distance: {Distance:F1}px",
                fromX, fromY, toX, toY, steps, distance);

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    int x = fromX + (int)((toX - fromX) * (double)i / steps);
                    int y = fromY + (int)((toY - fromY) * (double)i / steps);

                    SetCursorPos(x, y);
                    Thread.Sleep(10);

                    if (i % 5 == 0) // Log progress every 5 steps
                    {
                        _logger.Debug("🎯 Smooth movement step {Step}/{Steps}: ({X}, {Y})", i, steps, x, y);
                    }
                }

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ Smooth mouse movement completed in {ElapsedMs}ms - {Steps} steps, {Distance:F1}px",
                    stopwatch.ElapsedMilliseconds, steps, distance);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Smooth mouse movement failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to move mouse smoothly: {ex.Message}", ex);
            }
        }

        public void Click()
        {
            _logger.Debug("🖱️ Performing default left click at current position");
            Click(MouseButton.Left);
        }

        public void Click(MouseButton button)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖱️ Starting {Button} click at current position", button);

            try
            {
                var position = GetCurrentMousePosition();
                Click(position.X, position.Y, button);
                stopwatch.Stop();

                _logger.Information("✅ {Button} click completed in {ElapsedMs}ms at current position ({X}, {Y})",
                    button, stopwatch.ElapsedMilliseconds, position.X, position.Y);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ {Button} click failed after {ElapsedMs}ms", button, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to click mouse: {ex.Message}", ex);
            }
        }

        public void Click(int x, int y, MouseButton button = MouseButton.Left)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖱️ Starting {Button} click at ({X}, {Y})", button, x, y);

            try
            {
                MoveMouse(x, y);
                Thread.Sleep(100);

                int downFlag, upFlag;
                switch (button)
                {
                    case MouseButton.Left:
                        downFlag = MOUSEEVENTF_LEFTDOWN;
                        upFlag = MOUSEEVENTF_LEFTUP;
                        break;
                    case MouseButton.Right:
                        downFlag = MOUSEEVENTF_RIGHTDOWN;
                        upFlag = MOUSEEVENTF_RIGHTUP;
                        break;
                    case MouseButton.Middle:
                        downFlag = MOUSEEVENTF_MIDDLEDOWN;
                        upFlag = MOUSEEVENTF_MIDDLEUP;
                        break;
                    default:
                        _logger.Error("❌ Unsupported mouse button: {Button}", button);
                        throw new ArgumentException($"Unsupported mouse button: {button}");
                }

                _logger.Debug("⬇️ Mouse {Button} down at ({X}, {Y})", button, x, y);
                mouse_event(downFlag, x, y, 0, 0);
                Thread.Sleep(50);

                _logger.Debug("⬆️ Mouse {Button} up at ({X}, {Y})", button, x, y);
                mouse_event(upFlag, x, y, 0, 0);
                Thread.Sleep(50);

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ {Button} click completed in {ElapsedMs}ms at ({X}, {Y})",
                    button, stopwatch.ElapsedMilliseconds, x, y);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ {Button} click failed at ({X}, {Y}) after {ElapsedMs}ms",
                    button, x, y, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to click at ({x}, {y}): {ex.Message}", ex);
            }
        }

        public void DoubleClick()
        {
            _logger.Debug("🖱️ Performing default left double-click at current position");
            DoubleClick(MouseButton.Left);
        }

        public void DoubleClick(MouseButton button)
        {
            var stopwatch = Stopwatch.StartNew();
            var position = GetCurrentMousePosition();
            _logger.Debug("🖱️ Starting {Button} double-click at current position ({X}, {Y})", button, position.X, position.Y);

            try
            {
                Click(button);
                Thread.Sleep(50);
                Click(button);

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ {Button} double-click completed in {ElapsedMs}ms at ({X}, {Y})",
                    button, stopwatch.ElapsedMilliseconds, position.X, position.Y);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ {Button} double-click failed after {ElapsedMs}ms", button, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to double-click: {ex.Message}", ex);
            }
        }

        public void DoubleClick(int x, int y, MouseButton button = MouseButton.Left)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖱️ Starting {Button} double-click at ({X}, {Y})", button, x, y);

            try
            {
                Click(x, y, button);
                Thread.Sleep(50);
                Click(x, y, button);

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ {Button} double-click completed in {ElapsedMs}ms at ({X}, {Y})",
                    button, stopwatch.ElapsedMilliseconds, x, y);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ {Button} double-click failed at ({X}, {Y}) after {ElapsedMs}ms",
                    button, x, y, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to double-click at ({x}, {y}): {ex.Message}", ex);
            }
        }

        public void Drag(int fromX, int fromY, int toX, int toY, MouseButton button = MouseButton.Left)
        {
            var stopwatch = Stopwatch.StartNew();
            var distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));

            _logger.Debug("🔄 Starting {Button} drag from ({FromX}, {FromY}) to ({ToX}, {ToY}) - Distance: {Distance:F1}px",
                button, fromX, fromY, toX, toY, distance);

            try
            {
                MoveMouse(fromX, fromY);
                Thread.Sleep(100);

                // Press down
                int downFlag = button switch
                {
                    MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
                    MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
                    MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
                    _ => throw new ArgumentException($"Unsupported mouse button: {button}")
                };

                _logger.Debug("⬇️ {Button} button down at ({X}, {Y})", button, fromX, fromY);
                mouse_event(downFlag, fromX, fromY, 0, 0);
                Thread.Sleep(100);

                // Drag to destination
                _logger.Debug("🌊 Dragging to destination...");
                MoveMouseSmooth(fromX, fromY, toX, toY, 30);
                Thread.Sleep(100);

                // Release
                int upFlag = button switch
                {
                    MouseButton.Left => MOUSEEVENTF_LEFTUP,
                    MouseButton.Right => MOUSEEVENTF_RIGHTUP,
                    MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
                    _ => throw new ArgumentException($"Unsupported mouse button: {button}")
                };

                _logger.Debug("⬆️ {Button} button up at ({X}, {Y})", button, toX, toY);
                mouse_event(upFlag, toX, toY, 0, 0);
                Thread.Sleep(100);

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ {Button} drag completed in {ElapsedMs}ms from ({FromX}, {FromY}) to ({ToX}, {ToY}) - Distance: {Distance:F1}px",
                    button, stopwatch.ElapsedMilliseconds, fromX, fromY, toX, toY, distance);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ {Button} drag failed after {ElapsedMs}ms from ({FromX}, {FromY}) to ({ToX}, {ToY})",
                    button, stopwatch.ElapsedMilliseconds, fromX, fromY, toX, toY);
                throw new InvalidOperationException($"Failed to drag from ({fromX}, {fromY}) to ({toX}, {toY}): {ex.Message}", ex);
            }
        }

        public void Scroll(int scrollAmount)
        {
            var stopwatch = Stopwatch.StartNew();
            var position = GetCurrentMousePosition();
            var direction = scrollAmount > 0 ? "up" : "down";
            var absoluteAmount = Math.Abs(scrollAmount);

            _logger.Debug("🎡 Starting scroll {Direction} by {Amount} at ({X}, {Y})",
                direction, absoluteAmount, position.X, position.Y);

            try
            {
                mouse_event(MOUSEEVENTF_WHEEL, position.X, position.Y, scrollAmount * 120, 0);
                Thread.Sleep(50);

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ Scroll {Direction} completed in {ElapsedMs}ms - Amount: {Amount} at ({X}, {Y})",
                    direction, stopwatch.ElapsedMilliseconds, absoluteAmount, position.X, position.Y);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Scroll failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to scroll: {ex.Message}", ex);
            }
        }

        public void ScrollUp(int clicks = 3)
        {
            _logger.Debug("⬆️ Scrolling up {Clicks} clicks", clicks);
            Scroll(clicks);
        }

        public void ScrollDown(int clicks = 3)
        {
            _logger.Debug("⬇️ Scrolling down {Clicks} clicks", clicks);
            Scroll(-clicks);
        }

        public Rectangle GetScreenBounds()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            _logger.Debug("📏 Screen bounds: {Width}x{Height} at ({X}, {Y})",
                bounds.Width, bounds.Height, bounds.X, bounds.Y);
            return bounds;
        }

        public bool IsPointOnScreen(int x, int y)
        {
            var bounds = GetScreenBounds();
            var isOnScreen = x >= bounds.Left && x <= bounds.Right && y >= bounds.Top && y <= bounds.Bottom;

            _logger.Debug("📍 Point ({X}, {Y}) is {Status} screen bounds",
                x, y, isOnScreen ? "within" : "outside");

            return isOnScreen;
        }

        public Point ConvertClientToScreen(IntPtr windowHandle, Point clientPoint)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔄 Converting client point ({X}, {Y}) to screen coordinates for window {Handle:X}",
                clientPoint.X, clientPoint.Y, windowHandle.ToInt64());

            try
            {
                var point = new POINT { X = clientPoint.X, Y = clientPoint.Y };
                ClientToScreen(windowHandle, ref point);
                var screenPoint = new Point(point.X, point.Y);

                stopwatch.Stop();

                _logger.Information("✅ Client-to-screen conversion completed in {ElapsedMs}ms: ({ClientX}, {ClientY}) -> ({ScreenX}, {ScreenY})",
                    stopwatch.ElapsedMilliseconds, clientPoint.X, clientPoint.Y, screenPoint.X, screenPoint.Y);

                return screenPoint;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Client-to-screen conversion failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public void LogStatistics()
        {
            _logger.Information("📊 MouseAutomationService statistics - Actions: {ActionCount}, Errors: {ErrorCount}",
                _actionCount, _errorCount);
        }
    }

    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }
}