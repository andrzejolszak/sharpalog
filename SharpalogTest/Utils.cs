using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Sharplog.KME;
using Ast2;

namespace ProjectionalBlazorMonaco.Tests
{
    public static class Utils
    {
        public static async Task AssertTextContains(this Ast2Editor page, string text)
        {
            int caretPosition = text.IndexOf('\'');

            text = text.Replace("\'", "");

            string val = page._monacoEditor.EditorControl.Text.Replace("·", " ").Replace("\n", "\r\n");
            int idx = val.IndexOf(text);
            idx.Should().BeGreaterOrEqualTo(0, val);

            if (caretPosition >= 0)
            {
                int offset = page._monacoEditor.EditorControl.CaretOffset;
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
        /// <param name="keys"></param>
        /// <returns></returns>
        public static async Task Press(this Ast2Editor page, string keys)
        {
            // TODO:
            page._monacoEditor.EditorControl.RaiseEvent(null);
        }

        public static async Task PressArrowLeft(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "ArrowLeft");
            }
        }

        public static async Task PressArrowRight(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "ArrowRight");
            }
        }

        public static async Task PressEnter(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "Enter");
        }

        public static async Task PressControl(this Ast2Editor page)
        {
            await Press(page, "Control");
        }

        public static async Task PressBackspace(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "Backspace");
            }
        }

        public static async Task PressTab(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "Tab");
        }

        public static async Task PressDelete(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "Delete");
        }

        public static async Task PressEscape(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "Escape");
        }

        public static async Task PressArrowDown(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "ArrowDown");
        }

        public static async Task PressArrowUp(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "ArrowUp");
        }

        public static async Task PressEnd(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Press(page, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty) + "End");
        }

        public static async Task PressCtrlSpace(this Ast2Editor page)
        {
            await Press(page, "Control+Space");
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
