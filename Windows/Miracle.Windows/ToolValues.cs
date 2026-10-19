using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Miracle.Windows;
internal sealed class ToolValues
{
    private readonly IReadOnlyDictionary<string, string> values;
    public ToolValues(IReadOnlyDictionary<string,string>? values = null) { this.values = values ?? new Dictionary<string,string>(); }
    public string Text(string key, string fallback = "") { string value = values.TryGetValue(key, out var v) ? v : fallback; if (value.Length > 16000) throw new ArgumentException("Text is too long."); return value; }
    public string Required(string key) { string value = Text(key); if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(key + " is required."); return value; }
    public bool Flag(string key) => Text(key) == "true";
    public double Number(string key, double fallback, double minimum, double maximum)
    {
        string raw = Text(key); if (string.IsNullOrWhiteSpace(raw)) return Math.Clamp(fallback, minimum, maximum);
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number) || number < minimum || number > maximum) throw new ArgumentException(key + " must be between " + minimum + " and " + maximum + "."); return number;
    }
    public string Color(string key, string fallback) { string value = Text(key, fallback); if (!Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$")) throw new ArgumentException("Use a six-digit color such as #FFAA00."); return value; }
    public static string Invariant(double number) => number.ToString("0.######", CultureInfo.InvariantCulture);
}
