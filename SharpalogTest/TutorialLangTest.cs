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
        public void ReadonlyNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(1);
                editor.Editor.EditorControl.Focus();

                editor.AssertTextContains("'1. Read-only nodes:");

                editor.PressArrowDown();
                editor.AssertTextContains("'This is a");

                editor.Type("x");
                editor.AssertTextContains("'This is a");

                editor.Type("x");
                editor.Type(" ");
                editor.PressBackspace();
                editor.AssertTextContains("'This is a");

                editor.PressEnter();
                editor.AssertTextContains("'This is a ReadOnlyTextNode, this text cannot be edited.'");
            }
        }

        [Test]
        public void EditableTextNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(2);

                editor.AssertTextContains("'2. Editable nodes:");

                editor.PressArrowDown();
                editor.AssertTextContains("'This is an EditableTextNode");

                editor.Type("x");
                editor.AssertTextContains("x'This is a");

                editor.PressArrowRight(shift: true, count: 4);
                editor.Type("y");
                editor.AssertTextContains("xy' is a");

                editor.PressArrowRight(count: 3);
                editor.PressArrowLeft(shift: true, count: 3);
                editor.Type("z");
                editor.AssertTextContains("xyz' a");

                editor.PressEnd();
                editor.PressArrowLeft();
                editor.AssertTextContains("can be edited.'◦");

                editor.Type("p");
                editor.AssertTextContains("can be edited.p'◦");

                editor.PressBackspace(count: 2);
                editor.AssertTextContains("can be edited'◦");
            }
        }

        [Test]
        public void SynchronizedEditableTextNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(3);

                editor.AssertTextContains("'3. Synchronized");

                editor.PressArrowDown();
                editor.AssertTextContains("'Node◦ is");
                editor.AssertTextContains("with Node◦");

                editor.Type("x");
                editor.AssertTextContains("x'Node◦ is");
                editor.AssertTextContains("with xNode◦");
            }
        }

        [Test]
        public void HoleNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(4);

                editor.AssertTextContains("'4. Hole nodes");

                editor.PressArrowDown();
                editor.AssertTextContains("'◊ is a HoleNode");

                editor.OpenCompletion();
                editor.SelectCompletion("Banana");
                editor.AssertTextContains("Banana' is a HoleNode");

                editor.PressArrowLeft();
                editor.AssertTextContains("Banan'a is a HoleNode");
                editor.OpenCompletion();
                editor.SelectCompletion("Strawberry");
                editor.AssertTextContains("Strawberry' is a HoleNode");

                editor.PressArrowLeft();
                editor.Type("x");
                editor.AssertTextContains("Strawberr'y is a HoleNode");

                editor.PressBackspace();
                editor.AssertTextContains("'◊ is a HoleNode");

                editor.Type("xxx");
                editor.AssertTextContains("xxx'◦ is a HoleNode");
                editor.PressEscape();
                editor.AssertTextContains("'◊ is a HoleNode");

                editor.Type("xxx");
                editor.AssertTextContains("xxx'◦ is a HoleNode");
                editor.PressArrowLeft(count: 3);
                editor.AssertTextContains("'xxx◦ is a HoleNode");
                editor.PressArrowUp();
                editor.AssertTextContains("◊ is a HoleNode");

                editor.PressArrowDown();
                editor.AssertTextContains("'◊ is a HoleNode");
                editor.Type("xxx");
                editor.AssertTextContains("xxx'◦ is a HoleNode");
                editor.OpenCompletion();
                editor.SelectCompletion("Banana");
                editor.AssertTextContains("Banana' is a HoleNode");
            }
        }

        [Test]
        public void ReferenceNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(5);

                editor.AssertTextContains("'5. Reference nodes");

                editor.PressArrowDown();
                editor.AssertTextContains("'◊ is a Hole for ReferenceNode");

                editor.OpenCompletion();
                editor.SelectCompletion("an existing EditableTextNode");
                editor.AssertTextContains("an existing EditableTextNode.◦' is a Hole for ReferenceNode");

                editor.Type("x");
                editor.AssertTextContains("an existing EditableTextNode.◦ is a Hole for ReferenceNode");

                editor.PressEnd();
                editor.PressArrowLeft();
                editor.AssertTextContains("to an existing EditableTextNode.'◦");

                editor.Type("y");
                editor.AssertTextContains("to an existing EditableTextNode.y'◦");

                editor.AssertTextContains("an existing EditableTextNode.y◦ is a Hole for ReferenceNode");

                editor.ClickAsync("text=an", true);
                editor.AssertTextContains("that can refer to 'an existing EditableTextNode");
            }
        }

        [Test]
        public void EnumNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(6);

                editor.AssertTextContains("'6. Enum nodes");

                editor.PressArrowDown();
                editor.AssertTextContains("'foo is an EnumNode");

                editor.OpenCompletion();
                editor.SelectCompletion("bar");
                editor.AssertTextContains("'bar is an EnumNode");

                editor.Type("x");
                editor.PressBackspace();
                editor.PressEscape();
                editor.AssertTextContains("b'ar is an EnumNode");

                editor.OpenCompletion();
                editor.PressArrowRight(count: 2);
                editor.AssertTextContains("b'ar is an EnumNode");

                editor.OpenCompletion();
                editor.SelectCompletion("foo");
                editor.AssertTextContains("f'oo is an EnumNode");
            }
        }

        [Test]
        public void ToggleNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(7);

                editor.AssertTextContains("'7. Toggle nodes");

                editor.PressArrowDown();
                editor.AssertTextContains("'[ ] is a ToggleNode");

                editor.Type(" ");
                editor.AssertTextContains("['*] is a ToggleNode");

                editor.Type(" ");
                editor.AssertTextContains("[' ] is a ToggleNode");

                editor.Type("x");
                editor.AssertTextContains("[' ] is a ToggleNode");
            }
        }

        [Test]
        [Ignore("inifinite loop via dispatcher jobs")]
        public void ListNode()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Ast2Editor editor = new Ast2Editor(new CodeEditor());
                editor.InitAndLoadExample(8);

                editor.AssertTextContains("'8. List nodes");

                editor.PressArrowDown();
                editor.AssertTextContains("'[cat, dog] is a ListNode");

                editor.PressBackspace();
                editor.AssertTextContains("'[cat, dog] is a ListNode");

                editor.Type(",");
                editor.AssertTextContains("['◊, cat, dog] is a ListNode");

                editor.PressEscape();
                editor.AssertTextContains("['cat, dog] is a ListNode");

                editor.PressArrowRight(count: 3);
                editor.PressBackspace();
                editor.AssertTextContains("['dog] is a ListNode");

                editor.PressArrowRight(count: 3);
                editor.PressBackspace();
                editor.AssertTextContains("['] is a ListNode");
            }
        }
    }
}
