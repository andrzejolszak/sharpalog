using Avalonia.Input.GestureRecognizers;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using HarfBuzzSharp;
using IntervalTree;
using ProjectionalBlazorMonaco;
using Sharplog.KME;
using System;
using System.Text;
using static Sharplog.KME.CodeEditor;
using static Sharplog.KME.Completion;
using static System.Net.Mime.MediaTypeNames;

namespace Ast2
{
    public class Ast2Editor
    {
        public void InitAndLoadExample(int exampleNumber)
        {
            this.Init();

            this.SetRoot(new Node());
            if (exampleNumber == 0)
            {
                foreach (Action<Node, Ast2Editor> b in Tutorial.Builders)
                {
                    b.Invoke(this.Root, this);
                    this.Root.AddChild(ReadOnlyTextNode.NewLine()).AddChild(ReadOnlyTextNode.NewLine());
                }
            }
            else
            {
                this.ConsoleLog("Loading: " + exampleNumber);
                Tutorial.Builders[exampleNumber - 1].Invoke(this.Root, this);
            }

            this.HandleUserInputResult(UserInputResult.HandledNeedsGlobalRefresh());
        }

        private void ForceRefresh()
        {
            this.RefreshWholeEditor(UserInputResult.HandledNeedsGlobalRefresh());
        }

        public readonly CodeEditor Editor;

        /// <summary>
        /// Only nodes that have a View are added here, i.e. children-based nodes are not, but you can retrieve them
        /// through the parent relations.
        /// </summary>
        private IntervalTree<int, (int, Node)> _visibleNodesIntervalTree = new IntervalTree<int, (int, Node)>();

        private bool _refreshing;

        public Node Root { get; private set; }

        public List<(Type, Func<Node>)> FactoryRegistry { get; } = new List<(Type, Func<Node>)>();

        public Ast2Editor(CodeEditor monacoEditor)
        {
            this.Editor = monacoEditor;
        }

        public TextArea TextArea => this.Editor.EditorControl.TextArea;

        public TextDocument Document => this.Editor.EditorControl.Document;

        public void Init()
        {
            this.TextArea.Caret.PositionChanged += OnPositionChanged;
            this.TextArea.SelectionChanged += SelectionChanged;
            this.Editor.EditorControl.AddHandler(TextEditor.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            this.Editor.EditorControl.KeyUp += this.OnKeyUp;
            this.TextArea.TextView.GestureRecognizers.Add(new MouseRecognizer() { Parent = this });
            this.TextArea.TextEntering += TextEntering;
        }

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (this._refreshing)
            {
                return;
            }

            e.Handled = true;

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Right)
            {
                int listIndex = this.ListIndex();
                if (listIndex < this.VisibleNodesList.Count - 1)
                {
                    int newPosition = this.VisibleNodesList[listIndex].PositionInfo.EndOffset;
                    this.Select(newPosition);
                }
            }
            else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Left)
            {
                int listIndex = this.ListIndex();
                int newPosition = this.VisibleNodesList[Math.Max(0, listIndex - 1)].PositionInfo.StartOffset;
                this.Select(newPosition);
            }
            else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Down && !this.Editor.Completion.IsVisible)
            {
                DocumentLine nextLine = this.Document.GetLineByNumber(Math.Min(this.Document.LineCount, this.CurrentPosition.Line + 1));
                this.Select(nextLine.Offset + Math.Min(nextLine.Length, this.CurrentPosition.Column - 1));
            }
            else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Up && !this.Editor.Completion.IsVisible)
            {
                DocumentLine nextLine = this.Document.GetLineByNumber(Math.Max(1, this.CurrentPosition.Line - 1));
                this.Select(nextLine.Offset + Math.Min(nextLine.Length, this.CurrentPosition.Column - 1));
            }
            else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Up)
            {
                Node parent = this.CurrentNode.Parent;
                int newPosition = parent.PositionInfo.StartOffset;
                this.Select(newPosition);
            }
            else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Space)
            {
                this.RefreshCompletions();
                e.Handled = false;
            }
            else if (e.Key == Key.F5)
            {
                this.HandleUserInputResult(UserInputResult.HandledNeedsGlobalRefresh());
            }
            else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Left)
            {
                this.MoveCaret(-1);
            }
            else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Right)
            {
                this.MoveCaret(1);
            }
            else if (e.KeyModifiers == KeyModifiers.Shift && e.Key == Key.Right)
            {
                var oldPosition = this.CurrentPosition;
                this.MoveCaret(1);
                this.TextArea.Selection = this.TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, this.CurrentPosition);
            }
            else if (e.KeyModifiers == KeyModifiers.Shift && e.Key == Key.Left)
            {
                var oldPosition = this.CurrentPosition;
                this.MoveCaret(-1);
                this.TextArea.Selection = this.TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, this.CurrentPosition);
            }
            else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.End)
            {
                DocumentLine line = this.Document.GetLineByNumber(this.CurrentPosition.Line);
                int delta = line.TotalLength - line.DelimiterLength - this.CurrentPosition.Column + 1;
                if (delta > 0)
                {
                    this.MoveCaret(delta);
                }
            }
            else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Back)
            {
                int selLen = Math.Abs(this.CurrentSelectionEnd - this.CurrentSelectionStart);
                this.TextEntering(null, new TextInputEventArgs() { Text = new string('\b', selLen == 0 ? 1 : selLen) });
            }
            else if (e.Key == Key.LeftCtrl)
            {
                // TODO: does not seem to work, TODO: clear
                Node parent = GetParent(this.CurrentNode, true);
                if (parent != null && parent != this.Root)
                {
                    int min = this.CurrentNode.PositionInfo.StartOffset;
                    int max = this.CurrentNode.PositionInfo.EndOffset;
                    int listIndex = this.ListIndex();

                    for (int i = listIndex - 1; i >= 0; i--)
                    {
                        Node n = this.VisibleNodesList[i];
                        if (GetParent(n, true) != parent)
                        {
                            break;
                        }

                        min = n.PositionInfo.StartOffset;
                    }

                    for (int i = listIndex + 1; i < this.VisibleNodesList.Count; i++)
                    {
                        Node n = this.VisibleNodesList[i];
                        if (GetParent(n, true) != parent)
                        {
                            break;
                        }

                        max = n.PositionInfo.EndOffset;
                    }

                    this.AddSelectionStyle(min, max, VisualStyles.SelectedParentNodeText, null);
                }
            }
            else
            {
                e.Handled = false;

                Node currNode = this.CurrentNode;

                UserInputResult res = currNode.OnKeyDownBubble(this.GetEditorState(), e, currNode);
                this.HandleUserInputResult(res);
            }
        }

        public void MoveCaret(int delta)
        {
            TextLocation pos = this.CurrentPosition.Location;
            int offset = this.Document.GetOffset(pos);
            
            if (offset + 1 < this.Document.TextLength 
                && this.Document.GetCharAt(offset) == '\r'
                && this.Document.GetCharAt(offset + 1) == '\n'
                && delta == 1)
            {
                delta = 2;
            }
            
            TextLocation moved = this.GetPositionAt(Math.Clamp(this.Document.GetOffset(pos) + delta, 0, this.Document.TextLength));
            SetAndRevealPosition(moved);
        }

        class MouseRecognizer : IGestureRecognizer
        {
            public Ast2Editor Parent { get; set; }

            public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
            {
            }

            public void PointerCaptureLost(IPointer pointer)
            {
            }

            public void PointerMoved(PointerEventArgs e)
            {
            }

            public void PointerPressed(PointerPressedEventArgs e)
            {
                TextViewPosition? pos = this.Parent.Editor.EditorControl.GetPositionFromPoint(e.GetPosition(this.Parent.TextArea));
                if (pos is null)
                {
                    return;
                }

                (int, Node) node = this.Parent.AtPosition(this.Parent.Document.GetOffset(pos.Value.Location));
                if (node.Item2 != null)
                {
                    UserInputResult res = node.Item2.OnMouseClickBubble(this.Parent.GetEditorState(), e, node.Item2);
                    this.Parent.HandleUserInputResult(res);
                }
            }

            public void PointerReleased(PointerReleasedEventArgs e)
            {
            }
        }

        private void TextEntering(object? sender, TextInputEventArgs e)
        {
            // Only fires on text entry (i.e. not on deletion, not on other keys). Use keyBindings and set handled instead. Raise textarea.KeyDown to invoke
            // TODO: reverse bubbling
            if (this._refreshing || e == null || e.Text == null)
            {
                return;
            }
           
            // TODO: multi select
            UserInputResult res = this.CurrentNode.OnTextChangingBubble(this.GetEditorState(), e.Text, this.CurrentNode);
            res.NeedsGlobalEditorRefresh = true;

            if (res.EventHandled)
            {
                e.Handled = true;
            }

            HandleUserInputResult(res);
        }

        public void SetRoot(Node root)
        {
            this.Root = root;
            this.CurrentNode = root;
            this.CurrentSelectionStart = 0;
            this.CurrentSelectionEnd = 0;
        }

        public void ConsoleLog(string msg)
        {
            // await window.Console.Log(msg);
        }

        private void RefreshCompletions()
        {
            this.Root.CreateView(this.GetEditorState());

            List<AstAutocompleteItem> completions = this.GetCompletions();
            List<CompletionItem> completionItems = completions.Select(x => new CompletionItem(
                x.MenuText,
                x.DocTitle + " " + x.DocText,
                completionAction: (z, y, c) => 
                {
                    Node n = x.TriggerItemSelected();
                    this.Root.CreateView(this.GetEditorState());
                    this.RefreshWholeEditor(UserInputResult.HandledNeedsGlobalRefresh(changeFocusToNode: n, caretDelta: n.View.Text.Length));
                })).ToList();
            
            this.Editor.Completion.ExternalCompletions.Clear();
            this.Editor.Completion.ExternalCompletions.AddRange(completionItems);
        }

        public void HandleUserInputResult(UserInputResult res)
        {
            if (res.NeedsGlobalEditorRefresh)
            {
                if (this._refreshing)
                {
                    return;
                }

                this.Root.CreateView(this.GetEditorState());

                Dispatcher.UIThread.Post(() =>
                {
                    this.RefreshWholeEditor(res);
                });
            }
        }

        public void RefreshWholeEditor(UserInputResult res)
        {
            if (this._refreshing)
            {
                return;
            }

            {
                this._refreshing = true;
                this._visibleNodesIntervalTree.Clear();
                this.VisibleNodesList.Clear();

                List<(string text, PositionInfo info, VisualStyle style, VisualStyle backgroundStyle, VisualStyle overlayStyle)> renderInfo = new List<(string text, PositionInfo info, VisualStyle style, VisualStyle backgroundStyle, VisualStyle overlayStyle)>(1024);
                int addedLength = 0;


                RenderViewsRecusive(this.Root, renderInfo, ref addedLength);

                StringBuilder sb = new StringBuilder();
                List<VisualStyle> decors = new List<VisualStyle>(renderInfo.Count);
                int line = 1;
                int colInLine = 1;
                this.Editor.SyntaxHighlighter.ExternalStyles.Clear();
                foreach (var r in renderInfo)
                {
                    sb.Append(r.text);
                    int newLines = r.text.Count(x => x == '\n');
                    int startLine = line;
                    int startCol = colInLine;

                    // OBS: cannot use TextModel.GetPositionAt here, because we still have the previous text
                    if (newLines > 0)
                    {
                        line += newLines;
                        colInLine = 1;
                    }

                    if (r.text != "\r\n")
                    {
                        colInLine += r.text.Length;
                        this.Editor.SyntaxHighlighter.ExternalStyles.Add((r.info.StartOffset, r.info.EndOffset, r.style));
                    }
                }

                TextLocation oldPos = this.CurrentPosition.Location;
                Node oldNode = this.CurrentNode;

                {
                    string s = sb.ToString();
                    this.Editor.EditorControl.Text = s;
                }

                this._refreshing = false;

                {
                    bool cursorManipulated = false;

                    if (res.ChangeFocusToNode != null)
                    {
                        oldPos = this.GetPositionAt(res.ChangeFocusToNode.PositionInfo.StartOffset);
                    }
                    
                    if (res.CaretDelta != null)
                    {
                        oldPos = this.GetPositionAt(this.Document.GetOffset(oldPos) + res.CaretDelta.Value);
                    }

                    SetAndRevealPosition(oldPos);
                }
            }
        }

        private Selection GetSelection()
        {
            return this.TextArea.Selection;
        }

        private void RenderViewsRecusive(Node node, List<(string text, PositionInfo info, VisualStyle style, VisualStyle backgroundStyle, VisualStyle overlayStyle)> res, ref int addedLength)
        {
            if (node.View != null && (node.VisualChildren?.Count ?? 0) == 0)
            {
                node.PositionInfo = new PositionInfo { StartOffset = addedLength, EndOffset = addedLength + node.View.Text.Length};
                res.Add((node.View.Text, node.PositionInfo, node.View.Style, node.View.BackgroundStyle, node.View.OverlayStyle));
                addedLength += node.View.Text.Length;
                this._visibleNodesIntervalTree.Add(node.PositionInfo.StartOffset, node.PositionInfo.EndOffset, (this.VisibleNodesList.Count, node));
                this.VisibleNodesList.Add(node);
            }
            else if (node.View == null && node.VisualChildren?.Count > 0)
            {
                int childrenStart = addedLength;
                foreach (Node n in node.VisualChildren)
                {
                    RenderViewsRecusive(n, res, ref addedLength);
                }

                node.PositionInfo = new PositionInfo { StartOffset = childrenStart, EndOffset = addedLength};
            }
            else
            {
                throw new InvalidOperationException($"Either View or Children have to be set on {node.GetType().Name}. viewIsNull={node.View == null}");
            }
        }

        public int ListIndex() => AtPosition(this.CurrentOffset).Item1;

        public List<Node> VisibleNodesList { get; } = new List<Node>(100);

        public Node CurrentNode { get; private set; }

        public TextViewPosition CurrentPosition => this.TextArea.Caret.Position;

        public int CurrentOffset => this.Document.GetOffset(this.CurrentPosition.Location);
        // TODO: this breaks down after text editign
        public List<string> SelectionStyleIds { get; private set; } = new List<string>(2);
        public int CurrentSelectionStart { get; private set; }
        public int CurrentSelectionEnd { get; private set; }

        public (int VisibleNodesListIndex, Node) AtPosition(int position)
        {
            (int, Node)[] res = this._visibleNodesIntervalTree.Query(position + 1).ToArray();

            if (res.Length == 0)
            {
                return (0, null);
            }

            if (res.Length == 1)
            {
                return res[0];
            }
            
            if (res.Length > 2)
            {
                throw new InvalidOperationException("res.Length>2 = " + res.Length);
            }

            return res[0].Item1 < res[1].Item1 ? res[0] : res[1];
        }

        public void OnKeyUp(object s, KeyEventArgs e)
        {
            ConsoleLog("OnKeyUp");
            Node currNode = this.CurrentNode;
            UserInputResult res = currNode.OnKeyUpBubble(this.GetEditorState(), e, currNode);
            this.HandleUserInputResult(res);
        }

        private void Select(int position)
        {
            TextLocation newPos = this.GetPositionAt(position);
            SetAndRevealPosition(newPos);
        }

        private void SetAndRevealPosition(TextLocation position)
        {
            TextArea.Caret.Location = position;
            TextArea.Caret.BringCaretToView();
        }

        public Node GetParent(Node node, bool jumpHole)
        {
            Node parent = node?.Parent;
            if (parent == null)
            {
                return null;
            }

            if (jumpHole && parent.GetType().IsGenericType && parent.GetType().GetGenericTypeDefinition() == typeof(HoleNode<>))
            {
                parent = parent.Parent;
            }

            return parent;
        }

        public void OnPositionChanged(object? sender, EventArgs e)
        {
            // TODO source = deleteLeft
            if (this._refreshing)
            {
                return;
            }

            this.Editor.SyntaxHighlighter.ExternalSelectionStyles.Clear();

            // TODO source != api and != mouse
            // if (e.Source != "api" && e.Source != "mouse")
            // {
            //     this.SetAndRevealPosition(this.CurrentPosition);
            //     return;
            // }
            // 
            // TODO: consider skipping if position unchanged

            {
                Selection s = this.GetSelection();
                this.CurrentSelectionStart = this.Document.GetOffset(s.IsEmpty ? this.CurrentPosition.Location : s.StartPosition.Location);
                this.CurrentSelectionEnd = this.Document.GetOffset(s.IsEmpty ? this.CurrentPosition.Location : s.EndPosition.Location);

                Node current = this.AtPosition(this.CurrentOffset).Item2;
                ConsoleLog("CO" + this.CurrentOffset);
                if (current?.PositionInfo == null)
                {
                    return;
                }

                if (current.Unselectable)
                {
                    // TODO: next node?
                }

                if (this.CurrentNode != current)
                {
                    EditorState es = this.GetEditorState();
                    UserInputResult? res1 = this.CurrentNode?.OnNodeIsSelectedBubble(es, false, this.CurrentNode);
                    UserInputResult res2 = current.OnNodeIsSelectedBubble(es, true, current);

                    if (res1.HasValue)
                    {
                        this.HandleUserInputResult(res1.Value);
                    }

                    this.HandleUserInputResult(res2);
                }

                this.CurrentNode = current;

                this.AddSelectionStyle(this.CurrentNode.PositionInfo.StartOffset, this.CurrentNode.PositionInfo.EndOffset, VisualStyles.SelectedNodeText, null);
            }
        }

        private void SelectionChanged(object? sender, EventArgs e)
        {
            Selection s = this.GetSelection();
            this.CurrentSelectionStart = this.Document.GetOffset(s.IsEmpty ? this.CurrentPosition.Location : s.StartPosition.Location);
            this.CurrentSelectionEnd = this.Document.GetOffset(s.IsEmpty ? this.CurrentPosition.Location : s.EndPosition.Location);
        }

        private TextLocation GetPositionAt(int offset)
        {
            return this.Document.GetLocation(offset);
        }

        private void AddSelectionStyle(int min, int max, VisualStyle style, string hoverMessage)
        {
            this.Editor.SyntaxHighlighter.ExternalSelectionStyles.Add((min, max, style));
        }

        public EditorState GetEditorState(int offsetAdjustment = 0)
        {
            return new EditorState(this.CurrentOffset + offsetAdjustment, this.CurrentSelectionStart + offsetAdjustment, this.CurrentSelectionEnd + offsetAdjustment, this.FactoryRegistry, this.VisibleNodesList);
        }

        public List<AstAutocompleteItem> GetCompletions()
        {
            List<AstAutocompleteItem> completions = this.CurrentNode.GetCustomCompletions(this.GetEditorState());
            return completions;
        }
    }
}
