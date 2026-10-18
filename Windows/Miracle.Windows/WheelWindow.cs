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
    private readonly string[] files;
    public WheelWindow(string[] files, IReadOnlyList<string> formats, Action<string[], string> convert)
    {
        this.convert = convert; this.files = files;
        Width = 440; Height = 440; ResizeMode = ResizeMode.NoResize; WindowStyle = WindowStyle.None;
        AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false; Topmost = true;
        Content = canvas;
        var disc = new Ellipse { Width = 420, Height = 420, Fill = MainWindow.Paper, Stroke = MainWindow.Ink, StrokeThickness = 1 };
        Canvas.SetLeft(disc, 10); Canvas.SetTop(disc, 10); canvas.Children.Add(disc);
        var center = new Button { Width = 132, Height = 132, Background = MainWindow.Acid, BorderBrush = MainWindow.Ink, Content = files.Length == 1 ? "CHOOSE\nA FORMAT" : files.Length + " FILES\nCHOOSE FORMAT", FontSize = 15, FontWeight = FontWeights.Bold };
        Canvas.SetLeft(center, 154); Canvas.SetTop(center, 154); canvas.Children.Add(center);
        center.Click += (_, _) => Commit();
        for (int i = 0; i < formats.Count; i++)
        {
            double angle = i * Math.PI * 2 / formats.Count - Math.PI / 2;
            var point = new Point(220 + 153 * Math.Cos(angle), 220 + 153 * Math.Sin(angle));
            var button = new Button { Width = 88, Height = 55, Content = formats[i].ToUpperInvariant(), Background = MainWindow.Paper, Foreground = MainWindow.Ink, BorderBrush = MainWindow.Ink, FontSize = 14, FontWeight = FontWeights.SemiBold };
            string format = formats[i]; button.MouseEnter += (_, _) => Select(format); button.Click += (_, _) => { Select(format); Commit(); };
            Canvas.SetLeft(button, point.X - 44); Canvas.SetTop(button, point.Y - 27); canvas.Children.Add(button); choices.Add((button, point, format));
        }
        var hint = new TextBlock { Text = "Click a format · Esc to close", FontSize = 10, Foreground = MainWindow.Ink, Width = 230, TextAlignment = TextAlignment.Center, IsHitTestVisible = false };
        Canvas.SetLeft(hint, 105); Canvas.SetTop(hint, 292); canvas.Children.Add(hint);
        KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
        Closing += (_, _) => closing = true;
        Deactivated += (_, _) => { if (!closing) Close(); };
        AllowDrop = true;
        DragOver += (_, e) => { var p = e.GetPosition(canvas); double best = double.MaxValue; string? found = null; foreach (var choice in choices) { double distance = (p - choice.Center).Length; if (distance < best) { best = distance; found = choice.Format; } } if (found != null && best < 80) Select(found); else Select(null); e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        Drop += (_, e) => { e.Handled = true; if (selected != null && e.Data.GetData(DataFormats.FileDrop) is string[] dropped) Commit(dropped); };
        Loaded += (_, _) => { var p = System.Windows.Forms.Cursor.Position; var source = PresentationSource.FromVisual(this); var scale = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity; var mouse = scale.Transform(new Point(p.X, p.Y)); Rect area = SystemParameters.WorkArea; Left = Math.Clamp(mouse.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)); Top = Math.Clamp(mouse.Y - Height / 2, area.Top, Math.Max(area.Top, area.Bottom - Height)); Activate(); };
    }
    private void Select(string? format) { selected = format; foreach (var choice in choices) { choice.Button.Background = choice.Format == format ? MainWindow.Ink : MainWindow.Paper; choice.Button.Foreground = choice.Format == format ? MainWindow.Acid : MainWindow.Ink; } }
    private void Commit(string[]? dropped = null) { if (selected == null) return; string format = selected; Close(); convert(dropped ?? files, format); }
}
