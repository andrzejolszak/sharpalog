using Ast2;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using NXUI.Extensions;
using RoslynPad.Editor;
using Sharplog.KME;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvaloniaEdit.Demo
{
    class Program
    {
        private static CodeEditor _editor1;
        private static CodeEditor _editor2;
        private static Ast2Editor _astEditor;

        public static int Main(string[] args)
        {
            /// Drawing with layers, e.g. CaretLayer
            /// TextArea.OffsetProperty for AffectsRender and AvaloniaProperty.Register

            Window Build()
            {
                _editor1 = new CodeEditor();
                _editor2 = new CodeEditor();
                _astEditor = new Ast2Editor(new CodeEditor());
                _astEditor.Editor.EditorControl.Height = 400;
                _astEditor.InitAndLoadExample(0);
                _editor1.EditorControl.Document = new TextDocument(
"% AvaloniaEdit supports displaying control chars: \a or \b or \v" + Environment.NewLine +
"% AvaloniaEdit supports displaying underline and strikethrough" + Environment.NewLine +
"foo(a, b). bar(B, x) :- g, c.");
                _editor2.EditorControl.Document = new TextDocument("one_liner(a, 1, X).");

                Window(out var window)
                    .Styles(_editor1.GetWindowCompletionStyles())
                    .Title("NXUI").Width(800).Height(650)
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
                            _editor1.EditorControl,
                            Label().Content("One-liner: "),
                            _editor2.EditorControl,
                            _astEditor.Editor.EditorControl
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
    }
}
