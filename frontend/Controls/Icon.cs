using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Frontend.Controls;

/// <summary>
/// Векторна іконка з IconCatalog. Колір береться з Foreground (успадковується від батьківського
/// TextElement, тож у темі змінюється разом із DynamicResource).
/// Використання: &lt;ctl:Icon Kind="Calendar" Size="18" Foreground="{DynamicResource FlMuted}"/&gt;
/// </summary>
public sealed class Icon : FrameworkElement
{
    private static readonly Dictionary<string, Geometry> Cache = new();

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(string), typeof(Icon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(Icon),
        new FrameworkPropertyMetadata(18d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner(typeof(Icon),
            new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    public string? Kind { get => (string?)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public Icon() { IsHitTestVisible = false; SnapsToDevicePixels = false; }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    protected override void OnRender(DrawingContext dc)
    {
        if (Kind is null || !IconCatalog.All.TryGetValue(Kind, out var def)) return;
        if (!Cache.TryGetValue(Kind, out var geometry))
        {
            geometry = Geometry.Parse(def.Data);
            geometry.Freeze();
            Cache[Kind] = geometry;
        }

        var scale = Size / def.View;
        var pen = new Pen(Foreground, def.Stroke)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.DrawGeometry(null, pen, geometry);
        dc.Pop();
    }
}
