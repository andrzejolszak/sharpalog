using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Sharplog.KME;
using Ast2;
using AvaloniaEdit;

namespace ProjectionalBlazorMonaco.Tests
{
    public static class Utils
    {
        public static async Task AssertTextContains(this Ast2Editor page, string text)
        {
            int caretPosition = text.IndexOf('\'');

            text = text.Replace("\'", "");

            string val = page.Editor.EditorControl.Text.Replace("·", " ").Replace("\n", "\r\n");
            int idx = val.IndexOf(text);
            idx.Should().BeGreaterOrEqualTo(0, val);

            if (caretPosition >= 0)
            {
                int offset = page.Editor.EditorControl.CaretOffset;
                offset.Should().Be(caretPosition + idx, val.Insert(offset, "'"));
            }
        }

        /// <summary>
        /// E.g. Control+ArrowRight
        /// Backquote, Enter, Control, Minus, Equal, Backslash, Backspace, Tab, Delete, Escape,
        /// ArrowDown, End, Enter, Home, Insert, PageDown, PageUp, ArrowRight,
        /// ArrowUp, F1 - F12, Digit0 - Digit9, KeyA - KeyZ, etc.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        public static async Task Press(this Ast2Editor page, Key key, string modifiers = "")
        {
            KeyModifiers keyModifiers = KeyModifiers.None;
            if (modifiers.Contains("Control+"))
            {
                keyModifiers |= KeyModifiers.Control;
            }

            if (modifiers.Contains("Alt+"))
            {
                keyModifiers |= KeyModifiers.Alt;
            }

            if (modifiers.Contains("Shift+"))
            {
                keyModifiers |= KeyModifiers.Shift;
            }

            RaiseKeyEvent(page, key, keyModifiers);
        }

        private static void RaiseKeyEvent(Ast2Editor textBox, Key key, KeyModifiers inputModifiers)
        {
            textBox.Editor.EditorControl.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = TextEditor.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key,
                Source = textBox.Editor.EditorControl,
                Device = KeyboardDevice.Instance,
                Route = RoutingStrategies.Direct
            });
        }

        private static void RaiseTextEvent(Ast2Editor textBox, string text)
        {
            textBox.Editor.EditorControl.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = TextEditor.TextInputEvent,
                Text = text
            });
        }

        public static async Task PressArrowLeft(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await Press(page, Key.Left, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
            }
        }

        public static async Task PressArrowRight(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await Press(page, Key.Right, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
            }
        }

        public static async Task PressEnter(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.Enter,(ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressControl(this Ast2Editor page)
        {
            await Press(page, Key.LeftCtrl);
        }

        public static async Task PressBackspace(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await Press(page, Key.Back, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
            }
        }

        public static async Task PressTab(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.Tab, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressDelete(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.Delete, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressEscape(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.Escape, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressArrowDown(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.Down, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressArrowUp(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.Up, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressEnd(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, Key.End, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static async Task PressCtrlSpace(this Ast2Editor page)
        {
            await Press(page, Key.Space, "Control");
        }

        public static async Task Type(this Ast2Editor page, string text)
        {
            // TODO: send the keys, or just insert?
        }


        public static async Task ClickAsync(this Ast2Editor page, string v, bool ctrl)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
