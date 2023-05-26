using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Sharplog.KME;
using Ast2;

namespace ProjectionalBlazorMonaco.Tests
{
    public class TutorialLangTest
    {
        [Test]
        public async Task ReadonlyNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'1. Read-only nodes:");

            await page.PressArrowDown();
            await page.AssertTextContains("'This is a");

            await page.Press("x");
            await page.AssertTextContains("'This is a");

            await page.Press("x");
            await page.Press(" ");
            await page.PressBackspace();
            await page.AssertTextContains("'This is a");

            await page.PressEnter();
            await page.AssertTextContains("'This is a ReadOnlyTextNode, this text cannot be edited.'");
        }

        [Test]
        public async Task EditableTextNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'2. Editable nodes:");

            await page.PressArrowDown();
            await page.AssertTextContains("'This is an EditableTextNode");

            await page.Press("x");
            await page.AssertTextContains("x'This is a");

            await page.PressArrowRight(shift: true, count: 4);
            await page.Press("y");
            await page.AssertTextContains("xy' is a");

            await page.PressArrowRight(count: 3);
            await page.PressArrowLeft(shift: true, count: 3);
            await page.Press("z");
            await page.AssertTextContains("xyz' a");

            await page.PressEnd();
            await page.PressArrowLeft();
            await page.AssertTextContains("can be edited.'◦");

            await page.Press("p");
            await page.AssertTextContains("can be edited.p'◦");

            await page.PressBackspace(count: 2);
            await page.AssertTextContains("can be edited'◦");
        }

        [Test]
        public async Task SynchronizedEditableTextNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'3. Synchronized");

            await page.PressArrowDown();
            await page.AssertTextContains("'Node◦ is");
            await page.AssertTextContains("with Node◦");

            await page.Press("x");
            await page.AssertTextContains("x'Node◦ is");
            await page.AssertTextContains("with xNode◦");
        }

        [Test]
        public async Task HoleNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'4. Hole nodes");

            await page.PressArrowDown();
            await page.AssertTextContains("'◊ is a HoleNode");

            await page.PressCtrlSpace();
            await page.PressEnter();
            await page.AssertTextContains("'Banana is a HoleNode");

            await page.PressCtrlSpace();
            await page.PressArrowDown();
            await page.PressEnter();
            await page.AssertTextContains("'Strawberry is a HoleNode");

            await page.PressArrowRight();
            await page.PressBackspace();
            await page.AssertTextContains("'◊ is a HoleNode");

            await page.Type("xxx");
            await page.AssertTextContains("xxx'◦ is a HoleNode");
            await page.PressEscape();
            await page.AssertTextContains("'◊ is a HoleNode");

            await page.Type("xxx");
            await page.PressCtrlSpace();
            await page.PressEnter();
            await page.AssertTextContains("xxx'◦ is a HoleNode");

            await page.PressArrowUp();
            await page.AssertTextContains("◊ is a HoleNode");
        }

        [Test]
        public async Task ReferenceNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'5. Reference nodes");

            await page.PressArrowDown();
            await page.AssertTextContains("'◊ is a Hole for ReferenceNode");

            await page.PressCtrlSpace();
            await page.PressEnter();
            await page.AssertTextContains("'an existing EditableTextNode.◦ is a Hole for ReferenceNode");

            await page.Press("x");
            await page.AssertTextContains("'an existing EditableTextNode.◦ is a Hole for ReferenceNode");

            await page.PressEnd();
            await page.PressArrowLeft();
            await page.AssertTextContains("to an existing EditableTextNode.'◦");

            await page.Press("y");
            await page.AssertTextContains("to an existing EditableTextNode.y'◦");

            await page.AssertTextContains("an existing EditableTextNode.y◦ is a Hole for ReferenceNode");

            await page.ClickAsync("text=an", true);
            await page.AssertTextContains("that can refer to 'an existing EditableTextNode");
        }

        [Test]
        public async Task EnumNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'6. Enum nodes");

            await page.PressArrowDown();
            await page.AssertTextContains("'foo is an EnumNode");

            await page.PressCtrlSpace();
            await page.PressEnter();
            await page.AssertTextContains("'bar is an EnumNode");

            await page.Press("x");
            await page.PressBackspace();
            await page.PressEscape();
            await page.AssertTextContains("'bar is an EnumNode");

            await page.PressCtrlSpace();
            await page.PressArrowRight(count: 2);
            await page.AssertTextContains("ba'r is an EnumNode");

            await page.PressCtrlSpace();
            await page.PressArrowDown();
            await page.PressArrowDown();
            await page.PressEnter();
            await page.AssertTextContains("fo'o is an EnumNode");
        }

        [Test]
        public async Task ToggleNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'7. Toggle nodes");

            await page.PressArrowDown();
            await page.AssertTextContains("'[ ] is a ToggleNode");

            await page.Press(" ");
            await page.AssertTextContains("['*] is a ToggleNode");

            await page.Press(" ");
            await page.AssertTextContains("[' ] is a ToggleNode");

            await page.Press("x");
            await page.AssertTextContains("[' ] is a ToggleNode");
        }

        [Test]
        public async Task ListNode()
        {
            Ast2Editor page = new Ast2Editor(new CodeEditor());

            await page.AssertTextContains("'8. List nodes");

            await page.PressArrowDown();
            await page.AssertTextContains("'[cat, dog] is a ListNode");

            await page.PressBackspace();
            await page.AssertTextContains("'[cat, dog] is a ListNode");

            await page.Press(",");
            await page.AssertTextContains("['◊, cat, dog] is a ListNode");

            await page.PressEscape();
            await page.AssertTextContains("['cat, dog] is a ListNode");

            await page.PressArrowRight(count: 3);
            await page.PressBackspace();
            await page.AssertTextContains("['dog] is a ListNode");

            await page.PressArrowRight(count: 3);
            await page.PressBackspace();
            await page.AssertTextContains("['] is a ListNode");
        }
    }
}
