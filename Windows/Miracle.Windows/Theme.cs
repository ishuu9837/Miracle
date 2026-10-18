using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Miracle.Windows;
internal static class Theme
{
    public static readonly SolidColorBrush Ink = Brush("#111113"), Paper = Brush("#F3F0E8"), Muted = Brush("#ADA9B8"), Acid = Brush("#E3FF65"), White = Brush("#FFFFFF"), Panel = Brush("#212126");
    private static readonly string[] Palette = { "#FFB68F", "#99DDFF", "#CCB8FF", "#E3FF65", "#FF9DC7", "#90E5CB", "#FFE38A" };
    public static SolidColorBrush Brush(string hex) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); brush.Freeze(); return brush; }
    public static SolidColorBrush FormatColor(string format) { string[] known = { "jpg", "png", "bmp", "tiff", "webp", "mp3", "wav" }; int i = Array.IndexOf(known, format); if (i < 0) { i = 0; foreach (char c in format) i += c; } return Brush(Palette[i % Palette.Length]); }
    public static BitmapImage Logo() { var image = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon.png")); image.Freeze(); return image; }
    public static TextBlock Text(string text, double size = 13, Brush? color = null, bool bold = false) => new() { Text = text, FontSize = size, Foreground = color ?? Ink, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap };
    public static Button Button(string label, Action action, Brush? fill = null, Brush? foreground = null)
    {
        var button = new Button { Content = label, Background = fill ?? Paper, Foreground = foreground ?? Ink, BorderBrush = Ink, BorderThickness = new Thickness(1), Padding = new Thickness(16, 11, 16, 11), Margin = new Thickness(0, 0, 10, 10), FontSize = 13, FontWeight = FontWeights.SemiBold, Cursor = System.Windows.Input.Cursors.Hand };
        var frame = new FrameworkElementFactory(typeof(Border)); frame.SetValue(Border.CornerRadiusProperty, new CornerRadius(13)); frame.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty)); frame.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty)); frame.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter)); content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center); content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty)); frame.AppendChild(content);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = frame };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true }; hover.Setters.Add(new Setter(UIElement.OpacityProperty, .86)); template.Triggers.Add(hover);
        var pressed = new Trigger { Property = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, Value = true }; pressed.Setters.Add(new Setter(UIElement.OpacityProperty, .65)); template.Triggers.Add(pressed);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false }; disabled.Setters.Add(new Setter(UIElement.OpacityProperty, .35)); template.Triggers.Add(disabled);
        button.Template = template; button.Click += (_, _) => action(); return button;
    }
    public static Border Card(UIElement child, Brush? color = null) => new() { Child = child, Background = color ?? Paper, Padding = new Thickness(22), CornerRadius = new CornerRadius(20), Margin = new Thickness(0, 0, 0, 14) };
}
