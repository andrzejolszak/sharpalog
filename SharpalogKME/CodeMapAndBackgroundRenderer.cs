using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Sharplog.KME;

public class CodeMapAndBackgroundRenderer : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public CodeMapAndBackgroundRenderer(TextEditor textEditor)
    {
        _markerBrush = Brushes.LightSteelBlue;
        this.TextEditor = textEditor;

        this.TextEditor.TextArea.SelectionChanged += TextAreaSelectionChanged;
        this.TextEditor.TemplateApplied += TextEditorTemplateApplied;

        this.TextEditor.TextArea.TextView.BackgroundRenderers.Add(this);
    }

    private IBrush _markerBrush;

    public ScrollBar VerticalScroll { get; set; }

    public Button ScrollLineUpButton { get; set; }

    public List<TextSegment> Matches { get; set; } = new List<TextSegment>();
    public TextEditor TextEditor { get; internal set; }
    public SyntaxHighlightTransformer SyntaxHighlighter { get; internal set; }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView == null)
            throw new ArgumentNullException(nameof(textView));
        if (drawingContext == null)
            throw new ArgumentNullException(nameof(drawingContext));

        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0)
            return;

        DocumentLine firstViewLine = visualLines.First().FirstDocumentLine;
        DocumentLine lastViewLine = visualLines.Last().LastDocumentLine;

        var viewStartOffset = firstViewLine.Offset;
        var viewEndOffset = lastViewLine.EndOffset;

        // Code map
        if (VerticalScroll is not null && ScrollLineUpButton is not null)
        {
            double mapHeight = VerticalScroll.Bounds.Height - ScrollLineUpButton.Height * 2;
            double mapX = textView.Bounds.Right - 10 - VerticalScroll.Width;

            // Caret:
            double caretInRange = mapHeight * (TextEditor.TextArea.Caret.Line - 1) / TextEditor.LineCount;
            DrawRectangleGeometry(textView, drawingContext, new Rect(mapX, caretInRange + ScrollLineUpButton.Height, 10, 1), Brushes.Black);

            // Syntax error
            if (this.SyntaxHighlighter is not null && this.SyntaxHighlighter.SyntaxErrorLine is not null)
            {
                double errorInRange = mapHeight * (this.SyntaxHighlighter.SyntaxErrorLine.Value - 1) / TextEditor.LineCount;
                DrawRectangleGeometry(textView, drawingContext, new Rect(mapX + 6, errorInRange + ScrollLineUpButton.Height, 4, 4), Brushes.Red);
            }

            // Selection matches
            foreach (var match in Matches)
            {
                double matchInRange = mapHeight * (this.TextEditor.Document.GetLineByOffset(match.StartOffset).LineNumber - 1) / TextEditor.LineCount;
                DrawRectangleGeometry(textView, drawingContext, new Rect(mapX + 2, matchInRange + ScrollLineUpButton.Height, 6, 3), Brushes.SlateGray);
            }
        }

        // Selection match marks code background
        foreach (var result in Matches.Where(x => x.EndOffset >= viewStartOffset || x.StartOffset <= viewEndOffset))
        {
            var geoBuilder = new BackgroundGeometryBuilder
            {
                AlignToWholePixels = true,
                CornerRadius = 3
            };

            geoBuilder.AddSegment(textView, result);
            var geometry = geoBuilder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(_markerBrush, null, geometry);
            }
        }
    }

    private void DrawRectangleGeometry(TextView textView, DrawingContext drawingContext, Rect rectangle, IBrush brush)
    {
        var geoBuilder = new BackgroundGeometryBuilder
        {
            AlignToWholePixels = true
        };

        geoBuilder.AddRectangle(textView, rectangle);
        var geometry = geoBuilder.CreateGeometry();
        if (geometry != null)
        {
            drawingContext.DrawGeometry(brush, null, geometry);
        }
    }

    private void TextAreaSelectionChanged(object? sender, EventArgs e)
    {
        this.Matches.Clear();
        string selectedText = this.TextEditor.TextArea.Selection.GetText();
        if (selectedText is not null && selectedText.Length > 0)
        {
            int startIndex = 0;
            while (true)
            {
                startIndex = this.TextEditor.Text.IndexOf(selectedText, startIndex);
                if (startIndex == -1)
                {
                    break;
                }

                if (!this.TextEditor.TextArea.Selection.Contains(startIndex))
                {
                    this.Matches.Add(new TextSegment() { StartOffset = startIndex, Length = selectedText.Length });
                }

                startIndex += selectedText.Length;
            }
        }
    }

    private void TextEditorTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (VerticalScroll is null)
        {
            var scrollView = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
            scrollView.TemplateApplied += (s, ee) =>
            {
                if (VerticalScroll is null)
                {
                    ee.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar").AllowAutoHide = false;
                    VerticalScroll = ee.NameScope.Find<ScrollBar>("PART_VerticalScrollBar");
                    VerticalScroll.AllowAutoHide = false;
                    VerticalScroll.Width = 20;
                    VerticalScroll.TemplateApplied += (_, eee) =>
                    {
                        if (this.ScrollLineUpButton is null)
                        {
                            this.ScrollLineUpButton = eee.NameScope.Find<Button>("PART_LineUpButton");
                        }
                    };
                }
            };
        }
    }
}