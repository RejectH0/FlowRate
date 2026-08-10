using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace FlowRate.Controls;

/// <summary>
/// A custom-drawn, speedtest.net-style radial speedometer gauge.
/// The needle tracks <see cref="Value"/> (color-graded by speed), while a
/// secondary marker shows <see cref="Average"/>. The scale runs 0..<see cref="Maximum"/>.
/// </summary>
public sealed partial class SpeedometerGauge : UserControl
{
    // The dial sweeps 270 degrees: from 135 deg (lower-left) clockwise to 405 deg (lower-right).
    private const double StartAngle = 135.0;
    private const double SweepAngle = 270.0;
    private const int MajorTickCount = 10;

    private Line? _needle;
    private RotateTransform? _needleRotate;
    private Line? _averageMarker;
    private Path? _progressArc;
    private TextBlock? _valueText;
    private TextBlock? _unitText;

    private double _centerX;
    private double _centerY;
    private double _radius;

    public SpeedometerGauge()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>Current throughput value the needle points to.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(SpeedometerGauge),
            new PropertyMetadata(0.0, OnValueChanged));

    /// <summary>Running-average throughput, shown as a secondary marker.</summary>
    public double Average
    {
        get => (double)GetValue(AverageProperty);
        set => SetValue(AverageProperty, value);
    }

    public static readonly DependencyProperty AverageProperty =
        DependencyProperty.Register(
            nameof(Average),
            typeof(double),
            typeof(SpeedometerGauge),
            new PropertyMetadata(0.0, OnValueChanged));

    /// <summary>Full-scale maximum of the dial.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(SpeedometerGauge),
            new PropertyMetadata(100.0, OnValueChanged));

    /// <summary>Unit label shown under the value (e.g., "Mbps").</summary>
    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(
            nameof(Unit),
            typeof(string),
            typeof(SpeedometerGauge),
            new PropertyMetadata("Mbps", OnValueChanged));

    #endregion

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SpeedometerGauge)d).UpdateDynamicElements();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RebuildGauge(e.NewSize);
    }

    /// <summary>
    /// Rebuilds all static and dynamic gauge geometry when the canvas resizes.
    /// </summary>
    private void RebuildGauge(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return;

        GaugeCanvas.Children.Clear();

        _centerX = size.Width / 2.0;
        // Push the center down so the 270-degree open dial fits with the bottom gap.
        _centerY = size.Height * 0.62;
        _radius = Math.Min(size.Width, size.Height) * 0.42;

        DrawTicks();
        DrawTrackArc();
        CreateProgressArc();
        CreateAverageMarker();
        CreateNeedle();
        CreateReadout();

        UpdateDynamicElements();
    }

    /// <summary>Maps a value (0..Maximum) to an absolute dial angle in degrees.</summary>
    private double ValueToAngle(double value)
    {
        var max = Maximum <= 0 ? 1 : Maximum;
        var fraction = Math.Clamp(value / max, 0.0, 1.0);
        return StartAngle + fraction * SweepAngle;
    }

    private Point PointOnDial(double angleDegrees, double radius)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        return new Point(
            _centerX + radius * Math.Cos(rad),
            _centerY + radius * Math.Sin(rad));
    }

    private void DrawTicks()
    {
        var tickBrush = new SolidColorBrush(Color.FromArgb(160, 200, 200, 210));
        for (int i = 0; i <= MajorTickCount; i++)
        {
            var fraction = (double)i / MajorTickCount;
            var angle = StartAngle + fraction * SweepAngle;
            var outer = PointOnDial(angle, _radius);
            var inner = PointOnDial(angle, _radius - 12);

            var tick = new Line
            {
                X1 = inner.X,
                Y1 = inner.Y,
                X2 = outer.X,
                Y2 = outer.Y,
                Stroke = tickBrush,
                StrokeThickness = 2,
            };
            GaugeCanvas.Children.Add(tick);
        }
    }

    private void DrawTrackArc()
    {
        var track = BuildArcPath(0.0, 1.0, _radius);
        track.Stroke = new SolidColorBrush(Color.FromArgb(70, 150, 150, 160));
        track.StrokeThickness = 10;
        track.StrokeStartLineCap = PenLineCap.Round;
        track.StrokeEndLineCap = PenLineCap.Round;
        GaugeCanvas.Children.Add(track);
    }

    private void CreateProgressArc()
    {
        _progressArc = new Path
        {
            StrokeThickness = 10,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        GaugeCanvas.Children.Add(_progressArc);
    }

    private void CreateAverageMarker()
    {
        _averageMarker = new Line
        {
            Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 209, 102)),
            StrokeThickness = 4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        GaugeCanvas.Children.Add(_averageMarker);
    }

    private void CreateNeedle()
    {
        _needleRotate = new RotateTransform { CenterX = _centerX, CenterY = _centerY };
        _needle = new Line
        {
            X1 = _centerX,
            Y1 = _centerY,
            X2 = _centerX + _radius - 6,
            Y2 = _centerY,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            RenderTransform = _needleRotate,
        };
        GaugeCanvas.Children.Add(_needle);

        var hub = new Ellipse
        {
            Width = 16,
            Height = 16,
            Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 48)),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
        };
        Canvas.SetLeft(hub, _centerX - 8);
        Canvas.SetTop(hub, _centerY - 8);
        GaugeCanvas.Children.Add(hub);
    }

    private void CreateReadout()
    {
        _valueText = new TextBlock
        {
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            TextAlignment = TextAlignment.Center,
            Width = _radius * 2,
        };
        Canvas.SetLeft(_valueText, _centerX - _radius);
        Canvas.SetTop(_valueText, _centerY + 10);
        GaugeCanvas.Children.Add(_valueText);

        _unitText = new TextBlock
        {
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromArgb(200, 190, 190, 200)),
            TextAlignment = TextAlignment.Center,
            Width = _radius * 2,
            Text = Unit,
        };
        Canvas.SetLeft(_unitText, _centerX - _radius);
        Canvas.SetTop(_unitText, _centerY + 42);
        GaugeCanvas.Children.Add(_unitText);
    }

    /// <summary>Builds an arc Path from startFraction..endFraction of the sweep.</summary>
    private Path BuildArcPath(double startFraction, double endFraction, double radius)
    {
        var startAngle = StartAngle + startFraction * SweepAngle;
        var endAngle = StartAngle + endFraction * SweepAngle;
        var startPoint = PointOnDial(startAngle, radius);
        var endPoint = PointOnDial(endAngle, radius);
        var isLargeArc = (endAngle - startAngle) > 180.0;

        var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = isLargeArc,
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return new Path { Data = geometry };
    }

    /// <summary>
    /// Updates the needle, progress arc, average marker, color, and readout for the
    /// current Value/Average/Maximum. Animates the needle for a smooth sweep.
    /// </summary>
    private void UpdateDynamicElements()
    {
        if (_needle is null || _needleRotate is null || _progressArc is null ||
            _averageMarker is null || _valueText is null || _unitText is null)
        {
            return;
        }

        var color = SpeedToColor(Math.Clamp(Maximum <= 0 ? 0 : Value / Maximum, 0.0, 1.0));

        // Animate the needle rotation to the new angle.
        var targetAngle = ValueToAngle(Value);
        var animation = new DoubleAnimation
        {
            To = targetAngle,
            Duration = new Duration(TimeSpan.FromMilliseconds(400)),
            EnableDependentAnimation = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(animation, _needleRotate);
        Storyboard.SetTargetProperty(animation, "Angle");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();

        // Rebuild the colored progress arc up to the current value.
        var fraction = Math.Clamp(Maximum <= 0 ? 0 : Value / Maximum, 0.0, 1.0);
        if (fraction > 0.001)
        {
            var arc = BuildArcPath(0.0, fraction, _radius);
            _progressArc.Data = arc.Data;
            _progressArc.Stroke = new SolidColorBrush(color);
            _progressArc.Visibility = Visibility.Visible;
        }
        else
        {
            _progressArc.Visibility = Visibility.Collapsed;
        }

        // Position the average marker as a radial tick at the average value.
        var avgAngle = ValueToAngle(Average);
        var avgOuter = PointOnDial(avgAngle, _radius + 6);
        var avgInner = PointOnDial(avgAngle, _radius - 16);
        _averageMarker.X1 = avgInner.X;
        _averageMarker.Y1 = avgInner.Y;
        _averageMarker.X2 = avgOuter.X;
        _averageMarker.Y2 = avgOuter.Y;
        _averageMarker.Visibility = Average > 0.001 ? Visibility.Visible : Visibility.Collapsed;

        // Readout: value colored to match the needle grade; unit label static.
        _valueText.Text = Value.ToString("F1");
        _valueText.Foreground = new SolidColorBrush(color);
        _unitText.Text = Unit;
    }

    /// <summary>
    /// Grades a 0..1 speed fraction from cool blue (slow) through cyan/green to
    /// warm green (fast), giving an at-a-glance speedtest.net-style color cue.
    /// </summary>
    private static Color SpeedToColor(double fraction)
    {
        // Stops: blue -> cyan -> green -> lime.
        (double stop, Color color)[] stops =
        {
            (0.0, Color.FromArgb(255, 66, 133, 244)),   // blue
            (0.4, Color.FromArgb(255, 0, 200, 220)),     // cyan
            (0.7, Color.FromArgb(255, 6, 214, 160)),     // teal-green
            (1.0, Color.FromArgb(255, 118, 224, 84)),    // lime-green
        };

        for (int i = 0; i < stops.Length - 1; i++)
        {
            var (s0, c0) = stops[i];
            var (s1, c1) = stops[i + 1];
            if (fraction <= s1)
            {
                var t = (fraction - s0) / (s1 - s0);
                return LerpColor(c0, c1, t);
            }
        }

        return stops[^1].color;
    }

    private static Color LerpColor(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return Color.FromArgb(
            255,
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }
}
