#if IOS || MACCATALYST
using Microsoft.Maui;
using UIKit;

namespace DiagnosticsToolkit.Maui.Sample;

/// <summary>
/// Program.cs must be there becouse of the way iOS apps are started. 
/// Android apps can work without it but iOS apps need this entry point.
/// </summary>
public class Program : MauiUIApplicationDelegate
{
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
#endif