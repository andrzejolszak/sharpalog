﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharplog.KME;

namespace Ast2
{
    public class ReferenceNode<T> : Node
        where T : Node
    {
        public ReferenceNode(T target)
        {
            this.Target = target;
            this.Style = VisualStyles.NormalTextBlueBackgroundStyle;

            this.Target.ViewChangedEvent += () =>
            {
                this.View = new NodeView(this, this.Target.View?.Text ?? this.TargetDetachedText, this.Style);
            };
        }

        public T Target { get; }

        public VisualStyle Style { get; set; }

        public string TargetDetachedText { get; set; } = "<null_ref>";

        private bool IsTargetAttached(EditorState editorState) => editorState.VisibleNodesList.Contains(this.Target) && this.Target.View != null;

        public override void CreateView(EditorState editorState)
        {
            if (!IsTargetAttached(editorState))
            {
                // Target detached
                this.View = new NodeView(this, this.TargetDetachedText, this.Style);
            }
            else
            {
                this.View = new NodeView(this, this.Target.View.Text, this.Style, overlayStyle: VisualStyles.UnderlineStyle);
            }
        }

        public override UserInputResult OnMouseClick(EditorState state, PointerPressedEventArgs e, Node target)
        {
            // TODO left mouse button
            if (e.ClickCount == 1 && e.KeyModifiers.HasFlag(KeyModifiers.Control) && IsTargetAttached(state))
            {
                return UserInputResult.HandledNeedsGlobalRefresh(this.Target);
            }

            return base.OnMouseClick(state, e, target);
        }

        protected override UserInputResult OnKeyDown(EditorState state, KeyEventArgs keys, Node target)
        {
            if (keys.Key == Key.LeftCtrl)
            {
                return UserInputResult.HandledNeedsGlobalRefresh(cursor: Cursors.Hand);
            }

            return base.OnKeyDown(state, keys, target);
        }

        protected override UserInputResult OnKeyUp(EditorState state, KeyEventArgs keys, Node target)
        {
            if (keys.Key == Key.LeftCtrl)
            {
                return UserInputResult.HandledNeedsGlobalRefresh(cursor: Cursors.Default);
            }

            return base.OnKeyUp(state, keys, target);
        }
    }
}
