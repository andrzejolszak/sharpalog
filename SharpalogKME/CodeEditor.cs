﻿using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using RoslynPad.Editor;
using Stringes;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Sharplog.KME
{

    /// Drawing with layers, e.g. CaretLayer
    /// TextArea.OffsetProperty for AffectsRender and AvaloniaProperty.Register
    public class CodeEditor
    {
        private static Bitmap Foo = new Bitmap("res/foo.png");
        private static Bitmap Bulb = new Bitmap("res/bulb.png");


        private CompletionWindow _completionWindow;
        private OverloadInsightWindow _insightWindow;
        private MarkerMargin _errorMargin;
        private MarkerMargin _bulbMargin;
        private ContextActionsBulbContextMenu _contextMenu;
        private SelectionMatchRenderer _selectionMatchRenderer;
        private DispatcherTimer _delayMoveTimer;
        private ScrollBar? _verticalScrollBar;

        public TextEditor EditorControl { get; private set; }
        private SyntaxHighlightTransformer SyntaxHighlighter { get; }

        public CodeEditor()
        {
            this.EditorControl = new TextEditor()
            {
                Name = "Editor",
                FontFamily = FontFamily.Parse("Consolas,Menlo,Monospace"),
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                FontWeight = FontWeight.Light,
                FontSize = 14
            };

            this.EditorControl.ShowLineNumbers = true;
            this.EditorControl.Options.ShowBoxForControlCharacters = true;
            this.EditorControl.Options.ColumnRulerPositions = new List<int>() { 80, 100 };
            this.EditorControl.TextArea.RightClickMovesCaret = true;
            this.EditorControl.TextArea.TextView.Options.AllowScrollBelowDocument = false;
            this.EditorControl.TextArea.IndentationStrategy = new AvaloniaEdit.Indentation.CSharp.CSharpIndentationStrategy(this.EditorControl.Options);
            this.EditorControl.TextArea.TextView.Options.HighlightCurrentLine = true;
            this.EditorControl.TextArea.TextView.CurrentLineBackground = Brushes.Transparent;
            this.EditorControl.TextArea.TextView.CurrentLineBorder = new Pen(Brushes.LightGray);

            // General context menu
            this.EditorControl.ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                    {
                        new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                        new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                        new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) }
                    }
            };

            // Error margin
            _errorMargin = new MarkerMargin { Width = 16, MarkerImage = Foo };
            this._errorMargin.IsVisible = true;
            this.EditorControl.TextArea.LeftMargins.Insert(0, _errorMargin);

            // Bulb margin with menu
            _contextMenu = new ContextActionsBulbContextMenu();
            _bulbMargin = new MarkerMargin { Width = 16, Margin = new Thickness(0, 0, 5, 0) };
            _bulbMargin.MarkerPointerDown += (o, e) => OpenContextMenu();
            _bulbMargin.MarkerImage = Bulb;
            var index = this.EditorControl.TextArea.LeftMargins.Count > 0 ? this.EditorControl.TextArea.LeftMargins.Count - 1 : 0;
            this.EditorControl.TextArea.LeftMargins.Insert(index, _bulbMargin);

            this.EditorControl.KeyDown += ContextActionsRendererKeyDown;
            this.EditorControl.TextArea.Caret.PositionChanged += CaretPositionChanged;

            // Scrollbar code map
            this.EditorControl.TemplateApplied += TextEditorTemplateApplied;

            // Selection renderer
            _selectionMatchRenderer = new SelectionMatchRenderer();
            _selectionMatchRenderer.TextEditor = this.EditorControl;
            this.EditorControl.TextArea.TextView.BackgroundRenderers.Add(_selectionMatchRenderer);
            this.EditorControl.TextArea.SelectionChanged += TextAreaSelectionChanged;

            // Completion
            this.EditorControl.TextArea.TextEntering += TextAreaTextEntering;
            this.EditorControl.TextArea.TextEntered += TextAreaTextEntered;

            // Syntax highlighting
            this.SyntaxHighlighter = new SyntaxHighlightTransformer();
            this.EditorControl.TextArea.TextView.LineTransformers.Add(this.SyntaxHighlighter);
            this._selectionMatchRenderer.SyntaxHighlighter = this.SyntaxHighlighter;

            // Smalltalk-like in-line run results
            this.EditorControl.TextArea.TextView.ElementGenerators.Add(new RunResultElementGenerator());

            _delayMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _delayMoveTimer.Stop();
            _delayMoveTimer.Tick += DelayMoveTimerTick;
            this.EditorControl.TextChanged += CaretPositionChanged;
        }

        public IStyle[] GetWindowCompletionStyles() =>
            new IStyle[]{
                new StyleInclude((Uri?)null) { Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml") },
                Style(out var completionStyle)
                    .Selector(x => x.OfType<CompletionList>().Template().OfType<CompletionListBox>()
                      .Name("PART_ListBox"))
                    .SetAutoCompleteBoxItemTemplate(new DataTemplate() { DataType = typeof(ICompletionData), Content = new TextBlock() })};

        private void TextEditorTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            if (_verticalScrollBar is null)
            {
                var scrollView = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
                scrollView.TemplateApplied += (s, ee) =>
                {
                    if (_verticalScrollBar is null)
                    {
                        ee.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar").AllowAutoHide = false;
                        _verticalScrollBar = ee.NameScope.Find<ScrollBar>("PART_VerticalScrollBar");
                        _verticalScrollBar.AllowAutoHide = false;
                        _verticalScrollBar.Width = 20;
                        _selectionMatchRenderer.VerticalScroll = _verticalScrollBar;
                        _verticalScrollBar.TemplateApplied += (_, eee) =>
                        {
                            if (_selectionMatchRenderer.ScrollLineUpButton is null)
                            {
                                _selectionMatchRenderer.ScrollLineUpButton = eee.NameScope.Find<Button>("PART_LineUpButton");
                            }
                        };
                    }
                };
            }
        }

        private void TextAreaSelectionChanged(object? sender, EventArgs e)
        {
            _selectionMatchRenderer.Matches.Clear();
            string selectedText = EditorControl.TextArea.Selection.GetText();
            if (selectedText is not null && selectedText.Length > 0)
            {
                int startIndex = 0;
                while (true)
                {
                    startIndex = EditorControl.Text.IndexOf(selectedText, startIndex);
                    if (startIndex == -1)
                    {
                        break;
                    }

                    if (!EditorControl.TextArea.Selection.Contains(startIndex))
                    {
                        _selectionMatchRenderer.Matches.Add(new TextSegment() { StartOffset = startIndex, Length = selectedText.Length });
                    }

                    startIndex += selectedText.Length;
                }
            }
        }

        private void TextAreaTextEntering(object sender, TextInputEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }

            _insightWindow?.Hide();

            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        private async void ContextActionsRendererKeyDown(object? sender, KeyEventArgs e)
        {
            if (!(e.Key == Key.OemPeriod && e.KeyModifiers.HasFlag(KeyModifiers.Control))) return;

            this._delayMoveTimer.Stop();
            // !ANY actions?
            // {
            //     HideBulb();
            //     return;
            // }

            // _contextMenu.SetItems(_actions!);
            _contextMenu.ItemsSource = new List<MenuItem>
                {
                    new MenuItem { Header = "Foo" },
                    new MenuItem { Header = "Bar" },
                };

            _bulbMargin.LineNumber = EditorControl.TextArea.Caret.Line;
            OpenContextMenu();
        }

        private void HideBulb() => _bulbMargin.LineNumber = null;

        private void CaretPositionChanged(object? sender, EventArgs e)
        {
            this._delayMoveTimer.Stop();
            this._delayMoveTimer.Start();
        }
        private async void DelayMoveTimerTick(object? sender, EventArgs e)
        {
            if (!_delayMoveTimer.IsEnabled)
                return;

            this._delayMoveTimer.Stop();

            // !ANY actions?
            // {
            //     HideBulb();
            //     return;
            // }

            _contextMenu.ItemsSource = new List<MenuItem>
                {
                    new MenuItem { Header = "Hide", Command = new LambdaCommand<object>(_ => _bulbMargin.LineNumber = null) },
                    new MenuItem { Header = "BarClick" },
                };

            _bulbMargin.LineNumber = EditorControl.TextArea.Caret.Line;

            try
            {
                Universe parseUniverse = new Universe();
                parseUniverse.ExecuteAll2(this.EditorControl.Text, out var tokens, parseOnly: true);

                this.SyntaxHighlighter.Tokens = tokens;
                this.SyntaxHighlighter.SyntaxErrorOffset = null;
                this.SyntaxHighlighter.SyntaxErrorLine = null;
                this._errorMargin.LineNumber = null;
            }
            catch (DatalogException ex)
            {
                this.SyntaxHighlighter.Tokens = ex.Tokens;
                int i = ex.TokenIndex;
                if (this.SyntaxHighlighter.Tokens is not null && this.SyntaxHighlighter.Tokens.Count > i)
                {
                    this.SyntaxHighlighter.SyntaxErrorOffset = this.SyntaxHighlighter.Tokens[i].Offset;
                    this.SyntaxHighlighter.SyntaxErrorLine = this.SyntaxHighlighter.Tokens[i].Line + 1;
                    this._errorMargin.LineNumber = this.SyntaxHighlighter.SyntaxErrorLine;
                    this._errorMargin.Message = ex.Message;
                }
            }

            this.EditorControl.TextArea.TextView.Redraw();
        }

        private void OpenContextMenu()
        {
            _contextMenu.Open(_bulbMargin.Marker);
        }

        private void TextAreaTextEntered(object sender, TextInputEventArgs e)
        {
            if (e.Text == ".")
            {

                _completionWindow = new CompletionWindow(e.Source as TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;

                _completionWindow.HorizontalScrollBarVisibilityVisible();
                _completionWindow.CompletionList.ListBox.ItemTemplate = new FuncDataTemplate<CompletionDotProvider>((data, nameScope) =>
                    StackPanel()
                      .OrientationHorizontal().Height(18).VerticalAlignmentCenter()
                      .Children(
                        Image().Width(15).Height(15).Source(data.Image),
                        TextBlock().VerticalAlignmentCenter().Margin(10, 0, 0, 0).FontSize(15).Text(data.Text))
                    , false);

                var data = _completionWindow.CompletionList.CompletionData;
                data.Add(new CompletionDotProvider("Item1", Foo));
                data.Add(new CompletionDotProvider("Item2", Foo));
                data.Add(new CompletionDotProvider("Item3", Foo));
                data.Add(new CompletionDotProvider("Item4", Foo));
                data.Add(new CompletionDotProvider("Item5", Foo));
                data.Add(new CompletionDotProvider("Item6", Foo));
                data.Add(new CompletionDotProvider("Item7", Foo));
                data.Add(new CompletionDotProvider("Item8", Foo));
                data.Add(new CompletionDotProvider("Item9", Foo));
                data.Add(new CompletionDotProvider("Item10", Foo));
                data.Add(new CompletionDotProvider("Item11", Foo));
                data.Add(new CompletionDotProvider("Item12", Foo));
                data.Add(new CompletionDotProvider("Item13", Foo));


                _completionWindow.Show();
            }
            else if (e.Text == "(")
            {
                _insightWindow = new OverloadInsightWindow(e.Source as TextArea);
                _insightWindow.Closed += (o, args) => _insightWindow = null;

                _insightWindow.Provider = new CompletionOverloadProvider(new[]
                {
                    ("Method1(int, string)", "Method1 description"),
                    ("Method2(int)", "Method2 description"),
                    ("Method3(string)", "Method3 description"),
                });

                _insightWindow.Show();
            }
        }

        public class CompletionDotProvider : ICompletionData
        {
            public CompletionDotProvider(string text, IBitmap image)
            {
                this.Text = text;
                this.Image = image;
            }

            public IBitmap Image { get; }

            public string Text { get; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content => Text;

            public object Description => new ScrollViewer()
            {
                Content = new TextBlock() { Text = "Description for " + Text, TextWrapping = TextWrapping.Wrap, MaxWidth = 200, Background = Brushes.White },
                MaxHeight = 100,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };

            public double Priority { get; } = 0;

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }

        private class CompletionOverloadProvider : IOverloadProvider
        {
            private readonly IList<(string header, string content)> _items;
            private int _selectedIndex;

            public CompletionOverloadProvider(IList<(string header, string content)> items)
            {
                _items = items;
                SelectedIndex = 0;
            }

            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    _selectedIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentHeader));
                    OnPropertyChanged(nameof(CurrentContent));
                }
            }

            public int Count => _items.Count;
            public string CurrentIndexText => null;
            public object CurrentHeader => _items[SelectedIndex].header;
            public object CurrentContent => _items[SelectedIndex].content;

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class SyntaxHighlightTransformer : DocumentColorizingTransformer
    {
        public List<Token<Token>>? Tokens { get; set; }
        public int? SyntaxErrorOffset { get; set; }
        public int? SyntaxErrorLine { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.LineNumber == 2)
            {
                string lineText = this.CurrentContext.Document.GetText(line);

                int indexOfUnderline = lineText.IndexOf("underline");
                int indexOfStrikeThrough = lineText.IndexOf("strikethrough");

                if (indexOfUnderline != -1)
                {
                    ChangeLinePart(
                        line.Offset + indexOfUnderline,
                        line.Offset + indexOfUnderline + "underline".Length,
                        visualLine =>
                        {
                            if (visualLine.TextRunProperties.TextDecorations != null)
                            {
                                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Underline[0] };

                                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                                visualLine.TextRunProperties.SetForegroundBrush(Brushes.Red);
                            }
                            else
                            {
                                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                                visualLine.TextRunProperties.SetForegroundBrush(Brushes.Green);
                            }
                        }
                    );
                }

                if (indexOfStrikeThrough != -1)
                {
                    ChangeLinePart(
                        line.Offset + indexOfStrikeThrough,
                        line.Offset + indexOfStrikeThrough + "strikethrough".Length,
                        visualLine =>
                        {
                            if (visualLine.TextRunProperties.TextDecorations != null)
                            {
                                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Strikethrough[0] };

                                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                            }
                            else
                            {
                                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
                            }
                        }
                    );
                }
            }

            if (this.SyntaxErrorOffset is not null && this.SyntaxErrorOffset >= line.Offset && this.SyntaxErrorOffset <= line.EndOffset)
            {
                ChangeLinePart(
                    this.SyntaxErrorOffset.Value,
                    Math.Min(line.EndOffset, this.SyntaxErrorOffset.Value + 1),
                    visualLine =>
                    {
                        if (visualLine.TextRunProperties.TextDecorations != null)
                        {
                            var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations)
                            {
                                    CustomTextDecorations.SquiggleUnderline[0]
                            };

                            visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                            visualLine.TextRunProperties.SetForegroundBrush(Brushes.Red);
                        }
                        else
                        {
                            visualLine.TextRunProperties.SetTextDecorations(CustomTextDecorations.SquiggleUnderline);
                            visualLine.TextRunProperties.SetForegroundBrush(Brushes.Red);
                        }
                    }
                );
            }
        }
    }

    public sealed class LambdaCommand<T> : ICommand
    {
        private readonly Action<T> _cb;
        private bool _busy;
        private Func<T, Task> _acb;

        public LambdaCommand(Action<T> cb)
        {
            _cb = cb;
        }

        public LambdaCommand(Func<T, Task> cb)
        {
            _acb = cb;
        }

        private bool Busy
        {
            get => _busy;
            set
            {
                _busy = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => !_busy;


        public async void Execute(object parameter)
        {
            if (Busy)
                return;
            try
            {
                Busy = true;
                if (_cb != null)
                    _cb((T)parameter);
                else
                    await _acb((T)parameter);
            }
            finally
            {
                Busy = false;
            }
        }
    }

    public class SelectionMatchRenderer : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Background;

        public SelectionMatchRenderer()
        {
            _markerBrush = Brushes.LightSteelBlue;
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

            if (VerticalScroll is not null && ScrollLineUpButton is not null)
            {
                double mapHeight = VerticalScroll.Bounds.Height - ScrollLineUpButton.Height * 2;
                double mapX = textView.Bounds.Right - 10 - VerticalScroll.Width;

                // Caret:
                double caretInRange = mapHeight * (TextEditor.TextArea.Caret.Line - 1) / TextEditor.LineCount;
                drawingContext.FillRectangle(Brushes.Gray, new Rect(mapX, caretInRange + ScrollLineUpButton.Height, 10, 2));

                // Syntax error
                if (this.SyntaxHighlighter is not null && this.SyntaxHighlighter.SyntaxErrorLine is not null)
                {
                    double errorInRange = mapHeight * (this.SyntaxHighlighter.SyntaxErrorLine.Value - 1) / TextEditor.LineCount;
                    drawingContext.FillRectangle(Brushes.Red, new Rect(mapX + 6, errorInRange + ScrollLineUpButton.Height, 4, 4));
                }
            }

            if (Matches == null || Matches.Count == 0 || !textView.VisualLinesValid)
                return;

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
    }

    /// <summary>
    /// Defines a set of commonly used text decorations.
    /// </summary>
    public static class CustomTextDecorations
    {
        static CustomTextDecorations()
        {
            Underline = new TextDecorationCollection
                        {
                            new TextDecoration
                            {
                                Location = Avalonia.Media.TextDecorationLocation.Underline
                            }
                        };

            Strikethrough = new TextDecorationCollection
                            {
                                new TextDecoration
                                {
                                    Location = Avalonia.Media.TextDecorationLocation.Strikethrough
                                }
                            };

            SquiggleUnderline = new TextDecorationCollection
                       {
                           new TextDecoration
                           {
                               Location = Avalonia.Media.TextDecorationLocation.Underline,
                               StrokeDashArray = new AvaloniaList<double>{1, 1},
                               Stroke = Brushes.Red,
                               StrokeThickness = 3,
                               StrokeThicknessUnit = TextDecorationUnit.Pixel,
                               StrokeOffsetUnit = TextDecorationUnit.Pixel,
                               StrokeOffset = 4
                           }
                       };

            Baseline = new TextDecorationCollection
                       {
                           new TextDecoration
                           {
                               Location = Avalonia.Media.TextDecorationLocation.Baseline
                           }
                       };
        }

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing an underline.
        /// </summary>
        public static TextDecorationCollection Underline { get; }

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing a strikethrough.
        /// </summary>
        public static TextDecorationCollection Strikethrough { get; }

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing an overline.
        /// </summary>
        public static TextDecorationCollection SquiggleUnderline { get; }

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing a baseline.
        /// </summary>
        public static TextDecorationCollection Baseline { get; }
    }
}

sealed class RunResultElementGenerator : VisualLineElementGenerator
{
    public override int GetFirstInterestedOffset(int startOffset)
    {
        var endLine = CurrentContext.VisualLine.LastDocumentLine;
        var relevantText = CurrentContext.GetText(startOffset, endLine.EndOffset - startOffset);

        for (var i = 0; i < relevantText.Count; i++)
        {
            var c = relevantText.Text[relevantText.Offset + i];
            switch (c)
            {
                case 'ø':
                case '§':
                    // The offset will come from the parser/runner
                    return startOffset + i;
                default:
                    break;
            }
        }
        return -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        // The offset will come from the parser/runner
        var c = CurrentContext.Document.GetCharAt(offset);

        if (c == '§')
        {
            var runProperties = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
            runProperties.SetForegroundBrush(Brushes.White);
            var text = FormattedTextElement.PrepareText(TextFormatter.Current, "bananas", runProperties);
            return new SpecialCharacterBoxElement(text);
        }
        else if (c == 'ø')
        {
            // Probably the simpler approach
            return new InlineObjectElement(0, new TextBox() { Text = "Foo bar\nnextLine\ntoo", BorderBrush = Brushes.Gray, BorderThickness = new Thickness(2), IsReadOnly = true });
        }

        return null;
    }

    private sealed class SpecialCharacterBoxElement : FormattedTextElement
    {
        public SpecialCharacterBoxElement(TextLine line) : base(line, 1)
        {
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            return new SpecialCharacterTextRun(this, TextRunProperties);
        }
    }

    internal sealed class SpecialCharacterTextRun : FormattedTextRun
    {
        private static readonly ISolidColorBrush DarkGrayBrush;

        internal const double BoxMargin = 3;

        static SpecialCharacterTextRun()
        {
            DarkGrayBrush = new ImmutableSolidColorBrush(Color.FromArgb(200, 128, 128, 128));
        }

        public SpecialCharacterTextRun(FormattedTextElement element, TextRunProperties properties)
            : base(element, properties)
        {
        }

        public override Size Size
        {
            get
            {
                var s = base.Size;

                return s.WithWidth(s.Width + BoxMargin);
            }
        }

        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            var (x, y) = origin;

            var newOrigin = new Point(x + (BoxMargin / 2), y);

            var (width, height) = Size;

            var r = new Rect(x, y, width, height);

            drawingContext.FillRectangle(DarkGrayBrush, r, 2.5f);

            base.Draw(drawingContext, newOrigin);
        }
    }
}