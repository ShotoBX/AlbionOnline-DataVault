using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace StatisticsAnalysisTool.Common;

// https://stackoverflow.com/questions/14485818/how-to-update-a-progress-bar-so-it-increases-smoothly
public class ProgressBarSmoother
{
    public static readonly DependencyProperty SmoothValueProperty =
        DependencyProperty.RegisterAttached("SmoothValue", typeof(double), typeof(ProgressBarSmoother), new PropertyMetadata(0.0, Changing));

    public static double GetSmoothValue(DependencyObject obj)
    {
        return (double) obj.GetValue(SmoothValueProperty);
    }

    public static void SetSmoothValue(DependencyObject obj, double value)
    {
        obj.SetValue(SmoothValueProperty, value);
    }

    private static void Changing(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ProgressBar progressBar)
        {
            return;
        }

        var oldValue = Sanitize((double) e.OldValue, progressBar);
        var newValue = Sanitize((double) e.NewValue, progressBar);

        var anim = new DoubleAnimation(oldValue, newValue, new TimeSpan(0, 0, 0, 0, 250));
        progressBar.BeginAnimation(RangeBase.ValueProperty, anim, HandoffBehavior.Compose);
    }

    // A single bad tick (e.g. dividing by a not-yet-populated total) must not permanently
    // freeze the bar, since IsNaN/IsInfinity would otherwise never receive a corrective animation.
    private static double Sanitize(double value, RangeBase range)
    {
        if (double.IsNaN(value))
        {
            return range.Minimum;
        }

        if (double.IsPositiveInfinity(value))
        {
            return range.Maximum;
        }

        if (double.IsNegativeInfinity(value))
        {
            return range.Minimum;
        }

        return value;
    }
}