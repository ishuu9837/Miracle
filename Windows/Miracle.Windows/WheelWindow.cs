using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Miracle.Windows;
public sealed class WheelWindow : Window
{
    private readonly Canvas canvas = new();
    private readonly List<(Button Button, Point Center, string Format)> choices = new();
    private readonly Action<string[], string> convert;
    private readonly Action<string[], string>? tool;
    private readonly List<(string Title, string[] Options)> pages = new();
    private readonly string[] files;
    private readonly Button hub;
    private readonly TextBlock hint;
    private readonly DispatcherTimer? dragTimer;
    private string? selected;
    private bool closing;
    private int page;
    private long releasedAt;
    private const double Size = 440, Origin = 220, Radius = 153;
    internal int PageIndex => page;
    internal int PageCount => pages.Count;
    internal IReadOnlyList<string> VisibleOptions => pages[page].Options;
    internal bool DragMode { get; }
    public WheelWindow(string[] files, IReadOnlyList<string> formats, Action<string[], string> convert,
        bool dragMode = false, IReadOnlyList<string>? toolIds = null, Action<string[], string>? tool = null)
    {
        this.convert = convert; this.files = files.ToArray(); this.tool = tool; DragMode = dragMode;
        foreach (var chunk in formats.Chunk(6)) pages.Add(("FORMATS", chunk));
        if (toolIds != null && tool != null) foreach (var chunk in toolIds.Chunk(6)) pages.Add(("FILE TOOLS", chunk));
        if (pages.Count == 0) throw new ArgumentException("A wheel needs at least one supported option.", nameof(formats));
        Width = Size; Height = Size; ResizeMode = ResizeMode.NoResize; WindowStyle = WindowStyle.None;
        AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false; Topmost = true;
        ShowActivated = !dragMode; Content = canvas;
        var disc = new Ellipse { Width = Size - 20, Height = Size - 20, Fill = new LinearGradientBrush(Color.FromRgb(24,24,29), Color.FromRgb(51,35,56), 45), Stroke = Theme.Brush("#8D7D9E"), StrokeThickness = 2 };
        Canvas.SetLeft(disc, 10); Canvas.SetTop(disc, 10); canvas.Children.Add(disc);
        hub = Theme.Button("", () => Commit(), Theme.Paper); hub.Width = 132; hub.Height = 132; hub.Margin = new Thickness(0); hub.FontSize = 14;
        Canvas.SetLeft(hub, Origin - 66); Canvas.SetTop(hub, Origin - 66); canvas.Children.Add(hub);
        hint = new TextBlock { FontSize = 10, Foreground = Theme.Muted, Width = 270, TextAlignment = TextAlignment.Center, IsHitTestVisible = false };
        Canvas.SetLeft(hint, Origin - 135); Canvas.SetTop(hint, Origin + 73); canvas.Children.Add(hint);
        var next = Theme.Button("Next set ↻", NextPage, Theme.Paper); next.Width = 104; next.Height = 28; next.FontSize = 10; next.Margin = new Thickness(0);
        Canvas.SetLeft(next, Origin - 52); Canvas.SetTop(next, Size - 32); canvas.Children.Add(next);
        RenderPage();
        // During an OLE drag Explorer owns keyboard focus. The passive hook observes Shift/Esc.
        KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
        Closing += (_, _) => { closing = true; dragTimer?.Stop(); };
        Deactivated += (_, _) => { if (!closing && !DragMode) Close(); };
        AllowDrop = true;
        DragOver += (_, e) => { bool valid = e.Data.GetDataPresent(DataFormats.FileDrop) && (e.AllowedEffects & DragDropEffects.Copy) != 0; Select(valid ? Hit(e.GetPosition(canvas)) : null); e.Effects = valid ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        DragLeave += (_, _) => Select(null);
        Drop += (_, e) => {
            e.Handled = true;
            e.Effects = (e.AllowedEffects & DragDropEffects.Copy) != 0 && AcceptDrop(e.Data, e.GetPosition(canvas)) ? DragDropEffects.Copy : DragDropEffects.None;
        };
        SourceInitialized += (_, _) => {
            if (DragMode) { var hwnd = new WindowInteropHelper(this).Handle; SetWindowLongPtr(hwnd, -20, new IntPtr(GetWindowLongPtr(hwnd, -20).ToInt64() | 0x08000000)); }
        };
        Loaded += (_, _) => {
            var p = System.Windows.Forms.Cursor.Position;
            var scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var mouse = scale.Transform(new Point(p.X, p.Y));
            // Use the cursor's monitor, including negative coordinates and per-monitor scaling.
            var screen = System.Windows.Forms.Screen.FromPoint(p).WorkingArea;
            var tl = scale.Transform(new Point(screen.Left, screen.Top)); var br = scale.Transform(new Point(screen.Right, screen.Bottom));
            Left = Math.Clamp(mouse.X - Width / 2, tl.X, Math.Max(tl.X, br.X - Width));
            Top = Math.Clamp(mouse.Y - Height / 2, tl.Y, Math.Max(tl.Y, br.Y - Height));
            if (!DragMode) Activate();
        };
        if (dragMode) {
            dragTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            dragTimer.Tick += (_, _) => {
                if (ExplorerShiftHook.LeftButtonDown) releasedAt = 0;
                else if (releasedAt == 0) releasedAt = Environment.TickCount64;
                // Let the OLE Drop handler run before dismissing a released/cancelled drag.
                else if (Environment.TickCount64 - releasedAt > 240 && !closing) Close();
            };
            Loaded += (_, _) => dragTimer.Start();
        }
    }
    internal void NextPage() { if (closing) return; page = (page + 1) % pages.Count; RenderPage(); }
    private void RenderPage()
    {
        foreach (var choice in choices) canvas.Children.Remove(choice.Button);
        choices.Clear(); selected = null;
        var options = pages[page].Options;
        for (int i = 0; i < options.Length; i++) {
            string option = options[i];
            double angle = i * Math.PI * 2 / options.Length - Math.PI / 2;
            var point = new Point(Origin + Radius * Math.Cos(angle), Origin + Radius * Math.Sin(angle));
            string label = option.Contains('.') ? FileTools.For(files[0]).First(t => t.Id == option).Name : option.ToUpperInvariant();
            var button = Theme.Button(label, () => { }, option.Contains('.') ? Theme.Brush(new[] { "#99DDFF", "#FFB68F", "#FF91C8", "#CCB8FF", "#E4FF73", "#93E5CB" }[i]) : Theme.FormatColor(option));
            button.Width = 88; button.Height = 58; button.Margin = new Thickness(0); button.FontSize = 12; button.Padding = new Thickness(4);
            button.Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            System.Windows.Automation.AutomationProperties.SetName(button, label);
            button.MouseEnter += (_, _) => Select(option); button.Click += (_, _) => { Select(option); Commit(); };
            Canvas.SetLeft(button, point.X - 44); Canvas.SetTop(button, point.Y - 29); canvas.Children.Add(button); choices.Add((button, point, option));
        }
        hint.Text = $"{pages[page].Title}  {page + 1}/{pages.Count}\nShift: next set · Esc: close";
        Select(null);
    }
    internal string? Hit(Point point)
    {
        // Only a visible colored option accepts a drop; gaps and hub never guess a format.
        foreach (var choice in choices) if (Math.Abs(point.X - choice.Center.X) <= 44 && Math.Abs(point.Y - choice.Center.Y) <= 29) return choice.Format;
        return null;
    }
    internal bool ChoicesDoNotOverlap()
    {
        for (int i = 0; i < choices.Count; i++) for (int j = i + 1; j < choices.Count; j++) {
            var a = choices[i]; var b = choices[j];
            if (Math.Abs(a.Center.X - b.Center.X) < 88 && Math.Abs(a.Center.Y - b.Center.Y) < 58) return false;
        }
        return true;
    }
    internal void Select(string? option) {
        selected = option != null && VisibleOptions.Contains(option) ? option : null;
        hub.Content = selected == null ? (DragMode ? "SLIDE TO\nAN OPTION\n✦" : "PICK YOUR\nNEW FORM\n✦") : selected.Contains('.') ? "OPEN TOOL\n" + FileTools.For(files[0]).First(t => t.Id == selected).Name : "CONVERT TO\n" + selected.ToUpperInvariant() + "\n↗";
        foreach (var choice in choices) { choice.Button.BorderBrush = choice.Format == selected ? Theme.White : Theme.Ink; choice.Button.BorderThickness = new Thickness(choice.Format == selected ? 4 : 1); }
    }
    internal bool AcceptDrop(IDataObject data, Point point) {
        if (!data.GetDataPresent(DataFormats.FileDrop) || data.GetData(DataFormats.FileDrop) is not string[] dropped || dropped.Length == 0) { Select(null); return false; }
        Select(Hit(point));
        if (selected != null) { Commit(dropped); return true; }
        Close(); return false;
    }
    internal void Commit(string[]? dropped = null) {
        if (selected == null || closing) return;
        string option = selected; var input = (dropped ?? files).ToArray(); Close();
        // Finish the OLE drop before opening dialogs or starting a conversion.
        Dispatcher.BeginInvoke(() => { if (option.Contains('.')) tool?.Invoke(input, option); else convert(input, option); });
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
