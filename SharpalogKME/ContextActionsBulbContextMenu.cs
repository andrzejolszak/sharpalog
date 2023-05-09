using Avalonia.Controls;
using Avalonia.Styling;
using System.Linq;
using Avalonia.Data;
using Avalonia.Controls.Primitives;
using System.Reflection;
using System;
using Avalonia.Data.Converters;

namespace RoslynPad.Editor;

internal class ContextActionsBulbContextMenu : ContextMenu, IStyleable
{
    private bool _opened;

    Type IStyleable.StyleKey => typeof(ContextMenu);

    public new void Open(Control control)
    {
        base.Open(control);

        // workaroud for Avalonia's lack of placement option
        if (!_opened)
        {
            _opened = true;

            if (typeof(ContextMenu).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this) is Popup popup)
            {
                popup.PlacementMode = PlacementMode.Right;
            }

            base.Close();
            base.Open(control);
        }
    }
}