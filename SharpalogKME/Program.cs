using Avalonia;
using Avalonia.Markup.Parsers;
using Avalonia.Media.Imaging;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Microsoft.Win32;
using NXUI.Extensions;
using System.ComponentModel;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace AvaloniaEdit.Demo
{
    class Program
    {
        private static CompletionWindow _completionWindow;
        private static OverloadInsightWindow _insightWindow;

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
                    Background = Brushes.Red,
                    Height = 300
                };

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
                //textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
                textEditor.TextArea.RightClickMovesCaret = true;

                textEditor.Document = new TextDocument(
    "// AvaloniaEdit supports displaying control chars: \a or \b or \v" + Environment.NewLine +
    "// AvaloniaEdit supports displaying underline and strikethrough");
                textEditor.TextArea.TextView.LineTransformers.Add(new UnderlineAndStrikeThroughTransformer());

                return Window(out var window)
                    .Styles(
                        new StyleInclude((Uri?)null) { Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml") },
                        completionStyle)
                    .Title("NXUI").Width(400).Height(300)
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
            }

            return AppBuilder.Configure<Application>()
              .UsePlatformDetect()
              .UseFluentTheme()
              .StartWithClassicDesktopLifetime(Build, args);
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

        private static void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e)
        {
            if (e.Text == ".")
            {

                _completionWindow = new CompletionWindow(e.Source as TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;

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

            public IBitmap Image => null;

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
}
