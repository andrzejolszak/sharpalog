using System;
using System.Collections.Generic;

namespace Ast2
{
    public class ListNode<T> : Node where T:Node
    {
        public ListNode(string leftParen, string rightParen, string separator, List<Node> list = null)
        {
            this.LeftParen = leftParen;
            this.RightParen = rightParen;
            this.Separator = separator;
            this.List = list ?? new List<Node>();
            this.VisualChildren = new List<Node>();
            this.EmptyPlaceholder = new HoleNode<T>() { Parent = this } ;
            this.EmptyPlaceholder.ValueChanged += EmptyPlaceholder_ValueChanged;
        }

        public Type TargetNodeType { get; } = typeof(T);
        public List<Node> List { get; }
        public HoleNode<T> EmptyPlaceholder { get; }
        public int EmptyPlaceholderIndex { get; private set; } = -1;
        public string LeftParen { get; }
        public string RightParen { get; }
        public string Separator { get; }

        public override void CreateView(EditorState editorState)
        {
            this.VisualChildren.Clear();
            this.VisualChildren.Add(new ReadOnlyTextNode(this.LeftParen) { Parent = this });

            Node lastOrNull = this.List.Count > 0 ? this.List[this.List.Count - 1] : null;
            for (int i = 0; i < this.List.Count; i++)
            {
                if (this.EmptyPlaceholderIndex == i)
                {
                    this.VisualChildren.Add(EmptyPlaceholder);
                    this.VisualChildren.Add(new ReadOnlyTextNode(this.Separator) { Parent = this });
                }

                Node n = this.List[i];
                this.VisualChildren.Add(n);

                if (n != lastOrNull)
                {
                    this.VisualChildren.Add(new ReadOnlyTextNode(this.Separator) { Parent = this });
                }
            }

            if (this.EmptyPlaceholderIndex == this.List.Count)
            {
                if (this.List.Count > 0)
                {
                    this.VisualChildren.Add(new ReadOnlyTextNode(this.Separator) { Parent = this });
                }

                this.VisualChildren.Add(EmptyPlaceholder);
            }

            this.VisualChildren.Add(new ReadOnlyTextNode(this.RightParen) { Parent = this });
            
            base.CreateView(editorState);
        }

        public void InsertAt(Node existing, Node node)
        {
            if (existing == null)
            {
                this.List.Add(node);
            }
            else
            {
                int index = this.List.IndexOf(existing);
                this.List.Insert(index, node);
            }

            this.EmptyPlaceholderIndex = -1;
        }

        public UserInputResult Remove(Node target)
        {
            if (target == this.EmptyPlaceholder && this.EmptyPlaceholderIndex > -1)
            {
                int index = this.EmptyPlaceholderIndex;
                this.EmptyPlaceholderIndex = -1;
                return UserInputResult.HandledNeedsGlobalRefresh(this.List.Count > 0 ? this.List[Math.Min(this.List.Count - 1, index)] : this, caretDelta: this.List.Count > 0 && this.List.Count == index ? this.List.Last().View.Text.Length : 0);
            }

            if (this.List.Count == 0)
            {
                return UserInputResult.HandledNeedsGlobalRefresh();
            }

            int idx = this.List.IndexOf(target);
            if (idx < 0)
            {
                idx = this.VisualChildren.IndexOf(target);
                if (idx > -1)
                {
                    Node t = this.VisualChildren[idx];
                    if (t.View.Text == this.Separator || t.View.Text == this.RightParen)
                    {
                        t = this.VisualChildren[idx - 1];
                    }

                    idx = this.List.IndexOf(t);
                }
            }

            if (idx < 0)
            {
                return UserInputResult.HandledNeedsGlobalRefresh();
            }

            this.List.RemoveAt(idx);
            return UserInputResult.HandledNeedsGlobalRefresh(this.List.Count > 0 ? this.List[Math.Min(this.List.Count - 1, idx)] : this, caretDelta: this.List.Count > 0 ? 0 : 1);
        }

        protected override UserInputResult OnKeyDown(EditorState state, KeyEventArgs keys, Node target)
        {
            if (keys.Key == Key.Enter)
            {
                if (AddEmptyPlaceholder(target, out UserInputResult res))
                {
                    return res;
                }
            }
            else if (keys.Key == Key.Escape && target == this.EmptyPlaceholder)
            {
                return this.Remove(target);
            }
            else if (keys.Key == Key.Back && target != this.EmptyPlaceholder && this.List.Count > 0)
            {
                return this.Remove(target);
            }

            return base.OnKeyDown(state, keys, target);
        }

        protected override UserInputResult OnTextChanging(EditorState state, string insertingText, Node target)
        {
            if (insertingText.Length == 1 && this.Separator.Contains(insertingText[0]))
            {
                if (AddEmptyPlaceholder(target, out UserInputResult res))
                {
                    return res;
                }
            }

            return base.OnTextChanging(state, insertingText, target);
        }

        private bool AddEmptyPlaceholder(Node target, out UserInputResult res)
        {
            if (target is ReadOnlyTextNode && !this.List.Contains(target))
            {
                int idx = this.VisualChildren.IndexOf(target);
                if (idx == this.VisualChildren.Count - 1)
                {
                    this.EmptyPlaceholderIndex = this.List.Count;
                }
                else
                {
                    this.EmptyPlaceholderIndex = this.List.Count > 0 ? this.List.IndexOf(this.VisualChildren[idx + 1]) : 0;
                }

                res = UserInputResult.HandledNeedsGlobalRefresh(this.EmptyPlaceholder);
                return true;
            }

            res = default;
            return false;
        }

        protected override UserInputResult OnNodeIsSelectedChanged(EditorState state, bool hasFocus, Node target)
        {
            if (target == this.EmptyPlaceholder && !hasFocus)
            {
                return this.Remove(target);
            }

            return base.OnNodeIsSelectedChanged(state, hasFocus, target);
        }

        private void EmptyPlaceholder_ValueChanged(HoleNode<T> obj)
        {
            T val = obj.GetChildOrDefault();
            if (val == null)
            {
                return;
            }

            this.List.Insert(this.EmptyPlaceholderIndex, val);
            this.EmptyPlaceholderIndex = -1;
            this.EmptyPlaceholder.SetChild(null);
        }
    }
}