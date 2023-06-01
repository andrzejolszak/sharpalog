﻿using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using RoslynPad.Editor;
using Stringes;
using static Sharplog.KME.Completion;

namespace Sharplog.KME;

/// Drawing with layers, e.g. CaretLayer
/// TextArea.OffsetProperty for AffectsRender and AvaloniaProperty.Register
public class CodeEditor
{
    public Completion Completion { get; private set; }
    private MarkerMargin _errorMargin;
    private MarkerMargin _bulbMargin;
    private ContextActionsBulbContextMenu _contextMenu;
    private CodeMapAndBackgroundRenderer _selectionMatchRenderer;
    private DispatcherTimer _delayMoveTimer;
    private FreeFormInsightWindow _hoverInsightsWindow;

    public TextEditor EditorControl { get; private set; }
    public SyntaxHighlightTransformer SyntaxHighlighter { get; }

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
        _errorMargin = new MarkerMargin { Width = 16, MarkerImage = Resources.Foo };
        this._errorMargin.IsVisible = true;
        this.EditorControl.TextArea.LeftMargins.Insert(0, _errorMargin);

        // Bulb margin with menu
        _contextMenu = new ContextActionsBulbContextMenu();
        _bulbMargin = new MarkerMargin { Width = 16, Margin = new Thickness(0, 0, 5, 0) };
        _bulbMargin.MarkerPointerDown += (o, e) => OpenContextMenu();
        _bulbMargin.MarkerImage = Resources.Bulb;
        var index = this.EditorControl.TextArea.LeftMargins.Count > 0 ? this.EditorControl.TextArea.LeftMargins.Count - 1 : 0;
        this.EditorControl.TextArea.LeftMargins.Insert(index, _bulbMargin);

        this.EditorControl.KeyDown += ContextActionsRendererKeyDown;
        this.EditorControl.TextArea.Caret.PositionChanged += CaretPositionChanged;

        // Scrollbar code map and code background
        _selectionMatchRenderer = new CodeMapAndBackgroundRenderer(this.EditorControl);

        // Completion
        this.Completion = new Completion(this.EditorControl);

        // Syntax highlighting
        this.SyntaxHighlighter = new SyntaxHighlightTransformer();
        this.EditorControl.TextArea.TextView.LineTransformers.Add(this.SyntaxHighlighter);
        this._selectionMatchRenderer.SyntaxHighlighter = this.SyntaxHighlighter;

        // Smalltalk-like in-line run results
        this.EditorControl.TextArea.TextView.ElementGenerators.Add(new RunResultElementGenerator());

        // Read-only: will be useful for projectional
        this.EditorControl.TextArea.ReadOnlySectionProvider = new CustomReadOnlySectionProvider();

        // Hover
        this.EditorControl.PointerHover += EditorControl_PointerHover;

        _delayMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _delayMoveTimer.Stop();
        _delayMoveTimer.Tick += DelayMoveTimerTick;
        this.EditorControl.TextChanged += CaretPositionChanged;
    }

    private void EditorControl_PointerHover(object? sender, PointerEventArgs e)
    {
        TextViewPosition? pos = this.EditorControl.GetPositionFromPoint(e.GetPosition(this.EditorControl.TextArea));
        int offsetFromPoint = this.EditorControl.Document.GetOffset(pos.Value.Location);
        if (offsetFromPoint == this.SyntaxHighlighter.SyntaxErrorOffset)
        {
            // TODO: get from external
            this._hoverInsightsWindow?.Close();
            this._hoverInsightsWindow = new FreeFormInsightWindow(this.EditorControl.TextArea, pos.Value);
            this._hoverInsightsWindow.Provider = new CompletionOverloadProvider(new[]
            {
                    ("Syntax error:", (object)this._errorMargin.Message),
                    ("ff", (object)"bars"),
                    ("ctrl", (object)
                        StackPanel()
                        .Children(
                          Button(out var button)
                            .Content("Please click me!"),
                          Label()
                            .Content(button.ObserveOnClick().Select(x => $"You clicked.")))
                        )
                });

            this._hoverInsightsWindow.Open();
        }
    }

    public IStyle[] GetWindowCompletionStyles() =>
        new IStyle[]{
                new StyleInclude((Uri?)null) { Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml") },
                Style(out var completionStyle)
                    .Selector(x => x.OfType<CompletionList>().Template().OfType<CompletionListBox>()
                      .Name("PART_ListBox"))
                    .SetAutoCompleteBoxItemTemplate(new DataTemplate() { DataType = typeof(ICompletionData), Content = new TextBlock() })};

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
        // Move hover with text - for lens
        // if (this._hoverInsightsWindow is not null)
        // {
        //     this._hoverInsightsWindow.Position = new TextViewPosition(this._hoverInsightsWindow.Position.Line, this._hoverInsightsWindow.Position.Column + 1);
        //     this._hoverInsightsWindow.UpdatePos();
        // }

        this._hoverInsightsWindow?.Close();
        this._hoverInsightsWindow = null;

        try
        {
            this._delayMoveTimer?.Stop();
            this._delayMoveTimer?.Start();
        }
        catch (Exception ex)
        {
            // For tests
        }
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
}

public class SyntaxHighlightTransformer : DocumentColorizingTransformer
{
    public List<(int, int, VisualStyle)> ExternalStyles { get; } = new();
    public List<(int, int, VisualStyle)> ExternalSelectionStyles { get; } = new();

    public List<Token<Token>>? Tokens { get; set; }
    public int? SyntaxErrorOffset { get; set; }
    public int? SyntaxErrorLine { get; set; }

    protected override void ColorizeLine(DocumentLine line)
    {
        // TODO: perf
        foreach ((int, int, VisualStyle) s in this.ExternalStyles.Concat(this.ExternalSelectionStyles))
        {
            // TODO: styles cannot go across lines
            if (s.Item1 >= line.Offset && s.Item2 <= line.EndOffset)
            {
                ChangeLinePart(
                    s.Item1,
                    s.Item2,
                    visualLine =>
                    {
                        if (visualLine.TextRunProperties.TextDecorations != null)
                        {
                            var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations.Union(s.Item3.TextDecorations));
                            visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                        }
                        else
                        {
                            visualLine.TextRunProperties.SetTextDecorations(s.Item3.TextDecorations);
                        }

                        if (s.Item3.ForegroundBrush is not null)
                        {
                            visualLine.TextRunProperties.SetForegroundBrush(s.Item3.ForegroundBrush);
                        }

                        if (s.Item3.BackgroundBrush is not null)
                        {
                            visualLine.TextRunProperties.SetBackgroundBrush(s.Item3.BackgroundBrush);
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

class RunResultElementGenerator : VisualLineElementGenerator
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

        if (c == 'ø')
        {
            // Probably the simpler approach
            return new InlineObjectElement(0, new TextBox() { Text = "Foo bar\nnextLine\ntoo", BorderBrush = Brushes.Gray, BorderThickness = new Thickness(2), IsReadOnly = true });
        }

        return null;
    }
}

class CustomReadOnlySectionProvider : IReadOnlySectionProvider
{
    public bool CanInsert(int offset)
    {
        return offset > 5;
    }

    public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
    {
        if (segment.Offset > 5)
        {
            return ExtensionMethods.Sequence(segment);
        }
        else
        {
            return Enumerable.Empty<ISegment>();
        }
    }
}

public class FreeFormInsightWindow : InsightWindow
{
    private readonly OverloadViewer _overloadViewer = new OverloadViewer();

    public IOverloadProvider Provider
    {
        get
        {
            return _overloadViewer.Provider;
        }
        set
        {
            _overloadViewer.Provider = value;
        }
    }

    public TextViewPosition Position { get; set; }

    public void UpdatePos()
    {
        // For lens?
        base.SetPosition(this.Position);
        this.UpdatePosition();
    }

    public FreeFormInsightWindow(TextArea textArea, TextViewPosition position)
        : base(textArea)
    {
        _overloadViewer.Margin = new Thickness(2.0, 0.0, 0.0, 0.0);
        this.Position = position;
        base.Child = _overloadViewer;
        base.SetPosition(this.Position);
        base.CloseAutomatically = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && Provider != null && Provider.Count > 1)
        {
            switch (e.Key)
            {
                case Key.Up:
                    e.Handled = true;
                    _overloadViewer.ChangeIndex(-1);
                    break;
                case Key.Down:
                    e.Handled = true;
                    _overloadViewer.ChangeIndex(1);
                    break;
            }

            if (e.Handled)
            {
                UpdatePosition();
            }
        }
    }
}