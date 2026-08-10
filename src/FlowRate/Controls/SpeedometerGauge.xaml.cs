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
/// secondary marker shows <see cref="Average"/>. The scale runs 0..<see cref="Maximum"/>
/// with numeric labels at each major tick.
/// </summary>
public sealed partial class SpeedometerGauge : UserControl
{
    // The dial sweeps 270 degrees: from 135 deg (lower-left) clockwise to 405 deg (lower-right).
    private const double StartAngle = 135.0;
    private const double SweepAngle = 270.0;
    private const int MajorTickCount = 10;

    // Dynamic elements updated each frame.
    private Path? _progressArc;
    private Polygon? _needle;
    private RotateTransform? _needleRotate;
    private Path? _averageMarker;
    private RotateTransform? _averageRotate;
    private TextBlock? _valueText;
    private TextBlock? _unitText;

    private double _centerX;
    private double _centerY;
    private double _radius;
    private bool _needleInitialized;

    public SpeedometerGauge()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(SpeedometerGauge),
            new PropertyMetadata(0.0, OnValueChanged));

    public double Average
    {
        get => (double)GetValue(AverageProperty);
        set => SetValue(AverageProperty, value);
    }

    public static readonly DependencyProperty AverageProperty =
        DependencyProperty.Register(nameof(Average), typeof(double), typeof(SpeedometerGauge),
            new PropertyMetadata(0.0, OnValueChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(SpeedometerGauge),
            new PropertyMetadata(100.0, OnMaximumChanged));

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(SpeedometerGauge),
            new PropertyMetadata("Mbps", OnValueChanged));

    #endregion

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SpeedometerGauge)d).UpdateDynamicElements();

    // A scale change moves every tick label, so the whole dial must be redrawn.
    private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (SpeedometerGauge)d;
        gauge.RebuildGauge(new Size(gauge.GaugeCanvas.ActualWidth, gauge.GaugeCanvas.ActualHeight));
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        => RebuildGauge(e.NewSize);

    /// <summary>Rebuilds all gauge geometry. Safe to call repeatedly.</summary>
    private void RebuildGauge(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return;

        try
        {
            GaugeCanvas.Children.Clear();
            _needleInitialized = false;

            _centerX = size.Width / 2.0;
            _centerY = size.Height * 0.60;
            _radius = Math.Min(size.Width / 2.0, size.Height * 0.60) * 0.82;

            DrawTrackArc();
            _progressArc = CreateEmptyArc();
            GaugeCanvas.Children.Add(_progressArc);
            DrawTicksAndLabels();
            CreateAverageMarker();
            CreateNeedle();
            CreateReadout();

            UpdateDynamicElements();
        }
        catch (Exception ex)
        {
            FlowRate.Core.Diagnostics.Logger.Error("SpeedometerGauge.RebuildGauge failed", ex);
        }
    }

    private double Fraction(double value)
    {
        var max = SafeMax();
        return Math.Clamp(Sanitize(value) / max, 0.0, 1.0);
    }

    private double SafeMax()
    {
        var max = Maximum;
        return double.IsNaN(max) || double.IsInfinity(max) || max <= 0 ? 1.0 : max;
    }

    private double FractionToAngle(double fraction) => StartAngle + fraction * SweepAngle;

    private Point PointOnDial(double angleDegrees, double radius)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        return new Point(_centerX + radius * Math.Cos(rad), _centerY + radius * Math.Sin(rad));
    }

    private void DrawTrackArc()
    {
        var track = new Path
        {
            Data = BuildArcGeometry(0.0, 1.0, _radius),
            Stroke = new SolidColorBrush(Color.FromArgb(60, 150, 150, 165)),
            StrokeThickness = 12,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        GaugeCanvas.Children.Add(track);
    }

    private Path CreateEmptyArc() => new()
    {
        StrokeThickness = 12,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
    };

    private void DrawTicksAndLabels()
    {
        var tickBrush = new SolidColorBrush(Color.FromArgb(200, 210, 210, 220));
        var labelBrush = new SolidColorBrush(Color.FromArgb(230, 225, 225, 235));
        var max = SafeMax();

        for (int i = 0; i <= MajorTickCount; i++)
        {
            var fraction = (double)i / MajorTickCount;
            var angle = FractionToAngle(fraction);

            var outer = PointOnDial(angle, _radius);
            var inner = PointOnDial(angle, _radius - 14);
            GaugeCanvas.Children.Add(new Line
            {
                X1 = inner.X,
                Y1 = inner.Y,
                X2 = outer.X,
                Y2 = outer.Y,
                Stroke = tickBrush,
                StrokeThickness = 2.5,
            });

            // Numeric label for this tick, positioned just inside the ticks.
            var labelValue = max * fraction;
            var label = new TextBlock
            {
                Text = FormatTickLabel(labelValue, max),
                FontSize = 11,
                Foreground = labelBrush,
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var labelPoint = PointOnDial(angle, _radius - 30);
            Canvas.SetLeft(label, labelPoint.X - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, labelPoint.Y - label.DesiredSize.Height / 2);
            GaugeCanvas.Children.Add(label);
        }
    }

    private static string FormatTickLabel(double value, double max)
    {
        // Whole numbers when the scale is coarse; one decimal when fine.
        if (max >= 100) return Math.Round(value).ToString("0");
        if (max >= 10) return value.ToString("0.#");
        return value.ToString("0.##");
    }

    private void CreateAverageMarker()
    {
        // A small triangle pointing inward from the dial rim.
        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Point(_centerX, _centerY - _radius - 2),
            IsClosed = true,
        };
        figure.Segments.Add(new LineSegment { Point = new Point(_centerX - 6, _centerY - _radius - 14) });
        figure.Segments.Add(new LineSegment { Point = new Point(_centerX + 6, _centerY - _radius - 14) });
        geometry.Figures.Add(figure);

        _averageRotate = new RotateTransform { CenterX = _centerX, CenterY = _centerY, Angle = 0 };
        _averageMarker = new Path
        {
            Data = geometry,
            Fill = new SolidColorBrush(Color.FromArgb(255, 255, 209, 102)),
            RenderTransform = _averageRotate,
            Visibility = Visibility.Collapsed,
        };
        GaugeCanvas.Children.Add(_averageMarker);
    }

    private void CreateNeedle()
    {
        // A tapered triangle needle so it reads clearly at a glance.
        _needleRotate = new RotateTransform { CenterX = _centerX, CenterY = _centerY, Angle = StartAngle };
        _needle = new Polygon
        {
            Points =
            {
                new Point(_centerX, _centerY - 4),
                new Point(_centerX, _centerY + 4),
                new Point(_centerX + _radius - 10, _centerY),
            },
            Fill = new SolidColorBrush(Colors.White),
            RenderTransform = _needleRotate,
        };
        GaugeCanvas.Children.Add(_needle);

        var hub = new Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = new SolidColorBrush(Color.FromArgb(255, 45, 45, 55)),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
        };
        Canvas.SetLeft(hub, _centerX - 9);
        Canvas.SetTop(hub, _centerY - 9);
        GaugeCanvas.Children.Add(hub);
    }

    private void CreateReadout()
    {
        _valueText = new TextBlock
        {
            FontSize = 30,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            TextAlignment = TextAlignment.Center,
            Width = _radius * 2,
        };
        Canvas.SetLeft(_valueText, _centerX - _radius);
        Canvas.SetTop(_valueText, _centerY + 18);
        GaugeCanvas.Children.Add(_valueText);

        _unitText = new TextBlock
        {
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromArgb(210, 195, 195, 205)),
            TextAlignment = TextAlignment.Center,
            Width = _radius * 2,
            Text = Unit,
        };
        Canvas.SetLeft(_unitText, _centerX - _radius);
        Canvas.SetTop(_unitText, _centerY + 54);
        GaugeCanvas.Children.Add(_unitText);
    }

    /// <summary>Builds a fresh arc <see cref="Geometry"/> (never reparented).</summary>
    private Geometry BuildArcGeometry(double startFraction, double endFraction, double radius)
    {
        var startAngle = FractionToAngle(startFraction);
        var endAngle = FractionToAngle(endFraction);
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
        return geometry;
    }

    /// <summary>
    /// Updates needle, arc, average marker, color, and readout for the current
    /// Value/Average/Maximum. Sets the readout first so the number always renders,
    /// then animates the needle for a smooth sweep.
    /// </summary>
    private void UpdateDynamicElements()
    {
        if (_needle is null || _needleRotate is null || _progressArc is null ||
            _averageMarker is null || _averageRotate is null || _valueText is null || _unitText is null)
        {
            return;
        }

        try
        {
            var valueFraction = Fraction(Value);
            var avgFraction = Fraction(Average);
            var color = SpeedToColor(valueFraction);

            // Readout FIRST - guarantees the number shows even if later drawing fails.
            _valueText.Text = Sanitize(Value).ToString("F1");
            _valueText.Foreground = new SolidColorBrush(color);
            _unitText.Text = Unit;

            // Colored progress arc (build a NEW geometry; do not reparent).
            if (valueFraction > 0.001)
            {
                _progressArc.Data = BuildArcGeometry(0.0, valueFraction, _radius);
                _progressArc.Stroke = new SolidColorBrush(color);
                _progressArc.Visibility = Visibility.Visible;
            }
            else
            {
                _progressArc.Visibility = Visibility.Collapsed;
            }

            // Average marker: rotate the rim triangle to the average angle.
            _averageRotate.Angle = FractionToAngle(avgFraction) - StartAngle;
            _averageMarker.Visibility = Sanitize(Average) > 0.001 ? Visibility.Visible : Visibility.Collapsed;

            // Needle: snap on first frame, animate thereafter.
            var targetAngle = FractionToAngle(valueFraction);
            if (!_needleInitialized)
            {
                _needleRotate.Angle = targetAngle;
                _needleInitialized = true;
            }
            else
            {
                var animation = new DoubleAnimation
                {
                    To = targetAngle,
                    Duration = new Duration(TimeSpan.FromMilliseconds(450)),
                    EnableDependentAnimation = true,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                };
                Storyboard.SetTarget(animation, _needleRotate);
                Storyboard.SetTargetProperty(animation, "Angle");
                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }

            _needle.Fill = new SolidColorBrush(color);
        }
        catch (Exception ex)
        {
            FlowRate.Core.Diagnostics.Logger.Error("SpeedometerGauge.UpdateDynamicElements failed", ex);
        }
    }

    private static double Sanitize(double v)
        => double.IsNaN(v) || double.IsInfinity(v) || v < 0 ? 0 : v;

    /// <summary>
    /// Grades a 0..1 speed fraction from cool blue (slow) through cyan/teal to
    /// lime-green (fast), giving an at-a-glance speedtest.net-style color cue.
    /// </summary>
    private static Color SpeedToColor(double fraction)
    {
        (double stop, Color color)[] stops =
        {
            (0.0, Color.FromArgb(255, 66, 133, 244)),
            (0.4, Color.FromArgb(255, 0, 200, 220)),
            (0.7, Color.FromArgb(255, 6, 214, 160)),
            (1.0, Color.FromArgb(255, 118, 224, 84)),
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
