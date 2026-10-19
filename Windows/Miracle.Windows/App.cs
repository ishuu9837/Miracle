using System;
using System.Windows;

namespace Miracle.Windows;
public sealed class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        NativePdfRenderer.Shutdown();base.OnExit(e);
    }
    // Exercise feature checks under the same dispatcher as the desktop app.
    private static int RunWithDispatcher(Func<System.Threading.Tasks.Task<int>> operation)
    {
        var application=new App{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        application.Startup+=async(_,_)=>
        {
            int code=1;
            try{code=await operation();}
            catch(Exception ex){Console.Error.WriteLine(ex);}
            finally{application.Shutdown(code);}
        };
        return application.Run();
    }
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
        if (args.Length == 3 && args[0] == "--engine-test") { Environment.ExitCode = RunWithDispatcher(() => FeatureTest.EngineSmokeAsync(args[1],args[2])); return; }
        if (args.Length == 2 && args[0] == "--feature-test") { Environment.ExitCode = RunWithDispatcher(() => FeatureTest.RunAsync(args[1])); return; }
        if (args.Length == 2 && args[0] == "--self-test") { Environment.ExitCode = SelfTest.RunAsync(args[1]).GetAwaiter().GetResult(); return; }
        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new MainWindow(args);
        app.MainWindow = window;
        window.Show();
        app.Run();
    }
}
