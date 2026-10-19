using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Miracle.Windows;
public sealed class WheelWindow : Window
{
    private readonly Canvas canvas = new();
    private readonly List<(Button Button, Point Center, string Format)> choices = new();
    private readonly Action<string[], string> convert;
    private string? selected;
    private bool closing;
    private readonly Button hub;
    private readonly string[] files;
    public WheelWindow(string[] files, IReadOnlyList<string> formats, Action<string[], string> convert)
    {
        this.convert = convert; this.files = files;
        double size = formats.Count > 8 ? 600 : 440;
        double origin = size / 2, radius = formats.Count > 8 ? 235 : 153;
        double buttonWidth = formats.Count > 8 ? 78 : 88, buttonHeight = formats.Count > 8 ? 46 : 58;
        Width = size; Height = size; ResizeMode = ResizeMode.NoResize; WindowStyle = WindowStyle.None;
        AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false; Topmost = true;
        Content = canvas;
        var disc = new Ellipse { Width = size - 20, Height = size - 20, Fill = new LinearGradientBrush(Color.FromRgb(24,24,29), Color.FromRgb(51,35,56), 45), Stroke = Theme.Brush("#8D7D9E"), StrokeThickness = 2 };
        Canvas.SetLeft(disc, 10); Canvas.SetTop(disc, 10); canvas.Children.Add(disc);
        var center = Theme.Button("PICK YOUR\nNEW FORM\n✦", () => Commit(), Theme.Paper); hub = center; center.Width = 132; center.Height = 132; center.Margin = new Thickness(0); center.FontSize = 16;
        Canvas.SetLeft(center, origin - 66); Canvas.SetTop(center, origin - 66); canvas.Children.Add(center);

        for (int i = 0; i < formats.Count; i++)
        {
            double angle = i * Math.PI * 2 / formats.Count - Math.PI / 2;
            var point = new Point(origin + radius * Math.Cos(angle), origin + radius * Math.Sin(angle));
            var button = Theme.Button(formats[i].ToUpperInvariant(), () => { }, Theme.FormatColor(formats[i])); button.Width = buttonWidth; button.Height = buttonHeight; button.Margin = new Thickness(0); button.FontSize = 15;
            string format = formats[i]; button.MouseEnter += (_, _) => Select(format); button.Click += (_, _) => { Select(format); Commit(); };
            Canvas.SetLeft(button, point.X - buttonWidth / 2); Canvas.SetTop(button, point.Y - buttonHeight / 2); canvas.Children.Add(button); choices.Add((button, point, format));
        }
        var hint = new TextBlock { Text = "Click a format · Esc to close", FontSize = 10, Foreground = Theme.Muted, Width = 230, TextAlignment = TextAlignment.Center, IsHitTestVisible = false };
        Canvas.SetLeft(hint, origin - 115); Canvas.SetTop(hint, origin + 75); canvas.Children.Add(hint);
        KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
        Closing += (_, _) => closing = true;
        Deactivated += (_, _) => { if (!closing) Close(); };
        AllowDrop = true;
        DragOver += (_, e) => { var p = e.GetPosition(canvas); double best = double.MaxValue; string? found = null; foreach (var choice in choices) { double distance = (p - choice.Center).Length; if (distance < best) { best = distance; found = choice.Format; } } if (found != null && best < 80) Select(found); else Select(null); e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        Drop += (_, e) => { e.Handled = true; if (selected != null && e.Data.GetData(DataFormats.FileDrop) is string[] dropped) Commit(dropped); };
        Loaded += (_, _) => { var p = System.Windows.Forms.Cursor.Position; var source = PresentationSource.FromVisual(this); var scale = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity; var mouse = scale.Transform(new Point(p.X, p.Y)); Rect area = SystemParameters.WorkArea; Left = Math.Clamp(mouse.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)); Top = Math.Clamp(mouse.Y - Height / 2, area.Top, Math.Max(area.Top, area.Bottom - Height)); Activate(); };
    }
    internal bool ChoicesDoNotOverlap()
    {
        for (int i = 0; i < choices.Count; i++)
        for (int j = i + 1; j < choices.Count; j++)
        {
            var a = choices[i]; var b = choices[j];
            if (Math.Abs(a.Center.X - b.Center.X) < (a.Button.Width + b.Button.Width) / 2 &&
                Math.Abs(a.Center.Y - b.Center.Y) < (a.Button.Height + b.Button.Height) / 2) return false;
        }
        return true;
    }
    internal void Select(string? format) { selected = format; hub.Content = format == null ? "PICK YOUR\nNEW FORM\n✦" : "CONVERT TO\n" + format.ToUpperInvariant() + "\n↗"; foreach (var choice in choices) { choice.Button.BorderBrush = choice.Format == format ? Theme.White : Theme.Ink; choice.Button.BorderThickness = new Thickness(choice.Format == format ? 4 : 1); } }
    internal void Commit(string[]? dropped = null) { if (selected == null || closing) return; string format = selected; Close(); convert(dropped ?? files, format); }
}
