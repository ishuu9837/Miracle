using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace Miracle.Windows;

// Only a short, unmodified Shift tap in the foreground Explorer window triggers selection.
// Keyboard content is never collected or persisted, and events are never swallowed.
public sealed class ExplorerShiftHook : IDisposable
{
    private readonly HookProc callback;
    private readonly Dispatcher dispatcher;
    private readonly Action<string[]> selection;
    private IntPtr hook;
    private bool shiftHeld, chord;
    private long started;
    public bool Enabled { get; set; }
    public ExplorerShiftHook(Dispatcher dispatcher, Action<string[]> selection)
    {
        this.dispatcher = dispatcher; this.selection = selection; callback = Handle;
        hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
    private IntPtr Handle(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && Enabled)
        {
            int key = Marshal.ReadInt32(data); long msg = message.ToInt64();
            bool down = msg is 0x100 or 0x104, up = msg is 0x101 or 0x105;
            if (key is 0x10 or 0xA0 or 0xA1)
            {
                if (down && !shiftHeld) { shiftHeld = true; chord = false; started = Environment.TickCount64; }
                if (up && shiftHeld)
                {
                    shiftHeld = false;
                    if (!chord && Environment.TickCount64 - started < 650 && !ModifierPressed())
                    {
                        IntPtr foreground = GetForegroundWindow();
                        var name = new StringBuilder(256); GetClassName(foreground, name, name.Capacity);
                        if (name.ToString() == "CabinetWClass") dispatcher.BeginInvoke(() => ReadSelection(foreground));
                    }
                }
            }
            else if (down && shiftHeld) chord = true;
        }
        return CallNextHookEx(hook, code, message, data);
    }
    private static bool ModifierPressed() => (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
    private void ReadSelection(IntPtr foreground)
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
                    if (files.Count > 0) selection(files.ToArray());
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
