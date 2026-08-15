using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace FlowRate.Controls;

/// <summary>
/// A lightweight, dependency-free line chart that plots throughput (Mbps) over interval time.
/// Bind <see cref="Values"/> to an observable collection of per-interval Mbps readings; the
/// chart redraws as points are added and on resize. Intentionally custom-drawn to match the
/// hand-built <see cref="SpeedometerGauge"/> and avoid external charting dependencies.
/// </summary>
public sealed partial class ThroughputChart : UserControl
{
    private static readonly Color LineColor = Color.FromArgb(0xFF, 0x2E, 0xC4, 0xB6);
    private static readonly Color FillColor = Color.FromArgb(0x33, 0x2E, 0xC4, 0xB6);
    private static readonly Color AxisColor = Color.FromArgb(0x33, 0x80, 0x80, 0x80);
    private static readonly Color TextColor = Color.FromArgb(0x99, 0x80, 0x80, 0x80);

    private const double PaddingLeft = 44;
    private const double PaddingRight = 12;
    private const double PaddingTop = 12;
    private const double PaddingBottom = 22;

    public ThroughputChart()
    {
        InitializeComponent();
    }

    /// <summary>Per-interval throughput readings in Mbps, in chronological order.</summary>
    public IReadOnlyList<double> Values
    {
        get => (IReadOnlyList<double>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(IReadOnlyList<double>), typeof(ThroughputChart),
            new PropertyMetadata(null, OnValuesChanged));

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (ThroughputChart)d;

        if (e.OldValue is INotifyCollectionChanged oldObservable)
            oldObservable.CollectionChanged -= chart.OnValuesCollectionChanged;

        if (e.NewValue is INotifyCollectionChanged newObservable)
            newObservable.CollectionChanged += chart.OnValuesCollectionChanged;

        chart.Redraw();
    }

    private void OnValuesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        var canvas = ChartCanvas;
        canvas.Children.Clear();

        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        var plotWidth = width - PaddingLeft - PaddingRight;
        var plotHeight = height - PaddingTop - PaddingBottom;
        if (plotWidth <= 0 || plotHeight <= 0)
            return;

        // Axes.
        DrawLine(PaddingLeft, PaddingTop, PaddingLeft, PaddingTop + plotHeight, AxisColor, 1);
        DrawLine(PaddingLeft, PaddingTop + plotHeight, PaddingLeft + plotWidth, PaddingTop + plotHeight, AxisColor, 1);

        var values = Values;
        if (values is null || values.Count == 0)
            return;

        var maxValue = values.Max();
        if (maxValue <= 0)
            maxValue = 1;
        var niceMax = NiceCeiling(maxValue);

        // Horizontal gridlines + y labels (0, mid, max).
        for (var i = 0; i <= 2; i++)
        {
            var frac = i / 2.0;
            var y = PaddingTop + plotHeight - (frac * plotHeight);
            DrawLine(PaddingLeft, y, PaddingLeft + plotWidth, y, AxisColor, 0.5);
            AddLabel($"{niceMax * frac:F0}", 2, y - 8, 40, TextAlignment.Right);
        }

        // Build the throughput polyline.
        var count = values.Count;
        var stepX = count > 1 ? plotWidth / (count - 1) : 0;

        var points = new PointCollection();
        for (var i = 0; i < count; i++)
        {
            var x = PaddingLeft + (i * stepX);
            var y = PaddingTop + plotHeight - (values[i] / niceMax * plotHeight);
            points.Add(new Point(x, y));
        }

        // Filled area under the curve.
        if (count > 1)
        {
            var fill = new PointCollection();
            foreach (var p in points)
                fill.Add(p);
            fill.Add(new Point(PaddingLeft + plotWidth, PaddingTop + plotHeight));
            fill.Add(new Point(PaddingLeft, PaddingTop + plotHeight));

            canvas.Children.Add(new Polygon
            {
                Points = fill,
                Fill = new SolidColorBrush(FillColor),
            });
        }

        // The line itself.
        canvas.Children.Add(new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(LineColor),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
        });

        // X-axis label.
        AddLabel($"{count} interval{(count == 1 ? "" : "s")}",
            PaddingLeft, PaddingTop + plotHeight + 4, plotWidth, TextAlignment.Center);
    }

    private void DrawLine(double x1, double y1, double x2, double y2, Color color, double thickness)
    {
        ChartCanvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
        });
    }

    private void AddLabel(string text, double x, double y, double width, TextAlignment alignment)
    {
        var label = new TextBlock
        {
            Text = text,
            FontSize = 11,
            Width = width,
            TextAlignment = alignment,
            Foreground = new SolidColorBrush(TextColor),
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        ChartCanvas.Children.Add(label);
    }

    private static double NiceCeiling(double value)
    {
        if (value <= 0)
            return 1;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        var normalized = value / magnitude;
        double niceNormalized = normalized <= 1 ? 1
            : normalized <= 2 ? 2
            : normalized <= 5 ? 5
            : 10;
        return niceNormalized * magnitude;
    }
}
