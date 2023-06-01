using Sharplog.KME;

namespace Ast2
{
    public class ReadOnlyTextNode : Node
    {
        public static ReadOnlyTextNode Space() => new ReadOnlyTextNode(" ") { Unselectable = true };
        public static ReadOnlyTextNode FatSpace() => new ReadOnlyTextNode("˽") { Unselectable = true };
        public static ReadOnlyTextNode NewLine() => new ReadOnlyTextNode("\r\n") { Unselectable = true };
        public static ReadOnlyTextNode Tab() => new ReadOnlyTextNode("    ") { Unselectable = true };

        public ReadOnlyTextNode(string text)
        {
            this.Text = text;
            this.Style = VisualStyles.NormalTextStyle;
        }

        public string Text { get; protected set; }

        public VisualStyle Style { get; set; }

        public override void CreateView(EditorState editorState)
        {
            this.View = new NodeView(this, this.Text, this.Style);
        }
    }
}
