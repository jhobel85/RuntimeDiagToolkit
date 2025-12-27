using DiagnosticsToolkit.Maui.Extensions;
using DiagnosticsToolkit.Maui.Sample.ViewModels;
using Microsoft.Extensions.Logging;

namespace DiagnosticsToolkit.Maui.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // Configure base sampling interval; Android adapts in background
            .UseDiagnosticsToolkit(o => o.BaseInterval = TimeSpan.FromMilliseconds(250));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Pages & ViewModels
        builder.Services.AddSingleton<DiagnosticsViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
