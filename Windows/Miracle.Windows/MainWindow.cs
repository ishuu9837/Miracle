using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Miracle.Windows;
public sealed class MainWindow : Window
{
    public static readonly SolidColorBrush Ink = Theme.Ink, Paper = Theme.Paper, Acid = Theme.Acid;
    private readonly ConversionService service = new();
    private readonly WrapPanel formats = new();
    private readonly TextBlock selection = Theme.Text("Drop something good here.", 20, Theme.Ink, true);
    private readonly TextBlock fileDetail = Theme.Text("Images, audio, video or subtitles · files stay on your PC", 12, Brushes.DimGray);
    private readonly TextBlock engineStatus = Theme.Text("Checking engines…", 11, Theme.Muted);
    private readonly TextBlock status = Theme.Text("Ready for a little magic.", 13, Theme.Ink);
    private readonly TextBlock destination = Theme.Text("Save beside the originals", 11, Brushes.DimGray);
    private readonly TextBlock selectedLabel = Theme.Text("Choose a format first", 12, Theme.Ink);
    private readonly Button cancel, reveal, convertButton;
    private readonly WrapPanel fileControls = new();
    private readonly WrapPanel engineControls = new();
    private readonly Forms.NotifyIcon? tray;
    private ExplorerShiftHook? shiftHook;
    private WheelWindow? wheel;
    private string[] files = Array.Empty<string>();
    private string? outputFolder, lastOutput, selectedFormat;
    private CancellationTokenSource? cancellation;
    private Task? running;
    private bool quitting, balloonShown, detecting;
    internal bool HasPrimaryConversion => convertButton.IsEnabled;
    internal string? SelectedFormat => selectedFormat;
    internal async Task VerifyPrimaryConversionAsync()
    {
        if (!HasPrimaryConversion) throw new InvalidOperationException("Primary conversion is disabled.");
        convertButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (running != null) await running;
        if (lastOutput == null || !File.Exists(lastOutput)) throw new InvalidOperationException("Primary conversion produced no file.");
    }
    internal LogoIntro Intro { get; }
    public MainWindow(string[] initial) : this(initial, false) { }
    internal MainWindow(string[] initial, bool testMode)
    {
        Title = "miracle · A little file magic"; Width = 1040; Height = Math.Min(810, Math.Max(640, SystemParameters.WorkArea.Height - 35)); MinWidth = 760; MinHeight = 620;
        Background = Theme.Ink; Foreground = Theme.White; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var shell = new Grid(); Content = shell;
        shell.Children.Add(new Image { Source = Theme.Logo(), Stretch = Stretch.UniformToFill, Opacity = .12, IsHitTestVisible = false });
        var page = new Grid { Margin = new Thickness(28, 20, 28, 20) }; shell.Children.Add(page);
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); page.RowDefinitions.Add(new RowDefinition());
        var top = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 22) }; page.Children.Add(top);
        var brand = new StackPanel { Orientation = Orientation.Horizontal }; brand.Children.Add(new Image { Source = Theme.Logo(), Width = 42, Height = 42, Margin = new Thickness(0, 0, 12, 0) }); brand.Children.Add(Theme.Text("miracle", 28, Theme.White, true)); top.Children.Add(brand);
        var badge = Theme.Text("WINDOWS  /  PREVIEW 0.2", 10, Theme.Muted, true); badge.VerticalAlignment = VerticalAlignment.Center; DockPanel.SetDock(badge, Dock.Right); top.Children.Add(badge);
        var body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(225) }); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) }); body.ColumnDefinitions.Add(new ColumnDefinition()); Grid.SetRow(body, 1); page.Children.Add(body);
        var aside = new StackPanel { Margin = new Thickness(0, 4, 0, 0) }; body.Children.Add(new ScrollViewer { Content = aside, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        aside.Children.Add(Theme.Text("SMALL APP.\nBIG SHIFT.", 37, Theme.White, true));
        var mantra = Theme.Text("Your files.\nNew possibilities.", 19, Theme.Muted); mantra.Margin = new Thickness(0, 15, 0, 20); aside.Children.Add(mantra);
        aside.Children.Add(new Image { Source = Theme.Logo(), Width = 210, Height = 210, HorizontalAlignment = HorizontalAlignment.Left });
        var stars = Theme.Text("✦   ✳   ✦", 35, Theme.Acid); stars.Margin = new Thickness(0, 12, 0, 15); aside.Children.Add(stars);
        aside.Children.Add(Theme.Text("100% ON YOUR PC", 11, Theme.Acid, true));
        var privacy = Theme.Text("No uploads. No account.\nOriginals kept safe.", 12, Theme.Muted); privacy.Margin = new Thickness(0, 10, 0, 24); aside.Children.Add(privacy);
        var right = new StackPanel(); var contentScroll = new ScrollViewer { Content = right, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; Grid.SetColumn(contentScroll, 2); body.Children.Add(contentScroll);
        var add = new StackPanel(); add.Children.Add(StepHeading("01", "Bring your files.", Theme.Brush("#99DDFF")));
        var dropContent = new StackPanel(); selection.TextWrapping = TextWrapping.Wrap; dropContent.Children.Add(selection); fileDetail.Margin = new Thickness(0, 9, 0, 0); dropContent.Children.Add(fileDetail);
        var drop = new Border { Child = dropContent, BorderBrush = Theme.Brush("#D1CFC7"), BorderThickness = new Thickness(1), Background = Theme.Brush("#FAF9F4"), CornerRadius = new CornerRadius(13), Padding = new Thickness(16), Margin = new Thickness(0, 14, 0, 14), MinHeight = 90, AllowDrop = true };
        AutomationProperties.SetName(drop, "File drop area");
        drop.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && !Busy ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        drop.Drop += (_, e) => { e.Handled = true; if (e.Data.GetData(DataFormats.FileDrop) is string[] dropped) SetFiles(dropped); };
        add.Children.Add(drop);
        AddButton(fileControls, "＋ Add files", ChooseFiles, Theme.Brush("#99DDFF")); AddButton(fileControls, "Output folder…", ChooseOutput); AddButton(fileControls, "Beside originals", () => { if (!Busy) { outputFolder = null; destination.Text = "Save beside the originals"; } });
        add.Children.Add(fileControls); add.Children.Add(destination); right.Children.Add(Theme.Card(add));
        var pick = new StackPanel(); pick.Children.Add(StepHeading("02", "Pick a new form.", Theme.Brush("#FFB68F")));
        formats.Margin = new Thickness(0, 16, 0, 4); pick.Children.Add(formats);
        var convertRow = new WrapPanel();
        convertButton = Theme.Button("Convert file  ↗", () => { if (selectedFormat != null) BeginConversion(files, selectedFormat); }, Theme.Ink, Theme.White); convertButton.IsEnabled = false; convertButton.MinWidth = 185;
        AutomationProperties.SetName(convertButton, "Convert selected files"); convertRow.Children.Add(convertButton); selectedLabel.Margin = new Thickness(0, 10, 0, 8); convertRow.Children.Add(selectedLabel); pick.Children.Add(convertRow); right.Children.Add(Theme.Card(pick));
        var optional = new StackPanel();
        var wheelButton = Theme.Button("03   Not sure? Use the wheel.  ✦", () => ShowWheel(files), Theme.Brush("#CCB8FF")); wheelButton.FontSize = 18; wheelButton.MinHeight = 58; wheelButton.Margin = new Thickness(0); optional.Children.Add(wheelButton);
        var help = Theme.Text("A colorful detour. Same conversion, your way.", 12, Theme.Muted); help.Margin = new Thickness(0, 12, 0, 0); optional.Children.Add(help); right.Children.Add(Theme.Card(optional, Theme.Panel));
        var progress = new StackPanel(); progress.Children.Add(status); var jobs = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        cancel = Theme.Button("Cancel", () => cancellation?.Cancel()); cancel.IsEnabled = false; reveal = Theme.Button("Show result  ↗", Reveal); reveal.IsEnabled = false; jobs.Children.Add(cancel); jobs.Children.Add(reveal); progress.Children.Add(jobs); right.Children.Add(Theme.Card(progress, Theme.Acid));
        var settings = new StackPanel(); settings.Children.Add(engineStatus); engineControls.Margin = new Thickness(0, 10, 0, 0); AddButton(engineControls, "Locate ffmpeg.exe", ChooseFFmpeg, Theme.Panel, Theme.White); AddButton(engineControls, "Check engines", async () => await DetectAsync(), Theme.Panel, Theme.White); settings.Children.Add(engineControls);
        var shiftToggle = new CheckBox { Content = "Optional: Shift-tap wheel in File Explorer", Foreground = Theme.White, Margin = new Thickness(0, 6, 0, 12), FontSize = 12 };
        shiftToggle.Checked += (_, _) => { try { shiftHook ??= new ExplorerShiftHook(Dispatcher, ShowWheel); shiftHook.Enabled = true; } catch (Exception ex) { status.Text = "Explorer shortcut unavailable: " + ex.Message; shiftToggle.IsChecked = false; } };
        shiftToggle.Unchecked += (_, _) => { if (shiftHook != null) shiftHook.Enabled = false; }; settings.Children.Add(shiftToggle);
        settings.Children.Add(Theme.Text("Images & subtitles built in. Media needs FFmpeg.\nClosing keeps Miracle in the tray. Use its menu to quit.", 11, Theme.Muted)); right.Children.Add(settings);
        if (!testMode)
        {
            tray = new Forms.NotifyIcon { Text = "Miracle for Windows", Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Visible = true };
            var menu = new Forms.ContextMenuStrip(); menu.Items.Add("Open Miracle", null, (_, _) => Dispatcher.Invoke(ShowMain)); menu.Items.Add("Choose files…", null, (_, _) => Dispatcher.Invoke(() => { ShowMain(); ChooseFiles(); })); menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(RequestQuit)); tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMain); Closing += OnClosing;
        }
        Intro = new LogoIntro(); shell.Children.Add(Intro);
        Loaded += async (_, _) => { if (!testMode) { await DetectAsync(); if (initial.Length > 0) SetFiles(initial); } else { engineStatus.Text = "Native images & subtitles ready"; SetFiles(initial); } };
        RefreshFormats();
    }
    private bool Busy => running != null || detecting;
    private static UIElement StepHeading(string number, string title, Brush color, bool dark = false)
    {
        var row = new DockPanel(); var badge = new Border { Background = color, CornerRadius = new CornerRadius(9), Padding = new Thickness(9, 6, 9, 6), Margin = new Thickness(0, 0, 12, 0), Child = Theme.Text(number, 12, Theme.Ink, true) }; row.Children.Add(badge); var text = Theme.Text(title, 21, dark ? Theme.White : Theme.Ink, true); text.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(text); return row;
    }
    private static void AddButton(Panel panel, string text, Action action, Brush? fill = null, Brush? foreground = null) => panel.Children.Add(Theme.Button(text, action, fill, foreground));
    private async Task DetectAsync(string? path = null)
    {
        if (Busy || quitting) return;
        detecting = true; SetControls(); engineStatus.Text = "Checking conversion engines…";
        try { await service.DetectAsync(path); engineStatus.Text = service.EngineStatus; }
        catch (Exception ex) { engineStatus.Text = "Engine check failed: " + ex.Message; }
        finally { detecting = false; RefreshFormats(); SetControls(); }
    }
    private void SetControls() { fileControls.IsEnabled = !Busy; engineControls.IsEnabled = !Busy; formats.IsEnabled = !Busy; convertButton.IsEnabled = !Busy && selectedFormat != null && files.Length > 0; }
    private void ShowMain() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void ChooseFiles()
    {
        if (Busy) return;
        var dialog = new OpenFileDialog { Multiselect = true, Title = "Choose files to convert" };
        if (dialog.ShowDialog(this) == true) SetFiles(dialog.FileNames);
    }
    private void ChooseOutput()
    {
        if (Busy) return;
        var dialog = new OpenFolderDialog { Title = "Save converted files in…" };
        if (dialog.ShowDialog(this) == true) { outputFolder = dialog.FolderName; destination.Text = "Save to " + outputFolder; }
    }
    private async void ChooseFFmpeg()
    {
        if (Busy) return;
        var dialog = new OpenFileDialog { Filter = "FFmpeg executable|ffmpeg.exe", Title = "Choose a trusted FFmpeg executable" };
        if (dialog.ShowDialog(this) == true) await DetectAsync(dialog.FileName);
    }
    internal void SetFiles(string[] input)
    {
        if (Busy) return;
        try
        {
            if (input.Length > 500) throw new InvalidOperationException("Choose at most 500 files per batch.");
            files = input.Where(File.Exists).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            selectedFormat = null;
            selection.Text = files.Length == 0 ? "Drop something good here." : files.Length == 1 ? Path.GetFileName(files[0]) : files.Length + " files, ready for a change.";
            fileDetail.Text = files.Length == 0 ? "Choose files or drag them into this space." : string.Join(" · ", files.Take(3).Select(Path.GetFileName));
            RefreshFormats();
        }
        catch (Exception ex) { status.Text = "Could not select files: " + ex.Message; }
    }
    private void RefreshFormats()
    {
        formats.Children.Clear();
        var options = service.CommonOutputs(files);
        if (selectedFormat != null && !options.Contains(selectedFormat)) selectedFormat = null;
        foreach (string format in options)
        {
            var button = Theme.Button(format.ToUpperInvariant() + (selectedFormat == format ? "  ✓" : "  ↗"), () => SelectFormat(format), Theme.FormatColor(format));
            button.Width = 130; button.Height = 70; button.FontSize = 19; button.BorderThickness = new Thickness(selectedFormat == format ? 3 : 1); AutomationProperties.SetName(button, "Select " + format.ToUpperInvariant()); formats.Children.Add(button);
        }
        if (options.Count == 0) formats.Children.Add(Theme.Text(files.Length == 0 ? "Your options appear as soon as you add files." : "No shared output for this selection. Try files of the same kind, or add FFmpeg for media.", 13, Brushes.DimGray));
        selectedLabel.Text = selectedFormat == null ? "Choose a format first" : "→ " + selectedFormat.ToUpperInvariant() + "  /  " + files.Length + " file(s)";
        convertButton.Content = files.Length > 1 ? "Convert " + files.Length + " files  ↗" : "Convert file  ↗"; SetControls();
    }
    internal void SelectFormat(string format) { if (Busy || !service.CommonOutputs(files).Contains(format)) return; selectedFormat = format; RefreshFormats(); }
    private void ShowWheel(string[] input)
    {
        if (Busy || quitting) return;
        SetFiles(input);
        var options = service.CommonOutputs(files);
        if (options.Count == 0) { ShowMain(); status.Text = "No shared conversion for this selection. Choose a supported file or configure FFmpeg."; return; }
        wheel?.Close();
        wheel = new WheelWindow(files, options, BeginConversion); wheel.Show();
    }
    private async void BeginConversion(string[] input, string format)
    {
        if (Busy || quitting) return;
        var batch = input.ToArray();
        if (batch.Length == 0 || batch.Any(f => !service.Outputs(f).Contains(format))) { status.Text = "This format is unavailable for one or more selected files."; return; }
        ShowMain(); cancellation = new CancellationTokenSource(); cancel.IsEnabled = true; formats.IsEnabled = false; convertButton.IsEnabled = false; fileControls.IsEnabled = false; engineControls.IsEnabled = false;
        running = ConvertBatchAsync(batch, format, outputFolder, cancellation.Token);
        try { await running; }
        finally { running = null; cancellation.Dispose(); cancellation = null; cancel.IsEnabled = false; SetControls(); }
    }
    private async Task ConvertBatchAsync(string[] batch, string format, string? folder, CancellationToken token)
    {
        int complete = 0;
        try
        {
            foreach (string input in batch)
            {
                status.Text = "Converting " + (complete + 1) + "/" + batch.Length + ": " + Path.GetFileName(input);
                lastOutput = await service.ConvertAsync(input, format, folder, token);
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
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) { UseShellExecute = false }; start.ArgumentList.Add("/select,"); start.ArgumentList.Add(lastOutput); Process.Start(start);
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (quitting) return;
        e.Cancel = true; Hide();
        if (!balloonShown) { balloonShown = true; tray?.ShowBalloonTip(2000, "Miracle is still running", "Open or quit Miracle from its system tray icon.", Forms.ToolTipIcon.Info); }
    }
    private async void RequestQuit()
    {
        if (quitting) return;
        quitting = true; shiftHook?.Dispose(); wheel?.Close(); cancellation?.Cancel();
        if (running != null) await running;
        if (tray != null) { tray.Visible = false; tray.Dispose(); } Application.Current.Shutdown();
    }
}
