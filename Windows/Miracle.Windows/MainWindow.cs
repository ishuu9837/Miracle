using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Miracle.Windows;
public sealed class MainWindow : Window
{
    public static readonly SolidColorBrush Ink = new(Color.FromRgb(20, 20, 18));
    public static readonly SolidColorBrush Paper = new(Color.FromRgb(242, 241, 233));
    public static readonly SolidColorBrush Acid = new(Color.FromRgb(227, 255, 101));
    private readonly ConversionService service = new();
    private readonly StackPanel formats = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock selection = new() { Text = "Drop files here or choose files to begin.", TextWrapping = TextWrapping.Wrap, FontSize = 16 };
    private readonly TextBlock engineStatus = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brushes.DimGray };
    private readonly TextBlock status = new() { Text = "Ready. Originals are never overwritten.", TextWrapping = TextWrapping.Wrap, FontSize = 13 };
    private readonly TextBlock destination = new() { Text = "Save beside the original file", FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private readonly Button cancel = new() { Content = "Cancel conversion", IsEnabled = false };
    private readonly Button reveal = new() { Content = "Show last result", IsEnabled = false };
    private readonly Forms.NotifyIcon tray;
    private ExplorerShiftHook? shiftHook;
    private WheelWindow? wheel;
    private string[] files = Array.Empty<string>();
    private string? outputFolder, lastOutput;
    private CancellationTokenSource? cancellation;
    private Task? running;
    private bool quitting, balloonShown;
    public MainWindow(string[] initial)
    {
        Title = "Miracle for Windows · Preview"; Width = 760; Height = 690; MinWidth = 650; MinHeight = 600;
        Background = Paper; Foreground = Ink; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = new StackPanel { Margin = new Thickness(32) };
        var scroll = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Content = scroll;
        var brand = new StackPanel { Orientation = Orientation.Horizontal };
        var image = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon.png")), Width = 65, Height = 65, Margin = new Thickness(0, 0, 18, 0) };
        brand.Children.Add(image); brand.Children.Add(new TextBlock { Text = "miracle", FontSize = 38, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center }); root.Children.Add(brand);
        root.Children.Add(new TextBlock { Text = "A little file magic. Now on Windows.", FontSize = 19, Margin = new Thickness(0, 22, 0, 6) });
        root.Children.Add(new TextBlock { Text = "Windows preview · macOS conversion coverage is not yet ported.", FontSize = 12, Foreground = Brushes.DimGray });
        var drop = new Border { Child = selection, BorderBrush = Ink, BorderThickness = new Thickness(1), Padding = new Thickness(22), Margin = new Thickness(0, 25, 0, 12), MinHeight = 100, AllowDrop = true };
        drop.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && running == null ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        drop.Drop += (_, e) => { e.Handled = true; if (e.Data.GetData(DataFormats.FileDrop) is string[] dropped) SetFiles(dropped); };
        root.Children.Add(drop);
        var fileControls = new WrapPanel();
        AddButton(fileControls, "Choose files", ChooseFiles); AddButton(fileControls, "Open wheel", () => ShowWheel(files));
        AddButton(fileControls, "Choose output folder", ChooseOutput); AddButton(fileControls, "Beside originals", () => { outputFolder = null; destination.Text = "Save beside the original file"; });
        root.Children.Add(fileControls); root.Children.Add(destination);
        root.Children.Add(new TextBlock { Text = "CONVERT TO", FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 22, 0, 8) });
        root.Children.Add(new ScrollViewer { Content = formats, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
        root.Children.Add(new Border { Background = Acid, Child = status, Padding = new Thickness(13), Margin = new Thickness(0, 18, 0, 12) });
        var jobControls = new WrapPanel(); cancel.Margin = new Thickness(0, 0, 8, 8); cancel.Padding = new Thickness(12, 7, 12, 7); cancel.Click += (_, _) => cancellation?.Cancel(); jobControls.Children.Add(cancel);
        reveal.Margin = new Thickness(0, 0, 8, 8); reveal.Padding = new Thickness(12, 7, 12, 7); reveal.Click += (_, _) => Reveal(); jobControls.Children.Add(reveal); root.Children.Add(jobControls);
        var shiftToggle = new CheckBox { Content = "Enable Shift-tap wheel for selected files in File Explorer", Margin = new Thickness(0, 13, 0, 12), FontSize = 13 };
        shiftToggle.Checked += (_, _) => { try { shiftHook ??= new ExplorerShiftHook(Dispatcher, ShowWheel); shiftHook.Enabled = true; } catch (Exception ex) { status.Text = "Explorer shortcut unavailable: " + ex.Message; shiftToggle.IsChecked = false; } };
        shiftToggle.Unchecked += (_, _) => { if (shiftHook != null) shiftHook.Enabled = false; }; root.Children.Add(shiftToggle);
        root.Children.Add(engineStatus);
        var engineControls = new WrapPanel(); AddButton(engineControls, "Locate ffmpeg.exe", ChooseFFmpeg); AddButton(engineControls, "Check engines again", async () => await DetectAsync()); root.Children.Add(engineControls);
        root.Children.Add(new TextBlock { Text = "Closing this window keeps Miracle in the system tray. Use the tray menu to quit.\nNo account, uploads, automatic startup, or automatic downloads.", FontSize = 11, Foreground = Brushes.DimGray, Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap });
        tray = new Forms.NotifyIcon { Text = "Miracle for Windows", Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Miracle", null, (_, _) => Dispatcher.Invoke(ShowMain));
        menu.Items.Add("Choose files…", null, (_, _) => Dispatcher.Invoke(() => { ShowMain(); ChooseFiles(); }));
        menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(RequestQuit)); tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMain);
        Closing += OnClosing;
        Loaded += async (_, _) => { await DetectAsync(); if (initial.Length > 0) SetFiles(initial); };
    }
    private static void AddButton(Panel panel, string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 8), Background = Paper, Foreground = Ink, BorderBrush = Ink };
        button.Click += (_, _) => action(); panel.Children.Add(button);
    }
    private async Task DetectAsync(string? path = null)
    {
        if (running != null) { status.Text = "Wait for the current conversion before changing engines."; return; }
        engineStatus.Text = "Checking conversion engines…";
        await service.DetectAsync(path);
        engineStatus.Text = service.EngineStatus; RefreshFormats();
    }
    private void ShowMain() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void ChooseFiles()
    {
        if (running != null) return;
        var dialog = new OpenFileDialog { Multiselect = true, Title = "Choose files to convert" };
        if (dialog.ShowDialog(this) == true) SetFiles(dialog.FileNames);
    }
    private void ChooseOutput()
    {
        if (running != null) return;
        var dialog = new OpenFolderDialog { Title = "Save converted files in…" };
        if (dialog.ShowDialog(this) == true) { outputFolder = dialog.FolderName; destination.Text = "Save to " + outputFolder; }
    }
    private async void ChooseFFmpeg()
    {
        if (running != null) return;
        var dialog = new OpenFileDialog { Filter = "FFmpeg executable|ffmpeg.exe", Title = "Choose a trusted FFmpeg executable" };
        if (dialog.ShowDialog(this) == true) await DetectAsync(dialog.FileName);
    }
    private void SetFiles(string[] input)
    {
        if (running != null) return;
        files = input.Where(File.Exists).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        selection.Text = files.Length == 0 ? "No files selected." : files.Length == 1 ? Path.GetFileName(files[0]) : files.Length + " files selected\n" + string.Join(" · ", files.Take(3).Select(Path.GetFileName));
        RefreshFormats();
    }
    private void RefreshFormats()
    {
        formats.Children.Clear();
        var options = service.CommonOutputs(files);
        foreach (string format in options) AddButton(formats, format.ToUpperInvariant(), () => BeginConversion(files, format));
        if (options.Count == 0) formats.Children.Add(new TextBlock { Text = files.Length == 0 ? "Choose files to see available formats." : "No shared conversion available. Check engines or select files of the same kind.", FontSize = 12, TextWrapping = TextWrapping.Wrap });
    }
    private void ShowWheel(string[] input)
    {
        if (running != null || quitting) return;
        SetFiles(input);
        var options = service.CommonOutputs(files);
        if (options.Count == 0) { ShowMain(); status.Text = "No shared conversion for this selection. Choose a supported file or configure FFmpeg."; return; }
        wheel?.Close();
        wheel = new WheelWindow(files, options, BeginConversion); wheel.Show();
    }
    private async void BeginConversion(string[] input, string format)
    {
        if (running != null || quitting) return;
        var batch = input.ToArray();
        if (batch.Length == 0 || batch.Any(f => !service.Outputs(f).Contains(format))) { status.Text = "This format is unavailable for one or more selected files."; return; }
        ShowMain(); cancellation = new CancellationTokenSource(); cancel.IsEnabled = true; formats.IsEnabled = false;
        running = ConvertBatchAsync(batch, format, cancellation.Token);
        try { await running; }
        finally { running = null; cancellation.Dispose(); cancellation = null; cancel.IsEnabled = false; formats.IsEnabled = true; }
    }
    private async Task ConvertBatchAsync(string[] batch, string format, CancellationToken token)
    {
        int complete = 0;
        try
        {
            foreach (string input in batch)
            {
                status.Text = "Converting " + (complete + 1) + "/" + batch.Length + ": " + Path.GetFileName(input);
                lastOutput = await service.ConvertAsync(input, format, outputFolder, token);
                complete++; reveal.IsEnabled = true;
            }
            status.Text = "Done · " + complete + " file(s) saved. Originals preserved.\n" + lastOutput;
        }
        catch (OperationCanceledException) { status.Text = "Cancelled. " + complete + " completed file(s) kept; unfinished output removed."; }
        catch (Exception ex) { status.Text = "Conversion stopped: " + ex.Message + "\n" + complete + " completed file(s) kept."; }
    }
    private void Reveal()
    {
        if (lastOutput == null || !File.Exists(lastOutput)) return;
        var start = new ProcessStartInfo("explorer.exe") { UseShellExecute = false }; start.ArgumentList.Add("/select,"); start.ArgumentList.Add(lastOutput); Process.Start(start);
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (quitting) return;
        e.Cancel = true; Hide();
        if (!balloonShown) { balloonShown = true; tray.ShowBalloonTip(2000, "Miracle is still running", "Open or quit Miracle from its system tray icon.", Forms.ToolTipIcon.Info); }
    }
    private async void RequestQuit()
    {
        if (quitting) return;
        quitting = true; shiftHook?.Dispose(); wheel?.Close(); cancellation?.Cancel();
        if (running != null) await running;
        tray.Visible = false; tray.Dispose(); Application.Current.Shutdown();
    }
}
