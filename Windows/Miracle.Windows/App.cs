using System;
using System.Windows;

namespace Miracle.Windows;
public sealed class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--process-fixture")
        {
            if (args.Length > 1 && args[1] == "flood") { Console.Out.Write(new string('o', 200000)); Console.Error.Write(new string('e', 200000)); return; }
            if (args.Length > 1 && args[1] == "wait") { if (args.Length > 2) System.IO.File.WriteAllText(args[2], Environment.ProcessId.ToString()); System.Threading.Thread.Sleep(30000); return; }
            if (args.Length > 1 && args[1] == "fail") { Console.Error.Write("Intentional test failure"); Environment.ExitCode = 7; return; }
            Console.Out.Write(args.Length > 1 ? args[1] : ""); return;
        }
        if (args.Length == 2 && args[0] == "--self-test") { Environment.ExitCode = SelfTest.RunAsync(args[1]).GetAwaiter().GetResult(); return; }
        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new MainWindow(args);
        app.MainWindow = window;
        window.Show();
        app.Run();
    }
}
