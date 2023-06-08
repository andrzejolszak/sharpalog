using Ast2;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sharplog.KME;

public class Completion
{
    private readonly TextEditor _textEditor;

    public CompletionWindow CompletionWindow { get; private set; }

    private OverloadInsightWindow _overloadInsightWindow;

    public List<CompletionItem> ExternalCompletions { get; } = new List<CompletionItem>();

    public Completion(TextEditor textEditor)
    {
        this._textEditor = textEditor;

        this._textEditor.TextArea.TextEntered += this.TextAreaTextEntered;
        this._textEditor.TextArea.DefaultInputHandler.AddBinding(new RoutedCommand("completion"), KeyModifiers.Control, Key.Space, (s, e) => this.ShowCompletionMenu());
    }

    public bool IsVisible => this.CompletionWindow?.IsVisible ?? false;

    public void ShowCompletionMenu()
    {
        CompletionWindow = new CompletionWindow(this._textEditor.TextArea);
        CompletionWindow.Closed += (o, args) => CompletionWindow = null;

        CompletionWindow.HorizontalScrollBarVisibilityVisible();
        var data = CompletionWindow.CompletionList.CompletionData;
        data.AddRange(ExternalCompletions);
        data.Add(new CompletionItem("Item1", "desc sample", "replaced", Resources.Foo));
        data.Add(new CompletionItem("Item2", "desc"));

        if (CompletionWindow.CompletionList.ListBox is not null)
        {
            CompletionWindow.CompletionList.ListBox.ItemTemplate = new FuncDataTemplate<CompletionItem>((data, nameScope) =>
                StackPanel()
                  .OrientationHorizontal().Height(18).VerticalAlignmentCenter()
                  .Children(
                    Image().Width(15).Height(15).Source(data.Image),
                    TextBlock().VerticalAlignmentCenter().Margin(10, 0, 0, 0).FontSize(15).Text(data.Text))
                , false);

            CompletionWindow.CompletionList.ListBox.SelectionChanged += (s, e) =>
            {
                // TODO: mouse selection
                // _completionWindow.CompletionList.RequestInsertion(e);
            };
        }

        CompletionWindow.Show();
    }

    private void TextAreaTextEntered(object sender, TextInputEventArgs e)
    {
        if (e.Text == "(")
        {
            _overloadInsightWindow = new OverloadInsightWindow(e.Source as TextArea);
            _overloadInsightWindow.Closed += (o, args) => _overloadInsightWindow = null;

            _overloadInsightWindow.Provider = new CompletionOverloadProvider(new[]
            {
                    ("Method1(int, string)", (object)"Method1 description"),
                    ("Method2(int)", (object)"Method2 description"),
                    ("Method3(string)", (object)"Method3 description"),
                });

            _overloadInsightWindow.Show();
        }
    }

    public void Hide()
    {
        this.CompletionWindow?.Hide();
        this.CompletionWindow = null;
    }

    public class CompletionItem : ICompletionData
    {
        public CompletionItem(string text, string description, string? replacementText = null, IBitmap? image = null, Action<TextArea, ISegment, EventArgs>? completionAction = null)
        {
            this.Text = text;
            this.Image = image;
            this._description = description;
            this._replacementText = replacementText;
            this._completionAction = completionAction;
        }

        public IBitmap Image { get; }

        private readonly string _description;
        private readonly string? _replacementText;
        private readonly Action<TextArea, ISegment, EventArgs>? _completionAction;

        public string Text { get; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content => Text;

        public object Description => new ScrollViewer()
        {
            Content = new TextBlock() { Text = this._description, TextWrapping = TextWrapping.Wrap, MaxWidth = 200, Background = Brushes.White },
            MaxHeight = 100,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        public double Priority { get; } = 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this._replacementText ?? Text);
            this._completionAction?.Invoke(textArea, completionSegment, insertionRequestEventArgs);
        }
    }

    public class CompletionOverloadProvider : IOverloadProvider
    {
        private readonly IList<(string header, object content)> _items;
        private int _selectedIndex;

        public CompletionOverloadProvider(IList<(string header, object content)> items)
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