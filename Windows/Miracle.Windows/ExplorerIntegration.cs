using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace Miracle.Windows;

// Observe short Shift taps without swallowing input. Explorer remains the drag source.
// Keyboard content is never collected or persisted, and events are never swallowed.
public sealed class ExplorerShiftHook : IDisposable
{
    private readonly HookProc callback;
    private readonly Dispatcher dispatcher;
    private readonly Action<string[], bool> selection;
    private readonly Func<bool> cycle;
    private readonly Action dismiss;
    private IntPtr hook;
    private readonly ShiftTapGesture gesture = new();
    private bool enabled;
    public bool Enabled { get => enabled; set { enabled = value; gesture.Reset(); } }
    public ExplorerShiftHook(Dispatcher dispatcher, Action<string[], bool> selection, Func<bool> cycle, Action dismiss)
    {
        this.dispatcher = dispatcher; this.selection = selection; this.cycle = cycle; this.dismiss = dismiss; callback = Handle;
        hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
    private IntPtr Handle(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && Enabled)
        {
            int key = Marshal.ReadInt32(data); long msg = message.ToInt64();
            bool down = msg is 0x100 or 0x104, up = msg is 0x101 or 0x105;
            // Ignore synthetic key injection; no text/key history is collected.
            if ((Marshal.ReadInt32(data, 8) & 0x10) == 0)
            {
                if (down && key == 0x1B) dispatcher.BeginInvoke(dismiss);
                if (gesture.Feed(key, down, up, Environment.TickCount64, ModifierPressed(), LeftButtonDown))
                {
                    IntPtr foreground = GetForegroundWindow();
                    var name = new StringBuilder(256); GetClassName(foreground, name, name.Capacity);
                    bool drag = gesture.WasDragging;
                    dispatcher.BeginInvoke(() => { if (!Enabled) return; if (cycle()) return; if (name.ToString() == "CabinetWClass") ReadSelection(foreground, drag); });
                }
            }
        }
        return CallNextHookEx(hook, code, message, data);
    }
    private static bool ModifierPressed() => (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
    internal static bool LeftButtonDown => (GetAsyncKeyState(1) & 0x8000) != 0;
    private void ReadSelection(IntPtr foreground, bool drag)
    {
        if (!Enabled || GetForegroundWindow() != foreground) return;
        object? shell = null, windows = null;
        try
        {
            var type = Type.GetTypeFromProgID("Shell.Application");
            if (type == null) return;
            shell = Activator.CreateInstance(type);
            windows = ((dynamic)shell!).Windows();
            int count = ((dynamic)windows).Count;
            for (int i = 0; i < count; i++)
            {
                object? window = null, document = null, items = null;
                try
                {
                    window = ((dynamic)windows).Item(i);
                    if (window == null || new IntPtr(Convert.ToInt64(((dynamic)window).HWND)) != foreground) continue;
                    document = ((dynamic)window).Document;
                    items = ((dynamic)document).SelectedItems();
                    var files = new List<string>();
                    for (int j = 0; j < (int)((dynamic)items).Count; j++)
                    {
                        object? item = null;
                        try { item = ((dynamic)items).Item(j); string path = ((dynamic)item).Path; if (File.Exists(path)) files.Add(path); }
                        finally { Release(item); }
                    }
                    if (files.Count > 0) selection(files.ToArray(), drag);
                    break;
                }
                finally { Release(items); Release(document); Release(window); }
            }
        }
        catch (Exception ex) when (ex is COMException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or InvalidCastException) { Debug.WriteLine("Explorer selection unavailable: " + ex.Message); }
        finally { Release(windows); Release(shell); }
    }
    private static void Release(object? value) { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
    public void Dispose() { Enabled = false; if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } }
    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? module);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int maximum);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
}

// A tap fires only on the final Shift release, never on auto-repeat or a chord.
internal sealed class ShiftTapGesture
{
    private readonly HashSet<int> held = new();
    private long started;
    private bool chord, dragging;
    internal bool WasDragging { get; private set; }
    internal void Reset() { held.Clear(); chord = false; dragging = false; WasDragging = false; }
    internal bool Feed(int key, bool down, bool up, long now, bool modified, bool mouseDown)
    {
        if (key is not (0x10 or 0xA0 or 0xA1)) { if (down && held.Count > 0) chord = true; return false; }
        if (down) {
            if (held.Count == 0) { started = now; chord = modified; dragging = mouseDown; }
            held.Add(key); return false;
        }
        if (!up || !held.Remove(key) || held.Count != 0) return false;
        WasDragging = dragging && mouseDown;
        return !chord && !modified && now - started < 650;
    }
}
