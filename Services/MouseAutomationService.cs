using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AionDesktopAssistant.Services
{
    public class MouseAutomationService
    {
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
            GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
        }

        public void MoveMouse(int x, int y)
        {
            try
            {
                SetCursorPos(x, y);
                Thread.Sleep(50); // Small delay for stability
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to move mouse to ({x}, {y}): {ex.Message}", ex);
            }
        }

        public void MoveMouseSmooth(int fromX, int fromY, int toX, int toY, int steps = 20)
        {
            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    int x = fromX + (int)((toX - fromX) * (double)i / steps);
                    int y = fromY + (int)((toY - fromY) * (double)i / steps);

                    SetCursorPos(x, y);
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to move mouse smoothly: {ex.Message}", ex);
            }
        }

        public void Click()
        {
            Click(MouseButton.Left);
        }

        public void Click(MouseButton button)
        {
            try
            {
                var position = GetCurrentMousePosition();
                Click(position.X, position.Y, button);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to click mouse: {ex.Message}", ex);
            }
        }

        public void Click(int x, int y, MouseButton button = MouseButton.Left)
        {
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
                        throw new ArgumentException($"Unsupported mouse button: {button}");
                }

                mouse_event(downFlag, x, y, 0, 0);
                Thread.Sleep(50);
                mouse_event(upFlag, x, y, 0, 0);
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to click at ({x}, {y}): {ex.Message}", ex);
            }
        }

        public void DoubleClick()
        {
            DoubleClick(MouseButton.Left);
        }

        public void DoubleClick(MouseButton button)
        {
            try
            {
                Click(button);
                Thread.Sleep(50);
                Click(button);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to double-click: {ex.Message}", ex);
            }
        }

        public void DoubleClick(int x, int y, MouseButton button = MouseButton.Left)
        {
            try
            {
                Click(x, y, button);
                Thread.Sleep(50);
                Click(x, y, button);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to double-click at ({x}, {y}): {ex.Message}", ex);
            }
        }

        public void Drag(int fromX, int fromY, int toX, int toY, MouseButton button = MouseButton.Left)
        {
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

                mouse_event(downFlag, fromX, fromY, 0, 0);
                Thread.Sleep(100);

                // Drag to destination
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

                mouse_event(upFlag, toX, toY, 0, 0);
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to drag from ({fromX}, {fromY}) to ({toX}, {toY}): {ex.Message}", ex);
            }
        }

        public void Scroll(int scrollAmount)
        {
            try
            {
                var position = GetCurrentMousePosition();
                mouse_event(MOUSEEVENTF_WHEEL, position.X, position.Y, scrollAmount * 120, 0);
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to scroll: {ex.Message}", ex);
            }
        }

        public void ScrollUp(int clicks = 3)
        {
            Scroll(clicks);
        }

        public void ScrollDown(int clicks = 3)
        {
            Scroll(-clicks);
        }

        public Rectangle GetScreenBounds()
        {
            return Screen.PrimaryScreen.Bounds;
        }

        public bool IsPointOnScreen(int x, int y)
        {
            var bounds = GetScreenBounds();
            return x >= bounds.Left && x <= bounds.Right && y >= bounds.Top && y <= bounds.Bottom;
        }

        public Point ConvertClientToScreen(IntPtr windowHandle, Point clientPoint)
        {
            var point = new POINT { X = clientPoint.X, Y = clientPoint.Y };
            ClientToScreen(windowHandle, ref point);
            return new Point(point.X, point.Y);
        }
    }

    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }
}