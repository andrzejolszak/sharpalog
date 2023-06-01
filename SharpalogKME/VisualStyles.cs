﻿using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Utils;

namespace Sharplog.KME;

public static class VisualStyles
{
    // TODO
    public static readonly VisualStyle NormalTextStyle = new VisualStyle();

    public static readonly VisualStyle NormalTextGrayBackgroundStyle = new VisualStyle().SetBackgroundBrush(Brushes.LightGray);

    public static readonly VisualStyle NormalTextRedBackgroundStyle = new VisualStyle().SetBackgroundBrush(Brushes.LightSalmon);

    public static readonly VisualStyle NormalTextBlueBackgroundStyle = new VisualStyle().SetBackgroundBrush(Brushes.LightSteelBlue);

    public static readonly VisualStyle GrayTextStyle = new VisualStyle().SetForegroundBrush(Brushes.DarkGray);

    public static readonly VisualStyle Scratch;

    public static readonly VisualStyle UnderlineStyle = new VisualStyle().SetTextDecorations(CustomTextDecorations.Underline);

    public static readonly VisualStyle SelectedParentNodeText = new VisualStyle().SetTextDecorations(CustomTextDecorations.Underline);

    public static readonly VisualStyle SelectedNodeText = new VisualStyle().SetTextDecorations(CustomTextDecorations.Underline);

    public static readonly VisualStyle InvisibleCharsStyle;
}

public class VisualStyle : TextRunProperties
{
    private IBrush _backgroundBrush;
    private BaselineAlignment _baselineAlignment;
    private CultureInfo _cultureInfo;
    private double _fontRenderingEmSize;
    private IBrush _foregroundBrush;
    private Typeface _typeface;
    private TextDecorationCollection _textDecorations = new TextDecorationCollection();

    public override IBrush BackgroundBrush => _backgroundBrush;

    public IBrush CodeMapBrush { get; set; }

    public VisualStyle SetBackgroundBrush(IBrush value)
    {
        _backgroundBrush = value?.ToImmutable();
        return this;
    }

    public override BaselineAlignment BaselineAlignment => _baselineAlignment;

    public VisualStyle SetBaselineAlignment(BaselineAlignment value)
    {
        _baselineAlignment = value;
        return this;
    }

    public override CultureInfo CultureInfo => _cultureInfo;

    public VisualStyle SetCultureInfo(CultureInfo value)
    {
        _cultureInfo = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public override double FontRenderingEmSize => _fontRenderingEmSize;

    public VisualStyle SetFontRenderingEmSize(double value)
    {
        _fontRenderingEmSize = value;
        return this;
    }

    public override IBrush ForegroundBrush => _foregroundBrush;

    public VisualStyle SetForegroundBrush(IBrush value)
    {
        _foregroundBrush = value?.ToImmutable();
        return this;
    }

    public override Typeface Typeface => _typeface;

    public VisualStyle SetTypeface(Typeface value)
    {
        _typeface = value;
        return this;
    }

    public override TextDecorationCollection TextDecorations => _textDecorations;

    public VisualStyle SetTextDecorations(TextDecorationCollection value)
    {
        ExtensionMethods.CheckIsFrozen(value);
        if (_textDecorations == null)
            _textDecorations = value;
        else
            _textDecorations = new TextDecorationCollection(_textDecorations.Union(value));

        return this;
    }
}

/// <summary>
/// Defines a set of commonly used text decorations.
/// </summary>
public static class CustomTextDecorations
{
    static CustomTextDecorations()
    {
        Underline = new TextDecorationCollection
                        {
                            new TextDecoration
                            {
                                Location = Avalonia.Media.TextDecorationLocation.Underline
                            }
                        };

        Strikethrough = new TextDecorationCollection
                            {
                                new TextDecoration
                                {
                                    Location = Avalonia.Media.TextDecorationLocation.Strikethrough
                                }
                            };

        SquiggleUnderline = new TextDecorationCollection
                       {
                           new TextDecoration
                           {
                               Location = Avalonia.Media.TextDecorationLocation.Underline,
                               StrokeDashArray = new AvaloniaList<double>{1, 1},
                               Stroke = Brushes.Red,
                               StrokeThickness = 3,
                               StrokeThicknessUnit = TextDecorationUnit.Pixel,
                               StrokeOffsetUnit = TextDecorationUnit.Pixel,
                               StrokeOffset = 4
                           }
                       };

        Baseline = new TextDecorationCollection
                       {
                           new TextDecoration
                           {
                               Location = Avalonia.Media.TextDecorationLocation.Baseline
                           }
                       };
    }

    /// <summary>
    /// Gets a <see cref="TextDecorationCollection"/> containing an underline.
    /// </summary>
    public static TextDecorationCollection Underline { get; }

    /// <summary>
    /// Gets a <see cref="TextDecorationCollection"/> containing a strikethrough.
    /// </summary>
    public static TextDecorationCollection Strikethrough { get; }

    /// <summary>
    /// Gets a <see cref="TextDecorationCollection"/> containing an overline.
    /// </summary>
    public static TextDecorationCollection SquiggleUnderline { get; }

    /// <summary>
    /// Gets a <see cref="TextDecorationCollection"/> containing a baseline.
    /// </summary>
    public static TextDecorationCollection Baseline { get; }
}