using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using IntervalTree;
using ProjectionalBlazorMonaco;
using Sharplog.KME;
using System;
using System.Text;
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

        /*private void OnModelContentChanged(ModelContentChangedEvent eventArg)
        {
            // TODO
            // Use this instead of overriding commands for letters.
            // RangeLength = 0 for inserts. <>0 for deletes
            // await this._astEditor.OnTextTyped(eventArg);
        }*/

        private void ForceRefresh()
        {
            this.RefreshWholeEditor(UserInputResult.HandledNeedsGlobalRefresh());
        }

        public readonly CodeEditor _monacoEditor;

        private long _debugId = 0;

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
            this._monacoEditor = monacoEditor;
        }

        public void Init()
        {
            this._monacoEditor.EditorControl.TextArea.Caret.PositionChanged += OnPositionChanged;
            this._monacoEditor.EditorControl.AddHandler(TextEditor.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            this._monacoEditor.EditorControl.KeyUp += this.OnKeyUp;
            this._monacoEditor.EditorControl.PointerReleased += this.OnMouseDown;
            this._monacoEditor.EditorControl.Document.Changing += Document_Changing;
        }

        private void Document_Changing(object? sender, DocumentChangeEventArgs e)
        {
            if (this._refreshing || e == null)
            {
                return;
            }

            bool isDel = e.RemovalLength != 0;
            string text = (isDel ? new string('\b', e.RemovalLength) : string.Empty) + e.InsertedText.Text;
            
            // TODO: multi select
            UserInputResult res = this.CurrentNode.OnTextChangingBubble(this.GetEditorState(isDel && e.Offset == this.CurrentOffset && e.RemovalLength == 1 ? 1 : 0), text, this.CurrentNode);
            res.NeedsGlobalEditorRefresh = true;
            Dispatcher.UIThread.Post(() => HandleUserInputResult(res));
        }

        public void SetRoot(Node root)
        {
            this.Root = root;
            this.CurrentNode = root;
            this.CurrentPosition = new TextViewPosition(1, 1);
            this.CurrentSelectionStart = 0;
            this.CurrentSelectionEnd = 0;
        }

        public void ConsoleLog(string msg)
        {
            // await window.Console.Log(msg);
        }

        public void OnMouseDown(object s, PointerReleasedEventArgs e)
        {
            TextViewPosition? pos = this._monacoEditor.EditorControl.GetPositionFromPoint(e.GetPosition(this._monacoEditor.EditorControl.TextArea));
            if (pos is null)
            {
                return;
            }

            ConsoleLog("OnMouseDown");

            (int, Node) node = this.AtPosition(this._monacoEditor.EditorControl.Document.GetOffset(pos.Value.Location));
            if (node.Item2 != null)
            {
                UserInputResult res = node.Item2.OnMouseClickBubble(this.GetEditorState(), e, node.Item2);
                this.HandleUserInputResult(res);
            }
        }

        private void RefreshCompletions()
        {
            List<AstAutocompleteItem> completions = this.GetCompletions();
            List<object> jsCompletions = new List<object>(completions.Count);
            foreach (AstAutocompleteItem c in completions)
            {

                // Schema: https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.completionitem.html
                dynamic compl = new System.Dynamic.ExpandoObject();
                compl.insertText = c.Id;
                compl.filterText = c.SourceToMatch;
                compl.label = c.MenuText;
                compl.documentation = c.DocTitle + " " + c.DocText;
                compl.kind = 1;
                compl.detail = c.Id;

                jsCompletions.Add(compl);
            }

            // TODO: set the completions
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
                this.RefreshWholeEditor(res);
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

                List<(string text, PositionInfo info, TextDecoration style, TextDecoration backgroundStyle, TextDecoration overlayStyle)> renderInfo = new List<(string text, PositionInfo info, TextDecoration style, TextDecoration backgroundStyle, TextDecoration overlayStyle)>(1024);
                int addedLength = 0;


                RenderViewsRecusive(this.Root, renderInfo, ref addedLength);

                StringBuilder sb = new StringBuilder();
                List<TextDecoration> decors = new List<TextDecoration>(renderInfo.Count);
                int line = 1;
                int colInLine = 1;
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
                        // TextDecoration d = new TextDecoration
                        // {
                        //     Range = new BlazorMonaco.Range { StartColumn = startCol, StartLineNumber = startLine, EndColumn = colInLine, EndLineNumber = line },
                        //     Options = new TextDecoration
                        //     {
                        //         InlineClassName = r.style?.InlineClassName ?? "decorationPointer",
                        //         HoverMessage = new[] { new MarkdownString { Value = r.info.StartOffset + "-" + r.info.EndOffset } },
                        //         Minimap = new ModelDecorationMinimapOptions { Color = "red" },
                        //         OverviewRuler = new ModelDecorationOverviewRulerOptions { Color = "blue" }
                        //     }
                        // };
                        // decors.Add(d);
                    }
                }

                TextViewPosition oldPos = this.CurrentPosition;
                Node oldNode = this.CurrentNode;

                {
                    this._monacoEditor.EditorControl.Text = sb.ToString();
                }

                // {
                //     _ = _monacoEditor.DeltaDecorations(null, decors.ToArray());
                // }

                this._refreshing = false;

                {
                    if (res.NewLocalPosition.HasValue)
                    {
                        TextLocation newPos = this.GetPositionAt(res.NewLocalPositionContext.PositionInfo.StartOffset + res.NewLocalPosition.Value);
                        if (newPos != null)
                        {
                            SetAndRevealPosition(newPos);
                        }
                    }
                    else if (res.ChangeFocusToNode != null)
                    {
                        TextLocation newPos = this.GetPositionAt(res.ChangeFocusToNode.PositionInfo.StartOffset);
                        if (newPos != null)
                        {
                            SetAndRevealPosition(newPos);
                        }
                    }
                    else
                    {
                        SetAndRevealPosition(oldPos.Location);
                    }
                }
            }
        }

        private Selection GetSelection()
        {
            return this._monacoEditor.EditorControl.TextArea.Selection;
        }

        private void RenderViewsRecusive(Node node, List<(string text, PositionInfo info, TextDecoration style, TextDecoration backgroundStyle, TextDecoration overlayStyle)> res, ref int addedLength)
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

        public Dictionary<string, AstAutocompleteItem> LastCompletionsById { get; private set; } = new Dictionary<string, AstAutocompleteItem>();

        public TextViewPosition CurrentPosition { get; private set; } = new TextViewPosition(1, 1);

        public int CurrentOffset => this._monacoEditor.EditorControl.Document.GetOffset(this.CurrentPosition.Location);
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

        private void PopupMenu_ClosedWithouSelection(object sender, EventArgs e)
        {
            this.HandleUserInputResult(UserInputResult.HandledNeedsGlobalRefresh());
        }


        public void OnAstCompletionSelected(string completionId)
        {
            ConsoleLog("OnAstCompletionSelected" + completionId);
            AstAutocompleteItem ai = this.LastCompletionsById[completionId];
            ai.TriggerItemSelected();
            this.HandleUserInputResult(UserInputResult.HandledNeedsGlobalRefresh());
        }

        public void OnKeyUp(object s, KeyEventArgs e)
        {
            ConsoleLog("OnKeyUp");
            Node currNode = this.CurrentNode;
            UserInputResult res = currNode.OnKeyUpBubble(this.GetEditorState(), e, currNode);
            this.HandleUserInputResult(res);
        }

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (this._refreshing)
            {
                return;
            }

            ConsoleLog("OnKeyDown");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Right)
            {
                int listIndex = this.ListIndex();
                if (listIndex < this.VisibleNodesList.Count - 1)
                {
                    int newPosition = this.VisibleNodesList[listIndex + 1].PositionInfo.StartOffset;
                    this.Select(newPosition);
                    e.Handled = true;
                }                
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Left)
            {
                int listIndex = this.ListIndex();
                int newPosition = this.VisibleNodesList[Math.Max(0, listIndex - 1)].PositionInfo.EndOffset;
                this.Select(newPosition);
                e.Handled = true;
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key== Key.Up)
            {
                Node parent = this.CurrentNode.Parent;
                int newPosition = parent.PositionInfo.StartOffset;
                this.Select(newPosition);
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                this.HandleUserInputResult(UserInputResult.HandledNeedsGlobalRefresh());
            }
            else
            {
                Node currNode = this.CurrentNode;
                
                UserInputResult res = currNode.OnKeyDownBubble(this.GetEditorState(), e, currNode);
                this.HandleUserInputResult(res);
            }
        }

        private void Select(int position)
        {
            TextLocation newPos = this.GetPositionAt(position);
            SetAndRevealPosition(newPos);
        }

        private void SetAndRevealPosition(TextLocation position)
        {
            _monacoEditor.EditorControl.TextArea.Caret.Location = position;
            _monacoEditor.EditorControl.ScrollTo(position.Line, position.Column);
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

            // TODO source != api and != mouse
            // if (e.Source != "api" && e.Source != "mouse")
            // {
            //     this.SetAndRevealPosition(this.CurrentPosition);
            //     return;
            // }
            // 
            // TODO: consider skipping if position unchanged

            {
                // await using (await window.Console.Time("GetOffsets"))
                {
                    this.CurrentPosition = this._monacoEditor.EditorControl.TextArea.Caret.Position;
                    Selection s = this.GetSelection();
                    this.CurrentSelectionStart =  this._monacoEditor.EditorControl.Document.GetOffset(s.IsEmpty ? this.CurrentPosition.Location : s.StartPosition.Location);
                    this.CurrentSelectionEnd = this._monacoEditor.EditorControl.Document.GetOffset(s.IsEmpty ? this.CurrentPosition.Location : s.EndPosition.Location);
                }

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

                    this.SetStyleForRange(min, max, Styles.SelectedParentNodeText, null);
                }

                this.SetStyleForRange(this.CurrentNode.PositionInfo.StartOffset, this.CurrentNode.PositionInfo.EndOffset, Styles.SelectedNodeText, null);
            }
        }

        private TextLocation GetPositionAt(int offset)
        {
            return this._monacoEditor.EditorControl.Document.GetLocation(offset);
        }

        private void SetStyleForRange(int min, int max, TextDecoration selectedParentNodeText, string hoverMessage)
        {
            // List<TextDecoration> decors = new List<TextDecoration>(1);
            // TextLocation startPos = this.GetPositionAt(min);
            // TextLocation endPos = this.GetPositionAt(max);
            // TextDecoration d = new TextDecoration
            // {
            //     Range = new BlazorMonaco.Range { StartColumn = startPos.Column, StartLineNumber = startPos.LineNumber, EndColumn = endPos.Column, EndLineNumber = endPos.LineNumber },
            //     Options = new TextDecoration
            //     {
            //         InlineClassName = selectedParentNodeText.InlineClassName,
            //         Minimap = new ModelDecorationMinimapOptions { Color = "red" },
            //         OverviewRuler = new ModelDecorationOverviewRulerOptions { Color = "blue" }
            //     }
            // };
            // 
            // if (hoverMessage != null)
            // {
            //     d.Options.HoverMessage = new[] { new MarkdownString { Value = hoverMessage } };
            // }
            // 
            // decors.Add(d);
            // 
            // _monacoEditor.DeltaDecorations(null, decors.ToArray());
        }

        public EditorState GetEditorState(int offsetAdjustment = 0)
        {
            return new EditorState(this.CurrentOffset + offsetAdjustment, this.CurrentSelectionStart + offsetAdjustment, this.CurrentSelectionEnd + offsetAdjustment, this.FactoryRegistry, this.VisibleNodesList);
        }

        public List<AstAutocompleteItem> GetCompletions()
        {
            List<AstAutocompleteItem> completions = this.CurrentNode.GetCustomCompletions(this.GetEditorState());
            this.LastCompletionsById = completions.ToDictionary(x => x.Id);
            return completions;
        }
    }
}
