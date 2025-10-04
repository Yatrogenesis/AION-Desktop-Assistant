using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class KeyboardAutomationService
    {
        private static readonly ILogger _logger = Log.ForContext<KeyboardAutomationService>();

        // Windows API constants
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key

        private int _keyPressCount = 0;
        private int _textTypedCharacters = 0;
        private int _errorCount = 0;

        public KeyboardAutomationService()
        {
            _logger.Information("⌨️ KeyboardAutomationService initialized");
            var capsLock = IsCapsLockOn();
            var numLock = IsNumLockOn();
            var scrollLock = IsScrollLockOn();
            _logger.Debug("🔠 Keyboard state - CapsLock: {CapsLock}, NumLock: {NumLock}, ScrollLock: {ScrollLock}",
                capsLock, numLock, scrollLock);
        }

        // Windows API declarations
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // Key mapping for special characters
        private static readonly Dictionary<char, (Keys key, bool shift)> CharacterMap = new()
        {
            // Numbers
            {'0', (Keys.D0, false)}, {'1', (Keys.D1, false)}, {'2', (Keys.D2, false)}, {'3', (Keys.D3, false)},
            {'4', (Keys.D4, false)}, {'5', (Keys.D5, false)}, {'6', (Keys.D6, false)}, {'7', (Keys.D7, false)},
            {'8', (Keys.D8, false)}, {'9', (Keys.D9, false)},

            // Special characters that require shift
            {'!', (Keys.D1, true)}, {'@', (Keys.D2, true)}, {'#', (Keys.D3, true)}, {'$', (Keys.D4, true)},
            {'%', (Keys.D5, true)}, {'^', (Keys.D6, true)}, {'&', (Keys.D7, true)}, {'*', (Keys.D8, true)},
            {'(', (Keys.D9, true)}, {')', (Keys.D0, true)},

            // Punctuation
            {' ', (Keys.Space, false)}, {'.', (Keys.OemPeriod, false)}, {',', (Keys.Oemcomma, false)},
            {';', (Keys.OemSemicolon, false)}, {':', (Keys.OemSemicolon, true)},
            {'\'', (Keys.OemQuotes, false)}, {'"', (Keys.OemQuotes, true)},
            {'[', (Keys.OemOpenBrackets, false)}, {']', (Keys.OemCloseBrackets, false)},
            {'{', (Keys.OemOpenBrackets, true)}, {'}', (Keys.OemCloseBrackets, true)},
            {'\\', (Keys.OemBackslash, false)}, {'|', (Keys.OemBackslash, true)},
            {'/', (Keys.OemQuestion, false)}, {'?', (Keys.OemQuestion, true)},
            {'`', (Keys.Oemtilde, false)}, {'~', (Keys.Oemtilde, true)},
            {'-', (Keys.OemMinus, false)}, {'_', (Keys.OemMinus, true)},
            {'=', (Keys.Oemplus, false)}, {'+', (Keys.Oemplus, true)},
            {'<', (Keys.Oemcomma, true)}, {'>', (Keys.OemPeriod, true)}
        };

        public void TypeText(string text)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("📝 Starting to type text - Length: {Length} characters", text?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    _logger.Warning("⚠️ Attempted to type null or empty text");
                    return;
                }

                var preview = text.Length > 50 ? text.Substring(0, 50) + "..." : text;
                _logger.Debug("📄 Text preview: '{Preview}'", preview);

                foreach (char c in text)
                {
                    TypeCharacter(c);
                    Thread.Sleep(20); // Small delay between characters for stability
                }

                _textTypedCharacters += text.Length;
                stopwatch.Stop();

                _logger.Information("✅ Text typed successfully in {ElapsedMs}ms - {Length} characters, Total session: {TotalChars}",
                    stopwatch.ElapsedMilliseconds, text.Length, _textTypedCharacters);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to type text after {ElapsedMs}ms - Length: {Length}",
                    stopwatch.ElapsedMilliseconds, text?.Length ?? 0);
                throw new InvalidOperationException($"Failed to type text: {ex.Message}", ex);
            }
        }

        public void TypeCharacter(char character)
        {
            var stopwatch = Stopwatch.StartNew();
            var charDisplay = character == ' ' ? "SPACE" : character.ToString();
            _logger.Debug("🔤 Typing character: '{Character}' (Code: {CharCode})", charDisplay, (int)character);

            try
            {
                if (character == '\n' || character == '\r')
                {
                    _logger.Debug("⏎ Detected newline character, pressing Enter");
                    PressKey(Keys.Enter);
                    stopwatch.Stop();
                    _logger.Debug("✅ Newline character processed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return;
                }

                if (character == '\t')
                {
                    _logger.Debug("⭾ Detected tab character, pressing Tab");
                    PressKey(Keys.Tab);
                    stopwatch.Stop();
                    _logger.Debug("✅ Tab character processed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Handle uppercase letters
                if (char.IsUpper(character))
                {
                    var key = (Keys)Enum.Parse(typeof(Keys), character.ToString().ToUpper());
                    _logger.Debug("🔠 Uppercase letter detected: '{Character}' -> Key: {Key} with Shift", character, key);
                    PressKey(key, true); // With shift
                    stopwatch.Stop();
                    _logger.Debug("✅ Uppercase character typed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Handle lowercase letters
                if (char.IsLower(character))
                {
                    var key = (Keys)Enum.Parse(typeof(Keys), character.ToString().ToUpper());
                    _logger.Debug("🔡 Lowercase letter detected: '{Character}' -> Key: {Key}", character, key);
                    PressKey(key, false); // Without shift
                    stopwatch.Stop();
                    _logger.Debug("✅ Lowercase character typed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Handle special characters
                if (CharacterMap.ContainsKey(character))
                {
                    var (key, needsShift) = CharacterMap[character];
                    var shiftStatus = needsShift ? "with Shift" : "without Shift";
                    _logger.Debug("🔣 Special character detected: '{Character}' -> Key: {Key} {ShiftStatus}",
                        charDisplay, key, shiftStatus);
                    PressKey(key, needsShift);
                    stopwatch.Stop();
                    _logger.Debug("✅ Special character typed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Fallback for unknown characters
                stopwatch.Stop();
                _errorCount++;
                _logger.Error("❌ Unsupported character: '{Character}' (Code: {CharCode})", charDisplay, (int)character);
                throw new ArgumentException($"Unsupported character: '{character}'");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to type character '{Character}' after {ElapsedMs}ms", charDisplay, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to type character '{character}': {ex.Message}", ex);
            }
        }

        public void PressKey(Keys key, bool withShift = false)
        {
            var stopwatch = Stopwatch.StartNew();
            var shiftStatus = withShift ? " + Shift" : "";
            _logger.Debug("🎯 Pressing key: {Key}{ShiftStatus}", key, shiftStatus);

            try
            {
                if (withShift)
                {
                    _logger.Debug("⬇️ Shift key down");
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(10);
                }

                _logger.Debug("⬇️ Key {Key} down", key);
                keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
                Thread.Sleep(50);

                _logger.Debug("⬆️ Key {Key} up", key);
                keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);

                if (withShift)
                {
                    Thread.Sleep(10);
                    _logger.Debug("⬆️ Shift key up");
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                }

                Thread.Sleep(20);
                _keyPressCount++;
                stopwatch.Stop();

                _logger.Information("✅ Key pressed successfully in {ElapsedMs}ms: {Key}{ShiftStatus} - Total session: {TotalKeys}",
                    stopwatch.ElapsedMilliseconds, key, shiftStatus, _keyPressCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to press key {Key}{ShiftStatus} after {ElapsedMs}ms",
                    key, shiftStatus, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to press key {key}: {ex.Message}", ex);
            }
        }

        public void PressKeyCombo(params Keys[] keys)
        {
            var stopwatch = Stopwatch.StartNew();
            var keyNames = string.Join(" + ", keys);
            _logger.Debug("🎹 Pressing key combination: {KeyCombo} ({KeyCount} keys)", keyNames, keys.Length);

            try
            {
                _logger.Debug("⬇️ Pressing down all keys in combination");
                foreach (var key in keys)
                {
                    keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(10);
                }

                Thread.Sleep(50);

                _logger.Debug("⬆️ Releasing all keys in reverse order");
                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    keybd_event((byte)keys[i], 0, KEYEVENTF_KEYUP, 0);
                    Thread.Sleep(10);
                }

                Thread.Sleep(50);
                _keyPressCount++;
                stopwatch.Stop();

                _logger.Information("✅ Key combination executed successfully in {ElapsedMs}ms: {KeyCombo}",
                    stopwatch.ElapsedMilliseconds, keyNames);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to press key combination {KeyCombo} after {ElapsedMs}ms",
                    keyNames, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to press key combination: {ex.Message}", ex);
            }
        }

        // Common shortcuts
        public void Copy()
        {
            _logger.Debug("📋 Executing Copy shortcut (Ctrl+C)");
            PressKeyCombo(Keys.Control, Keys.C);
        }

        public void Paste()
        {
            _logger.Debug("📋 Executing Paste shortcut (Ctrl+V)");
            PressKeyCombo(Keys.Control, Keys.V);
        }

        public void Cut()
        {
            _logger.Debug("✂️ Executing Cut shortcut (Ctrl+X)");
            PressKeyCombo(Keys.Control, Keys.X);
        }

        public void SelectAll()
        {
            _logger.Debug("🔍 Executing Select All shortcut (Ctrl+A)");
            PressKeyCombo(Keys.Control, Keys.A);
        }

        public void Undo()
        {
            _logger.Debug("↩️ Executing Undo shortcut (Ctrl+Z)");
            PressKeyCombo(Keys.Control, Keys.Z);
        }

        public void Redo()
        {
            _logger.Debug("↪️ Executing Redo shortcut (Ctrl+Y)");
            PressKeyCombo(Keys.Control, Keys.Y);
        }

        public void Save()
        {
            _logger.Debug("💾 Executing Save shortcut (Ctrl+S)");
            PressKeyCombo(Keys.Control, Keys.S);
        }

        public void Find()
        {
            _logger.Debug("🔍 Executing Find shortcut (Ctrl+F)");
            PressKeyCombo(Keys.Control, Keys.F);
        }

        public void NewTab()
        {
            _logger.Debug("🆕 Executing New Tab shortcut (Ctrl+T)");
            PressKeyCombo(Keys.Control, Keys.T);
        }

        public void CloseTab()
        {
            _logger.Debug("❌ Executing Close Tab shortcut (Ctrl+W)");
            PressKeyCombo(Keys.Control, Keys.W);
        }

        public void SwitchTab()
        {
            _logger.Debug("🔄 Executing Switch Tab shortcut (Ctrl+Tab)");
            PressKeyCombo(Keys.Control, Keys.Tab);
        }

        public void AltTab()
        {
            _logger.Debug("🔄 Executing Alt+Tab (Window switch)");
            PressKeyCombo(Keys.Alt, Keys.Tab);
        }

        public void WindowsKey()
        {
            _logger.Debug("🪟 Pressing Windows key");
            PressKey(Keys.LWin);
        }

        public void TaskManager()
        {
            _logger.Debug("📊 Executing Task Manager shortcut (Ctrl+Shift+Esc)");
            PressKeyCombo(Keys.Control, Keys.Shift, Keys.Escape);
        }

        public void Screenshot()
        {
            _logger.Debug("📸 Executing Screenshot shortcut (Win+PrintScreen)");
            PressKeyCombo(Keys.LWin, Keys.PrintScreen);
        }

        public void SnippingTool()
        {
            _logger.Debug("✂️ Executing Snipping Tool shortcut (Win+Shift+S)");
            PressKeyCombo(Keys.LWin, Keys.Shift, Keys.S);
        }

        // Navigation keys
        public void ArrowUp()
        {
            _logger.Debug("⬆️ Pressing Arrow Up");
            PressKey(Keys.Up);
        }

        public void ArrowDown()
        {
            _logger.Debug("⬇️ Pressing Arrow Down");
            PressKey(Keys.Down);
        }

        public void ArrowLeft()
        {
            _logger.Debug("⬅️ Pressing Arrow Left");
            PressKey(Keys.Left);
        }

        public void ArrowRight()
        {
            _logger.Debug("➡️ Pressing Arrow Right");
            PressKey(Keys.Right);
        }

        public void Home()
        {
            _logger.Debug("🏠 Pressing Home key");
            PressKey(Keys.Home);
        }

        public void End()
        {
            _logger.Debug("🔚 Pressing End key");
            PressKey(Keys.End);
        }

        public void PageUp()
        {
            _logger.Debug("⬆️ Pressing Page Up");
            PressKey(Keys.PageUp);
        }

        public void PageDown()
        {
            _logger.Debug("⬇️ Pressing Page Down");
            PressKey(Keys.PageDown);
        }

        public void Backspace()
        {
            _logger.Debug("⌫ Pressing Backspace");
            PressKey(Keys.Back);
        }

        public void Delete()
        {
            _logger.Debug("🗑️ Pressing Delete");
            PressKey(Keys.Delete);
        }

        public void Enter()
        {
            _logger.Debug("⏎ Pressing Enter");
            PressKey(Keys.Enter);
        }

        public void Tab()
        {
            _logger.Debug("⭾ Pressing Tab");
            PressKey(Keys.Tab);
        }

        public void Escape()
        {
            _logger.Debug("⎋ Pressing Escape");
            PressKey(Keys.Escape);
        }

        // Utility methods
        public bool IsKeyPressed(Keys key)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var isPressed = (GetKeyState((int)key) & 0x8000) != 0;
                stopwatch.Stop();

                _logger.Debug("🔍 Key state check for {Key}: {Status} (checked in {ElapsedMs}ms)",
                    key, isPressed ? "PRESSED" : "NOT PRESSED", stopwatch.ElapsedMilliseconds);

                return isPressed;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to check key state for {Key} after {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public bool IsCapsLockOn()
        {
            var isOn = (GetKeyState((int)Keys.CapsLock) & 0x0001) != 0;
            _logger.Debug("🔠 CapsLock state: {Status}", isOn ? "ON" : "OFF");
            return isOn;
        }

        public bool IsNumLockOn()
        {
            var isOn = (GetKeyState((int)Keys.NumLock) & 0x0001) != 0;
            _logger.Debug("🔢 NumLock state: {Status}", isOn ? "ON" : "OFF");
            return isOn;
        }

        public bool IsScrollLockOn()
        {
            var isOn = (GetKeyState((int)Keys.Scroll) & 0x0001) != 0;
            _logger.Debug("📜 ScrollLock state: {Status}", isOn ? "ON" : "OFF");
            return isOn;
        }

        public void LogStatistics()
        {
            _logger.Information("📊 KeyboardAutomationService statistics - Keys: {KeyCount}, Characters: {CharCount}, Errors: {ErrorCount}",
                _keyPressCount, _textTypedCharacters, _errorCount);
        }
    }
}