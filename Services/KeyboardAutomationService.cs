using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

namespace AionDesktopAssistant.Services
{
    public class KeyboardAutomationService
    {
        // Windows API constants
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key

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
            try
            {
                if (string.IsNullOrEmpty(text))
                    return;

                foreach (char c in text)
                {
                    TypeCharacter(c);
                    Thread.Sleep(20); // Small delay between characters for stability
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to type text: {ex.Message}", ex);
            }
        }

        public void TypeCharacter(char character)
        {
            try
            {
                if (character == '\n' || character == '\r')
                {
                    PressKey(Keys.Enter);
                    return;
                }

                if (character == '\t')
                {
                    PressKey(Keys.Tab);
                    return;
                }

                // Handle uppercase letters
                if (char.IsUpper(character))
                {
                    var key = (Keys)Enum.Parse(typeof(Keys), character.ToString().ToUpper());
                    PressKey(key, true); // With shift
                    return;
                }

                // Handle lowercase letters
                if (char.IsLower(character))
                {
                    var key = (Keys)Enum.Parse(typeof(Keys), character.ToString().ToUpper());
                    PressKey(key, false); // Without shift
                    return;
                }

                // Handle special characters
                if (CharacterMap.ContainsKey(character))
                {
                    var (key, needsShift) = CharacterMap[character];
                    PressKey(key, needsShift);
                    return;
                }

                // Fallback for unknown characters
                throw new ArgumentException($"Unsupported character: '{character}'");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to type character '{character}': {ex.Message}", ex);
            }
        }

        public void PressKey(Keys key, bool withShift = false)
        {
            try
            {
                if (withShift)
                {
                    // Press shift
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(10);
                }

                // Press the key
                keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
                Thread.Sleep(50);
                keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);

                if (withShift)
                {
                    Thread.Sleep(10);
                    // Release shift
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                }

                Thread.Sleep(20);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to press key {key}: {ex.Message}", ex);
            }
        }

        public void PressKeyCombo(params Keys[] keys)
        {
            try
            {
                // Press all keys down
                foreach (var key in keys)
                {
                    keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(10);
                }

                Thread.Sleep(50);

                // Release all keys in reverse order
                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    keybd_event((byte)keys[i], 0, KEYEVENTF_KEYUP, 0);
                    Thread.Sleep(10);
                }

                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to press key combination: {ex.Message}", ex);
            }
        }

        // Common shortcuts
        public void Copy()
        {
            PressKeyCombo(Keys.Control, Keys.C);
        }

        public void Paste()
        {
            PressKeyCombo(Keys.Control, Keys.V);
        }

        public void Cut()
        {
            PressKeyCombo(Keys.Control, Keys.X);
        }

        public void SelectAll()
        {
            PressKeyCombo(Keys.Control, Keys.A);
        }

        public void Undo()
        {
            PressKeyCombo(Keys.Control, Keys.Z);
        }

        public void Redo()
        {
            PressKeyCombo(Keys.Control, Keys.Y);
        }

        public void Save()
        {
            PressKeyCombo(Keys.Control, Keys.S);
        }

        public void Find()
        {
            PressKeyCombo(Keys.Control, Keys.F);
        }

        public void NewTab()
        {
            PressKeyCombo(Keys.Control, Keys.T);
        }

        public void CloseTab()
        {
            PressKeyCombo(Keys.Control, Keys.W);
        }

        public void SwitchTab()
        {
            PressKeyCombo(Keys.Control, Keys.Tab);
        }

        public void AltTab()
        {
            PressKeyCombo(Keys.Alt, Keys.Tab);
        }

        public void WindowsKey()
        {
            PressKey(Keys.LWin);
        }

        public void TaskManager()
        {
            PressKeyCombo(Keys.Control, Keys.Shift, Keys.Escape);
        }

        public void Screenshot()
        {
            PressKeyCombo(Keys.LWin, Keys.PrintScreen);
        }

        public void SnippingTool()
        {
            PressKeyCombo(Keys.LWin, Keys.Shift, Keys.S);
        }

        // Navigation keys
        public void ArrowUp()
        {
            PressKey(Keys.Up);
        }

        public void ArrowDown()
        {
            PressKey(Keys.Down);
        }

        public void ArrowLeft()
        {
            PressKey(Keys.Left);
        }

        public void ArrowRight()
        {
            PressKey(Keys.Right);
        }

        public void Home()
        {
            PressKey(Keys.Home);
        }

        public void End()
        {
            PressKey(Keys.End);
        }

        public void PageUp()
        {
            PressKey(Keys.PageUp);
        }

        public void PageDown()
        {
            PressKey(Keys.PageDown);
        }

        public void Backspace()
        {
            PressKey(Keys.Back);
        }

        public void Delete()
        {
            PressKey(Keys.Delete);
        }

        public void Enter()
        {
            PressKey(Keys.Enter);
        }

        public void Tab()
        {
            PressKey(Keys.Tab);
        }

        public void Escape()
        {
            PressKey(Keys.Escape);
        }

        // Utility methods
        public bool IsKeyPressed(Keys key)
        {
            return (GetKeyState((int)key) & 0x8000) != 0;
        }

        public bool IsCapsLockOn()
        {
            return (GetKeyState((int)Keys.CapsLock) & 0x0001) != 0;
        }

        public bool IsNumLockOn()
        {
            return (GetKeyState((int)Keys.NumLock) & 0x0001) != 0;
        }

        public bool IsScrollLockOn()
        {
            return (GetKeyState((int)Keys.Scroll) & 0x0001) != 0;
        }
    }
}