namespace Ast2
{
    public class NodeView
    {
        public NodeView(Node node, string text, TextDecoration style, TextDecoration backgroundStyle = null, TextDecoration overlayStyle = null)
        {
            this.Node = node;
            this.Text = text;
            this.Style = style;
            this.BackgroundStyle = backgroundStyle;
            this.OverlayStyle = overlayStyle;
        }

        public string Text { get; }

        public TextDecoration Style { get; }

        public TextDecoration BackgroundStyle { get; }

        public TextDecoration OverlayStyle { get; }
        
        public Node Node { get; }
    }
}
