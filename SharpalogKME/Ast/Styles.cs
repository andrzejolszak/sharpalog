using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using Sharplog.KME;
using System.Threading.Tasks;

namespace Ast2
{
    public static class Styles
    {
        // TODO
        public static readonly VisualStyle NormalTextStyle = new VisualStyle();

        public static readonly VisualStyle NormalTextGrayBackgroundStyle = new VisualStyle().SetBackgroundBrush(Brushes.LightGray);

        public static readonly VisualStyle NormalTextRedBackgroundStyle = new VisualStyle().SetBackgroundBrush(Brushes.LightSalmon);

        public static readonly VisualStyle NormalTextBlueBackgroundStyle = new VisualStyle().SetBackgroundBrush(Brushes.LightSteelBlue);

        public static readonly VisualStyle GrayTextStyle = new VisualStyle().SetForegroundBrush(Brushes.DarkGray);

        public static readonly VisualStyle Scratch;
        public static readonly VisualStyle UnderlineStyle = new VisualStyle().SetTextDecorations(CustomTextDecorations.Underline);

        public static readonly VisualStyle SelectedParentNodeText = new VisualStyle().SetTextDecorations(CustomTextDecorations.Underline);

        public static readonly VisualStyle SelectedNodeText = new VisualStyle().SetTextDecorations(CustomTextDecorations.Underline);

        public static readonly VisualStyle InvisibleCharsStyle;
    }

    public class VisualStyle : TextRunProperties
    {
        private IBrush _backgroundBrush;
        private BaselineAlignment _baselineAlignment;
        private CultureInfo _cultureInfo;
        private double _fontRenderingEmSize;
        private IBrush _foregroundBrush;
        private Typeface _typeface;
        private TextDecorationCollection _textDecorations = new TextDecorationCollection();

        public override IBrush BackgroundBrush => _backgroundBrush;

        public IBrush CodeMapBrush { get; set; }

        public VisualStyle SetBackgroundBrush(IBrush value)
        {
            _backgroundBrush = value?.ToImmutable();
            return this;
        }

        public override BaselineAlignment BaselineAlignment => _baselineAlignment;

        public VisualStyle SetBaselineAlignment(BaselineAlignment value)
        {
            _baselineAlignment = value;
            return this;
        }

        public override CultureInfo CultureInfo => _cultureInfo;

        public VisualStyle SetCultureInfo(CultureInfo value)
        {
            _cultureInfo = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        public override double FontRenderingEmSize => _fontRenderingEmSize;

        public VisualStyle SetFontRenderingEmSize(double value)
        {
            _fontRenderingEmSize = value;
            return this;
        }

        public override IBrush ForegroundBrush => _foregroundBrush;

        public VisualStyle SetForegroundBrush(IBrush value)
        {
            _foregroundBrush = value?.ToImmutable();
            return this;
        }

        public override Typeface Typeface => _typeface;

        public VisualStyle SetTypeface(Typeface value)
        {
            _typeface = value;
            return this;
        }

        public override TextDecorationCollection TextDecorations => _textDecorations;

        public VisualStyle SetTextDecorations(TextDecorationCollection value)
        {
            ExtensionMethods.CheckIsFrozen(value);
            if (_textDecorations == null)
                _textDecorations = value;
            else
                _textDecorations = new TextDecorationCollection(_textDecorations.Union(value));

            return this;
        }
    }
}
