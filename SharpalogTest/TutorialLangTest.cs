using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Sharplog.KME;
using Ast2;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.AvaloniaMocks;
using AvaloniaEdit.Rendering;
using Moq;

namespace ProjectionalBlazorMonaco.Tests
{
    public class TutorialLangTest
    {

        [Test]
        public void Visual_Line_Should_Create_Two_Text_Lines_When_Wrapping()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            TextView textView = new TextView();

            TextDocument document = new TextDocument("hello world".ToCharArray());

            textView.Document = document;

            ((ILogicalScrollable)textView).CanHorizontallyScroll = false;
            textView.Width = MockGlyphTypeface.GlyphAdvance * 8;

            textView.Measure(Size.Infinity);

            VisualLine visualLine = textView.GetOrConstructVisualLine(document.Lines[0]);

            Assert.AreEqual(2, visualLine.TextLines.Count);
            Assert.AreEqual("hello ", new string(visualLine.TextLines[0].TextRuns[0].Text.Span));
            Assert.AreEqual("world", new string(visualLine.TextLines[1].TextRuns[0].Text.Span));
        }

        [Test]
        public void ClearCaretAndSelectionOnDocumentChange()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                TextArea textArea = new TextArea();
                textArea.Document = new TextDocument("1\n2\n3\n4th line");
                textArea.Caret.Offset = 6;
                textArea.Selection = Selection.Create(textArea, 3, 6);
                textArea.Document = new TextDocument("1\n2nd");
                Assert.AreEqual(0, textArea.Caret.Offset);
                Assert.AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
                Assert.IsTrue(textArea.Selection.IsEmpty);
            }
        }

        [Test]
        public void SetDocumentToNull()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                TextArea textArea = new TextArea();
                textArea.Document = new TextDocument("1\n2\n3\n4th line");
                textArea.Caret.Offset = 6;
                textArea.Selection = Selection.Create(textArea, 3, 6);
                textArea.Document = null;
                Assert.AreEqual(0, textArea.Caret.Offset);
                Assert.AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
                Assert.IsTrue(textArea.Selection.IsEmpty);
            }
        }

        [Test]
        public async Task ReadonlyNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(1);

                await editor.AssertTextContains("'1. Read-only nodes:");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'This is a");

                await editor.Press(Key.X);
                await editor.AssertTextContains("'This is a");

                await editor.Press(Key.X);
                await editor.Press(Key.Space);
                await editor.PressBackspace();
                await editor.AssertTextContains("'This is a");

                await editor.PressEnter();
                await editor.AssertTextContains("'This is a ReadOnlyTextNode, this text cannot be edited.'");
            }
        }

        [Test]
        public async Task ReadonlyNode2()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(1);

                await editor.AssertTextContains("'1. Read-only nodes:");

                await editor.PressArrowRight(ctrl: true);
                await editor.AssertTextContains("1. Read-only nodes:'");

                await editor.PressArrowRight(ctrl: true);
                await editor.PressArrowRight(ctrl: true);
                await editor.AssertTextContains("cannot be edited'");

                await editor.PressArrowLeft(ctrl: true);
                await editor.AssertTextContains("'This is a");

                await editor.Press(Key.X);
                await editor.AssertTextContains("'This is a");

                await editor.Press(Key.X);
                await editor.Press(Key.Space);
                await editor.PressBackspace();
                await editor.AssertTextContains("'This is a");

                await editor.PressEnter();
                await editor.AssertTextContains("'This is a ReadOnlyTextNode, this text cannot be edited.'");
            }
        }

        [Test]
        public async Task EditableTextNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(2);

                await editor.AssertTextContains("'2. Editable nodes:");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'This is an EditableTextNode");

                await editor.Press(Key.X);
                await editor.AssertTextContains("x'This is a");

                await editor.PressArrowRight(shift: true, count: 4);
                await editor.Press(Key.Y);
                await editor.AssertTextContains("xy' is a");

                await editor.PressArrowRight(count: 3);
                await editor.PressArrowLeft(shift: true, count: 3);
                await editor.Press(Key.Z);
                await editor.AssertTextContains("xyz' a");

                await editor.PressEnd();
                await editor.PressArrowLeft();
                await editor.AssertTextContains("can be edited.'◦");

                await editor.Press(Key.P);
                await editor.AssertTextContains("can be edited.p'◦");

                await editor.PressBackspace(count: 2);
                await editor.AssertTextContains("can be edited'◦");
            }
        }

        [Test]
        public async Task SynchronizedEditableTextNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(3);

                await editor.AssertTextContains("'3. Synchronized");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'Node◦ is");
                await editor.AssertTextContains("with Node◦");

                await editor.Press(Key.X);
                await editor.AssertTextContains("x'Node◦ is");
                await editor.AssertTextContains("with xNode◦");
            }
        }

        [Test]
        public async Task HoleNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(4);

                await editor.AssertTextContains("'4. Hole nodes");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'◊ is a HoleNode");

                await editor.PressCtrlSpace();
                await editor.PressEnter();
                await editor.AssertTextContains("'Banana is a HoleNode");

                await editor.PressCtrlSpace();
                await editor.PressArrowDown();
                await editor.PressEnter();
                await editor.AssertTextContains("'Strawberry is a HoleNode");

                await editor.PressArrowRight();
                await editor.PressBackspace();
                await editor.AssertTextContains("'◊ is a HoleNode");

                await editor.Type("xxx");
                await editor.AssertTextContains("xxx'◦ is a HoleNode");
                await editor.PressEscape();
                await editor.AssertTextContains("'◊ is a HoleNode");

                await editor.Type("xxx");
                await editor.PressCtrlSpace();
                await editor.PressEnter();
                await editor.AssertTextContains("xxx'◦ is a HoleNode");

                await editor.PressArrowUp();
                await editor.AssertTextContains("◊ is a HoleNode");
            }
        }

        [Test]
        public async Task ReferenceNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(5);

                await editor.AssertTextContains("'5. Reference nodes");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'◊ is a Hole for ReferenceNode");

                await editor.PressCtrlSpace();
                await editor.PressEnter();
                await editor.AssertTextContains("'an existing EditableTextNode.◦ is a Hole for ReferenceNode");

                await editor.Press(Key.X);
                await editor.AssertTextContains("'an existing EditableTextNode.◦ is a Hole for ReferenceNode");

                await editor.PressEnd();
                await editor.PressArrowLeft();
                await editor.AssertTextContains("to an existing EditableTextNode.'◦");

                await editor.Press(Key.Y);
                await editor.AssertTextContains("to an existing EditableTextNode.y'◦");

                await editor.AssertTextContains("an existing EditableTextNode.y◦ is a Hole for ReferenceNode");

                await editor.ClickAsync("text=an", true);
                await editor.AssertTextContains("that can refer to 'an existing EditableTextNode");
            }
        }

        [Test]
        public async Task EnumNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(6);

                await editor.AssertTextContains("'6. Enum nodes");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'foo is an EnumNode");

                await editor.PressCtrlSpace();
                await editor.PressEnter();
                await editor.AssertTextContains("'bar is an EnumNode");

                await editor.Press(Key.X);
                await editor.PressBackspace();
                await editor.PressEscape();
                await editor.AssertTextContains("'bar is an EnumNode");

                await editor.PressCtrlSpace();
                await editor.PressArrowRight(count: 2);
                await editor.AssertTextContains("ba'r is an EnumNode");

                await editor.PressCtrlSpace();
                await editor.PressArrowDown();
                await editor.PressArrowDown();
                await editor.PressEnter();
                await editor.AssertTextContains("fo'o is an EnumNode");
            }
        }

        [Test]
        public async Task ToggleNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(7);

                await editor.AssertTextContains("'7. Toggle nodes");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'[ ] is a ToggleNode");

                await editor.Press(Key.Space);
                await editor.AssertTextContains("['*] is a ToggleNode");

                await editor.Press(Key.Space);
                await editor.AssertTextContains("[' ] is a ToggleNode");

                await editor.Press(Key.X);
                await editor.AssertTextContains("[' ] is a ToggleNode");
            }
        }

        [Test]
        public async Task ListNode()
        {
            using (UnitTestApplication.Start(new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                platform: new MockRuntimePlatform(),
                platformHotkeyConfiguration: new MockPlatformHotkeyConfiguration(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl())))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(8);

                await editor.AssertTextContains("'8. List nodes");

                await editor.PressArrowDown();
                await editor.AssertTextContains("'[cat, dog] is a ListNode");

                await editor.PressBackspace();
                await editor.AssertTextContains("'[cat, dog] is a ListNode");

                await editor.Press(Key.OemComma);
                await editor.AssertTextContains("['◊, cat, dog] is a ListNode");

                await editor.PressEscape();
                await editor.AssertTextContains("['cat, dog] is a ListNode");

                await editor.PressArrowRight(count: 3);
                await editor.PressBackspace();
                await editor.AssertTextContains("['dog] is a ListNode");

                await editor.PressArrowRight(count: 3);
                await editor.PressBackspace();
                await editor.AssertTextContains("['] is a ListNode");
            }
        }
    }
}
