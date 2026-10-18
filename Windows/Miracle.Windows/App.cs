using System;
using System.Windows;

namespace Miracle.Windows;
public sealed class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test") { Environment.ExitCode = SelfTest.RunAsync(args[1]).GetAwaiter().GetResult(); return; }
        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new MainWindow(args);
        app.MainWindow = window;
        window.Show();
        app.Run();
    }
}
