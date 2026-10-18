using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Miracle.Windows;
internal sealed class LogoIntro : Border
{
    private bool dismissed;
    internal event Action? Dismissed;
    public LogoIntro()
    {
        Background = Theme.Ink;
        var layout = new Grid(); Child = layout;
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var logo = new Image { Source = Theme.Logo(), Width = 200, Height = 200, RenderTransformOrigin = new Point(.5, .5) };
        var transforms = new TransformGroup(); var scale = new ScaleTransform(1, 1); var turn = new RotateTransform(0); transforms.Children.Add(scale); transforms.Children.Add(turn); logo.RenderTransform = transforms;
        stack.Children.Add(logo); stack.Children.Add(Theme.Text("A LITTLE FILE MAGIC.", 16, Theme.White, true));
        var note = Theme.Text("Made for your files. Made to stay on your device.", 12, Theme.Muted); note.Margin = new Thickness(0, 15, 0, 0); stack.Children.Add(note);
        var track = new Border { Background = Theme.Panel, Height = 4, Width = 300, Margin = new Thickness(0, 28, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        var progress = new Border { Background = Theme.Acid, Height = 4, Width = 0, HorizontalAlignment = HorizontalAlignment.Left }; track.Child = progress; stack.Children.Add(track); layout.Children.Add(stack);
        var skip = Theme.Button("Skip intro  ↗", Finish, Theme.Panel, Theme.White); skip.HorizontalAlignment = HorizontalAlignment.Right; skip.VerticalAlignment = VerticalAlignment.Bottom; skip.Margin = new Thickness(24); layout.Children.Add(skip);
        Loaded += async (_, _) =>
        {
            if (!SystemParameters.ClientAreaAnimation) { Finish(); return; }
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.7, 1, TimeSpan.FromSeconds(.9)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.7, 1, TimeSpan.FromSeconds(.9)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            turn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-12, 0, TimeSpan.FromSeconds(.9)));
            logo.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.7)));
            progress.BeginAnimation(WidthProperty, new DoubleAnimation(0, 300, TimeSpan.FromSeconds(1.15)));
            await Task.Delay(1350);
            if (dismissed) return;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(.22)); fade.Completed += (_, _) => Finish(); BeginAnimation(OpacityProperty, fade);
        };
        PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) { e.Handled = true; Finish(); } };
    }
    internal void Finish() { if (dismissed) return; dismissed = true; Visibility = Visibility.Collapsed; IsHitTestVisible = false; Dismissed?.Invoke(); }
}
