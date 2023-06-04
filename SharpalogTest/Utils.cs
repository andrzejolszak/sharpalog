using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Sharplog.KME;
using Ast2;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace ProjectionalBlazorMonaco.Tests
{
    public static class Utils
    {
        public static void AssertTextContains(this Ast2Editor page, string text)
        {
            Dispatcher.UIThread.RunJobs();

            int caretPosition = text.IndexOf('\'');

            text = text.Replace("\'", "");

            string val = page.Editor.EditorControl.Text.Replace("·", " ").Replace("\n", "\r\n").Replace("\r\r", "\r");
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
        public static void Press(this Ast2Editor page, Key key, string modifiers = "")
        {
            KeyModifiers keyModifiers = KeyModifiers.None;
            if (modifiers.Contains("Control"))
            {
                keyModifiers |= KeyModifiers.Control;
            }

            if (modifiers.Contains("Alt"))
            {
                keyModifiers |= KeyModifiers.Alt;
            }

            if (modifiers.Contains("Shift"))
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
                Device = KeyboardDevice.Instance
            });
        }

        public static void PressArrowLeft(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Press(page, Key.Left, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
            }
        }

        public static void PressArrowRight(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Press(page, Key.Right, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
            }
        }

        public static void PressEnter(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.Enter,(ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void PressControl(this Ast2Editor page)
        {
            Press(page, Key.LeftCtrl);
        }

        public static void PressBackspace(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Press(page, Key.Back, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
            }
        }

        public static void PressTab(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.Tab, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void PressDelete(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.Delete, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void PressEscape(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.Escape, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void PressArrowDown(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.Down, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void PressArrowUp(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.Up, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void PressEnd(this Ast2Editor page, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Press(page, Key.End, (ctrl ? "Control+" : string.Empty) + (alt ? "Alt+" : string.Empty) + (shift ? "Shift+" : string.Empty));
        }

        public static void OpenCompletion(this Ast2Editor page)
        {
            Press(page, Key.Space, "Control");
            page.Editor.Completion.ShowCompletionMenu();
        }

        public static void SelectCompletion(this Ast2Editor page, string containsText)
        {
            page.Editor.Completion.ExternalCompletions.Single(x => x.Text.Contains(containsText))
                .Complete(
                    page.Editor.EditorControl.TextArea,
                    new AnchorSegment(page.Editor.EditorControl.Document, page.CurrentOffset, 1),
                    new EventArgs());

            page.Editor.Completion.CompletionWindow.Hide();
        }

        public static void Type(this Ast2Editor page, string text)
        {
            RaiseTextEvent(page, text);
        }

        private static void RaiseTextEvent(Ast2Editor textBox, string text)
        {
            textBox.Editor.EditorControl.TextArea.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = TextEditor.TextInputEvent,
                Text = text,
                Device = KeyboardDevice.Instance,
                Source = textBox.Editor.EditorControl.TextArea
            });
        }


        public static void ClickAsync(this Ast2Editor page, string v, bool ctrl)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
