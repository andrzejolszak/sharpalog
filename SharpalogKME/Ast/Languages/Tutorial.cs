
using Ast2;
using Sharplog.KME;
using System;
using System.Collections.Generic;

namespace ProjectionalBlazorMonaco
{
    public static class Tutorial
    {
        public static Action<Node, Ast2Editor>[] Builders = new Action<Node, Ast2Editor>[]
        {
            (n, e) => n.WithChildren(
                    new ReadOnlyTextNode("1. Read-only nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new ReadOnlyTextNode("This is a ReadOnlyTextNode, this text cannot be edited."),
                    ReadOnlyTextNode.NewLine()),
            (n, e) => n.WithChildren(
                    new ReadOnlyTextNode("2. Editable nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new EditableTextNode("This is an EditableTextNode, this text can be edited.")),
            (n, e) =>
            {
                EditableTextNode.TextHolder textHolder = new EditableTextNode.TextHolder() { Text = "Node" };
                n.WithChildren(
                    new ReadOnlyTextNode("3. Synchronized editable nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new EditableTextNode(textHolder),
                    new ReadOnlyTextNode(" is synchronized through a TextHolder with "),
                    new EditableTextNode(textHolder),
                    new ReadOnlyTextNode(" . Editing either will affect the other."));
            },
            (n, e) => {
                e.FactoryRegistry.Add((typeof(Banana), () => new Banana()));
                e.FactoryRegistry.Add((typeof(Strawberry), () => new Strawberry()));
                e.FactoryRegistry.Add((typeof(EditableTextNode), () => new EditableTextNode("Bus")));
                e.FactoryRegistry.Add((typeof(EditableTextNode), () => new EditableTextNode("Busstop")));

                n.WithChildren(
                    new ReadOnlyTextNode("4. Hole nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new HoleNode<ReadOnlyTextNode>(),
                    new ReadOnlyTextNode(" is a HoleNode that can be filled with registered ReadOnlyTextNode completion objects using ctrl+space"),
                    ReadOnlyTextNode.NewLine(),
                    new HoleNode<EditableTextNode>(),
                    new ReadOnlyTextNode(" <- this one can be filled with registered EditableTextNode completion objects using ctrl+space"));
            },

            (n, e) => {
                n.WithChildren(
                    new ReadOnlyTextNode("5. Reference nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new HoleNode<ReferenceNode<EditableTextNode>>(),
                    new ReadOnlyTextNode(" is a Hole for ReferenceNode that can refer to "),
                    new EditableTextNode("an existing EditableTextNode."));
            },

            (n, e) => {
                n.WithChildren(
                    new ReadOnlyTextNode("6. Enum nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new EnumNode(new HashSet<string>{ "foo", "bar", "car" }, "foo"),
                    new ReadOnlyTextNode(" is an EnumNode that only allows values from a predefined set"));
            },
            (n, e) => {
                n.WithChildren(
                    new ReadOnlyTextNode("7. Toggle nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new ToggleNode(),
                    new ReadOnlyTextNode(" is a ToggleNode that can be edited with mouse or spacebar"));
            },
            (n, e) => {
                n.WithChildren(
                    new ReadOnlyTextNode("8. List nodes:") { Style = VisualStyles.UnderlineStyle },
                    ReadOnlyTextNode.NewLine(),
                    new ListNode<ReadOnlyTextNode>("[", "]", ", ", new List<Node>{ new ReadOnlyTextNode("cat"), new ReadOnlyTextNode("dog") }),
                    new ReadOnlyTextNode(" is a ListNode that can add and remove elements (use Enter, Comma, Backspace)"));
            }
        };
    }

    public class Banana : ReadOnlyTextNode
    {
        public Banana() : base("Banana")
        {
        }
    }

    public class Strawberry : ReadOnlyTextNode
    {
        public Strawberry() : base("Strawberry")
        {
        }
    }
}
