using Sharplog.KME;

namespace Ast2
{
    public class NodeView
    {
        public NodeView(Node node, string text, VisualStyle style, VisualStyle backgroundStyle = null, VisualStyle overlayStyle = null)
        {
            this.Node = node;
            this.Text = text;
            this.Style = style;
            this.BackgroundStyle = backgroundStyle;
            this.OverlayStyle = overlayStyle;
        }

        public string Text { get; }

        public VisualStyle Style { get; }

        public VisualStyle BackgroundStyle { get; }

        public VisualStyle OverlayStyle { get; }
        
        public Node Node { get; }
    }
}
