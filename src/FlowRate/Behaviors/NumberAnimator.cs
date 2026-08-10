using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlowRate.Behaviors;

/// <summary>
/// Attached properties that animate a <see cref="TextBlock"/>'s displayed number
/// smoothly toward a target value, giving readouts a lively "counting" feel as
/// live throughput updates arrive.
///
/// Usage:
///   &lt;TextBlock
///       behaviors:NumberAnimator.Value="{x:Bind ViewModel.CurrentThroughputGbps, Mode=OneWay}"
///       behaviors:NumberAnimator.Format="F3"
///       behaviors:NumberAnimator.Suffix=" Gbps" /&gt;
/// </summary>
public static class NumberAnimator
{
    private const double DurationMs = 500.0;
    private const double FrameMs = 16.0;

    #region Value

    public static double GetValue(DependencyObject obj) => (double)obj.GetValue(ValueProperty);
    public static void SetValue(DependencyObject obj, double value) => obj.SetValue(ValueProperty, value);

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached(
            "Value", typeof(double), typeof(NumberAnimator),
            new PropertyMetadata(0.0, OnValueChanged));

    #endregion

    #region Format

    public static string GetFormat(DependencyObject obj) => (string)obj.GetValue(FormatProperty);
    public static void SetFormat(DependencyObject obj, string value) => obj.SetValue(FormatProperty, value);

    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.RegisterAttached(
            "Format", typeof(string), typeof(NumberAnimator), new PropertyMetadata("F2"));

    #endregion

    #region Suffix

    public static string GetSuffix(DependencyObject obj) => (string)obj.GetValue(SuffixProperty);
    public static void SetSuffix(DependencyObject obj, string value) => obj.SetValue(SuffixProperty, value);

    public static readonly DependencyProperty SuffixProperty =
        DependencyProperty.RegisterAttached(
            "Suffix", typeof(string), typeof(NumberAnimator), new PropertyMetadata(string.Empty));

    #endregion

    // Per-element animation state (current displayed value + active timer).
    private static readonly DependencyProperty DisplayedProperty =
        DependencyProperty.RegisterAttached(
            "Displayed", typeof(double), typeof(NumberAnimator), new PropertyMetadata(0.0));

    private static readonly DependencyProperty TimerProperty =
        DependencyProperty.RegisterAttached(
            "Timer", typeof(DispatcherTimer), typeof(NumberAnimator), new PropertyMetadata(null));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        var target = (double)e.NewValue;
        var start = (double)d.GetValue(DisplayedProperty);

        // Stop any in-flight animation on this element.
        if (d.GetValue(TimerProperty) is DispatcherTimer existing)
            existing.Stop();

        // No visible change or non-finite target: snap immediately.
        if (double.IsNaN(target) || double.IsInfinity(target) || Math.Abs(target - start) < 0.0005)
        {
            SetDisplayed(textBlock, target, isFinal: true);
            return;
        }

        var elapsed = 0.0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FrameMs) };
        timer.Tick += (_, _) =>
        {
            elapsed += FrameMs;
            var t = Math.Clamp(elapsed / DurationMs, 0.0, 1.0);
            // CubicEaseOut for a natural settle.
            var eased = 1.0 - Math.Pow(1.0 - t, 3.0);
            var current = start + (target - start) * eased;

            if (t >= 1.0)
            {
                timer.Stop();
                SetDisplayed(textBlock, target, isFinal: true);
            }
            else
            {
                SetDisplayed(textBlock, current, isFinal: false);
            }
        };
        d.SetValue(TimerProperty, timer);
        timer.Start();
    }

    private static void SetDisplayed(TextBlock textBlock, double value, bool isFinal)
    {
        textBlock.SetValue(DisplayedProperty, value);
        var format = GetFormat(textBlock);
        var suffix = GetSuffix(textBlock);
        textBlock.Text = value.ToString(format) + suffix;
    }
}
