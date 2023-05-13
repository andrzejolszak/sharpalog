using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using RoslynPad.Editor;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvaloniaEdit.Demo
{
    class Program
    {
        private static CompletionWindow _completionWindow;
        private static OverloadInsightWindow _insightWindow;
        private static MarkerMargin _errorMargin;
        private static MarkerMargin _bulbMargin;
        private static ContextActionsBulbContextMenu _contextMenu;
        private static Bitmap Foo = new Bitmap("res/foo.png");
        private static Bitmap Bulb = new Bitmap("res/bulb.png");
        private static TextEditor _editor;
        private static SearchResultBackgroundRenderer _selectionRenderer;
        private static DispatcherTimer _delayMoveTimer;
        private static ScrollBar? _verticalScrollBar;

        public static int Main(string[] args)
        {
            /// Simple highlighting: DocumentColorizingTransformer
            /// Drawing with layers, e.g. CaretLayer
            /// TextArea.OffsetProperty for AffectsRender and AvaloniaProperty.Register

            Window Build()
            {
                Style(out var completionStyle)
                    .Selector(x => x.OfType<CompletionList>().Template().OfType<CompletionListBox>()
                      .Name("PART_ListBox"))
                    .SetAutoCompleteBoxItemTemplate(new DataTemplate() { DataType = typeof(ICompletionData), Content = new TextBlock() });

                TextEditor textEditor = new TextEditor()
                {
                    Name = "Editor",
                    FontFamily = FontFamily.Parse("Consolas,Menlo,Monospace"),
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                    FontWeight = FontWeight.Light,
                    FontSize = 14,
                    Height = 400
                };

                textEditor.TextArea.TextView.Options.AllowScrollBelowDocument = false;
                textEditor.TextArea.TextView.Options.HighlightCurrentLine = true;

                textEditor.ShowLineNumbers = true;
                textEditor.ContextMenu = new ContextMenu
                {
                    ItemsSource = new List<MenuItem>
                    {
                        new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                        new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                        new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) }
                    }
                };

                textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
                textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
                textEditor.Options.ShowBoxForControlCharacters = true;
                textEditor.Options.ColumnRulerPositions = new List<int>() { 80, 100 };
                textEditor.TextArea.IndentationStrategy = new Indentation.CSharp.CSharpIndentationStrategy(textEditor.Options);
                textEditor.TextArea.RightClickMovesCaret = true;

                textEditor.Document = new TextDocument(
    "// AvaloniaEdit supports displaying control chars: \a or \b or \v" + Environment.NewLine +
    "-- AvaloniaEdit supports displaying underline and strikethrough" + Environment.NewLine +
    "foo(a, b). bar(B, x) :- g, c.");
                textEditor.TextArea.TextView.LineTransformers.Add(new UnderlineAndStrikeThroughTransformer());

                _errorMargin = new MarkerMargin { Width = 16, MarkerImage = Foo };
                textEditor.TextArea.LeftMargins.Insert(0, _errorMargin);
                _errorMargin.IsVisible = true;
                _errorMargin.LineNumber = 2;
                _errorMargin.Message = "Foo";

                _delayMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _delayMoveTimer.Stop();
                _delayMoveTimer.Tick += TimerMoveTick;

                textEditor.KeyDown += ContextActionsRenderer_KeyDown;

                textEditor.TextArea.Caret.PositionChanged += CaretPositionChanged;

                _contextMenu = new ContextActionsBulbContextMenu();
                _bulbMargin = new MarkerMargin { Width = 16, Margin = new Thickness(0, 0, 5, 0) };
                _bulbMargin.MarkerPointerDown += (o, e) => OpenContextMenu();
                _bulbMargin.MarkerImage = Bulb;
                var index = textEditor.TextArea.LeftMargins.Count > 0 ? textEditor.TextArea.LeftMargins.Count - 1 : 0;
                textEditor.TextArea.LeftMargins.Insert(index, _bulbMargin);
                _editor = textEditor;

                _selectionRenderer = new SearchResultBackgroundRenderer();
                _selectionRenderer.TextEditor = textEditor;
                textEditor.TextArea.TextView.BackgroundRenderers.Add(_selectionRenderer);
                textEditor.TextArea.SelectionChanged += TextArea_SelectionChanged;

                textEditor.TemplateApplied += TextEditor_TemplateApplied;

                Window(out var window)
                    .Styles(
                        new StyleInclude((Uri?)null) { Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml") },
                        completionStyle)
                    .Title("NXUI").Width(800).Height(600)
                    .Content(
                      StackPanel()
                        .Children(
                          Button(out var button)
                            .Content("Welcome to Avalonia, please click me!"),
                          TextBox(out var tb1)
                            .Text("NXUI"),
                          TextBox()
                            .Text(window.BindTitle()),
                          Label()
                            .Content(button.ObserveOnClick().Select(x => $"You clicked.")),
                          textEditor
                          )
                        )
                    .Title(tb1.ObserveText().Select(x => x?.ToUpper()));
                
                return window;
            }

            return AppBuilder.Configure<Application>()
              .UsePlatformDetect()
              .UseFluentTheme()
              .StartWithClassicDesktopLifetime(Build, args);
        }

        private static void TextEditor_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
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
                        _selectionRenderer.VerticalScroll = _verticalScrollBar;
                        _verticalScrollBar.TemplateApplied += (_, eee) =>
                        { 
                            if (_selectionRenderer.ScrollLineUpButton is null)
                            {
                                _selectionRenderer.ScrollLineUpButton = eee.NameScope.Find<Button>("PART_LineUpButton");
                            }
                        };
                    }
                };
            }
        }

        private static void TextArea_SelectionChanged(object? sender, EventArgs e)
        {
            _selectionRenderer.Matches.Clear();
            string selectedText = _editor.TextArea.Selection.GetText();
            if (selectedText is not null && selectedText.Length > 0)
            {
                int startIndex = 0;
                while (true)
                {
                    startIndex = _editor.Text.IndexOf(selectedText, startIndex);
                    if (startIndex == -1)
                    {
                        break;
                    }

                    if (!_editor.TextArea.Selection.Contains(startIndex))
                    {
                        _selectionRenderer.Matches.Add(new TextSegment() { StartOffset = startIndex, Length = selectedText.Length });
                    }

                    startIndex += selectedText.Length;
                }
            }
        }

        private static void textEditor_TextArea_TextEntering(object sender, TextInputEventArgs e)
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

        private static async void ContextActionsRenderer_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!(e.Key == Key.OemPeriod && e.KeyModifiers.HasFlag(KeyModifiers.Control))) return;
            
            Cancel();
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

            _bulbMargin.LineNumber = _editor.TextArea.Caret.Line;
            OpenContextMenu();
        }

        private static void HideBulb() => _bulbMargin.LineNumber = null;

        private static void CaretPositionChanged(object? sender, EventArgs e) => StartTimer();

        private static void StartTimer()
        {
            _delayMoveTimer.Start();
        }

        private static void Cancel()
        {
            _delayMoveTimer.Stop();
        }

        private static async void TimerMoveTick(object? sender, EventArgs e)
        {
            if (!_delayMoveTimer.IsEnabled)
                return;

            Cancel();

            // !ANY actions?
            // {
            //     HideBulb();
            //     return;
            // }

            _contextMenu.ItemsSource = new List<MenuItem>
                {
                    new MenuItem { Header = "Hide", Command = MiniCommand.Create(() => { _bulbMargin.LineNumber = null; }) },
                    new MenuItem { Header = "BarClick" },
                };

            _bulbMargin.LineNumber = _editor.TextArea.Caret.Line;
        }

        private static void OpenContextMenu()
        {
            _contextMenu.Open(_bulbMargin.Marker);
        }

        private static void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e)
        {
            if (e.Text == ".")
            {

                _completionWindow = new CompletionWindow(e.Source as TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;

                _completionWindow.HorizontalScrollBarVisibilityVisible();
                _completionWindow.CompletionList.ListBox.ItemTemplate = new FuncDataTemplate<MyCompletionData>((data, _) =>
                    StackPanel()
                      .OrientationHorizontal().Height(18).VerticalAlignmentCenter()
                      .Children(
                        Image().Width(15).Height(15).Source(data.Image),
                        TextBlock().VerticalAlignmentCenter().Margin(10, 0, 0, 0).FontSize(15).Text(data.Text))
                    , true);

                var data = _completionWindow.CompletionList.CompletionData;
                data.Add(new MyCompletionData("Item1"));
                data.Add(new MyCompletionData("Item2"));
                data.Add(new MyCompletionData("Item3"));
                data.Add(new MyCompletionData("Item4"));
                data.Add(new MyCompletionData("Item5"));
                data.Add(new MyCompletionData("Item6"));
                data.Add(new MyCompletionData("Item7"));
                data.Add(new MyCompletionData("Item8"));
                data.Add(new MyCompletionData("Item9"));
                data.Add(new MyCompletionData("Item10"));
                data.Add(new MyCompletionData("Item11"));
                data.Add(new MyCompletionData("Item12"));
                data.Add(new MyCompletionData("Item13"));


                _completionWindow.Show();
            }
            else if (e.Text == "(")
            {
                _insightWindow = new OverloadInsightWindow(e.Source as TextArea);
                _insightWindow.Closed += (o, args) => _insightWindow = null;

                _insightWindow.Provider = new MyOverloadProvider(new[]
                {
                    ("Method1(int, string)", "Method1 description"),
                    ("Method2(int)", "Method2 description"),
                    ("Method3(string)", "Method3 description"),
                });

                _insightWindow.Show();
            }
        }

        public class MyCompletionData : ICompletionData
        {
            public MyCompletionData(string text)
            {
                Text = text;
            }

            public IBitmap Image => Foo;

            public string Text { get; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content => Text;

            public object Description => "Description for " + Text;

            public double Priority { get; } = 0;

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }

        private class MyOverloadProvider : IOverloadProvider
        {
            private readonly IList<(string header, string content)> _items;
            private int _selectedIndex;

            public MyOverloadProvider(IList<(string header, string content)> items)
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
                    // ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(CurrentHeader));
                    OnPropertyChanged(nameof(CurrentContent));
                    // ReSharper restore ExplicitCallerInfoArgument
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

        class UnderlineAndStrikeThroughTransformer : DocumentColorizingTransformer
        {
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
                                }
                                else
                                {
                                    visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
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
            }
        }
    }

    public sealed class MiniCommand<T> : MiniCommand, ICommand
    {
        private readonly Action<T> _cb;
        private bool _busy;
        private Func<T, Task> _acb;

        public MiniCommand(Action<T> cb)
        {
            _cb = cb;
        }

        public MiniCommand(Func<T, Task> cb)
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


        public override event EventHandler CanExecuteChanged;
        public override bool CanExecute(object parameter) => !_busy;

        public override async void Execute(object parameter)
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

    public abstract class MiniCommand : ICommand
    {
        public static MiniCommand Create(Action cb) => new MiniCommand<object>(_ => cb());
        public static MiniCommand Create<TArg>(Action<TArg> cb) => new MiniCommand<TArg>(cb);
        public static MiniCommand CreateFromTask(Func<Task> cb) => new MiniCommand<object>(_ => cb());

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);
        public abstract event EventHandler CanExecuteChanged;
    }

    internal class SearchResultBackgroundRenderer : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Background;

        public SearchResultBackgroundRenderer()
        {
            _markerBrush = Brushes.LightSteelBlue;
        }

        private IBrush _markerBrush;

        public ScrollBar VerticalScroll { get; set; }

        public Button ScrollLineUpButton { get; set; }

        public List<TextSegment> Matches { get; set; } = new List<TextSegment>();
        public TextEditor TextEditor { get; internal set; }

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
}
