using System;
using DiagnosticsToolkit.DependencyInjection;
using DiagnosticsToolkit.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace DiagnosticsToolkit.Maui.Extensions;

/// <summary>
/// Extension methods to wire DiagnosticsToolkit into a MAUI app.
/// </summary>
public static class MauiDiagnosticsExtensions
{
    /// <summary>
    /// Registers runtime diagnostics services for MAUI apps.
    /// Usage: builder.UseDiagnosticsToolkit();
    /// </summary>
    public static MauiAppBuilder UseDiagnosticsToolkit(this MauiAppBuilder builder, Action<MobileSamplingOptions>? configure = null)
    {
        builder.Services.AddRuntimeDiagnostics();
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android =>
            {
                android.OnStart(activity => Foreground());
                android.OnResume(activity => Foreground());
                android.OnPause(activity => Background());
                android.OnStop(activity => Background());
            });
#endif
#if IOS
            events.AddiOS(ios =>
            {
                ios.WillEnterForeground(app => Foreground());
                ios.OnActivated(app => Foreground());
                ios.DidEnterBackground(app => Background());
                ios.OnResignActivation(app => Background());
            });
#endif
#if MACCATALYST
            events.AddMacCatalyst(mac =>
            {
                mac.WillEnterForeground(app => Foreground());
                mac.OnActivated(app => Foreground());
                mac.DidEnterBackground(app => Background());
                mac.OnResignActivation(app => Background());
            });
#else
    // Handle other platforms
#endif
        });

        return builder;

        static void Foreground()
        {
            var provider = ResolveMetricsProvider();
            if (provider is null)
            {
                return;
            }

#if ANDROID
            if (provider is DiagnosticsToolkit.Providers.Platforms.DefaultAndroidMetricsProvider android)
            {
                android.OnAppForegrounded();
                var opts = ResolveOptions();
                if (opts?.BaseInterval is TimeSpan interval && interval > TimeSpan.Zero)
                {
                    android.SetSamplingInterval(interval);
                }
            }
#endif
#if IOS || MACCATALYST
            if (provider is DiagnosticsToolkit.Providers.Platforms.DefaultAppleMetricsProvider apple)
            {
                apple.OnAppForegrounded();
                var opts = ResolveOptions();
                if (opts?.BaseInterval is TimeSpan interval && interval > TimeSpan.Zero)
                {
                    apple.SetSamplingInterval(interval);
                }
            }
#endif
        }

        static void Background()
        {
            var provider = ResolveMetricsProvider();
            if (provider is null)
            {
                return;
            }

#if ANDROID
            if (provider is DiagnosticsToolkit.Providers.Platforms.DefaultAndroidMetricsProvider android)
            {
                android.OnAppBackgrounded();
                var opts = ResolveOptions();
                if (opts?.BaseInterval is TimeSpan interval && interval > TimeSpan.Zero)
                {
                    android.SetSamplingInterval(interval);
                }
            }
#endif
#if IOS || MACCATALYST
            if (provider is DiagnosticsToolkit.Providers.Platforms.DefaultAppleMetricsProvider apple)
            {
                apple.OnAppBackgrounded();
                var opts = ResolveOptions();
                if (opts?.BaseInterval is TimeSpan interval && interval > TimeSpan.Zero)
                {
                    apple.SetSamplingInterval(interval);
                }
            }
#endif
        }

        static IRuntimeMetricsProvider? ResolveMetricsProvider()
        {
            var services = ResolveServices();
            return services?.GetService<IRuntimeMetricsProvider>();
        }

        static MobileSamplingOptions? ResolveOptions()
        {
            var services = ResolveServices();
            return services?.GetService<IOptions<MobileSamplingOptions>>()?.Value;
        }

        static IServiceProvider? ResolveServices()
        {
#if ANDROID
            return Microsoft.Maui.MauiApplication.Current?.Services;
#elif IOS
            return Microsoft.Maui.MauiUIApplicationDelegate.Current?.Services;
#elif MACCATALYST
            return Microsoft.Maui.MauiUIApplicationDelegate.Current?.Services;
#else
            // Replace obsolete references
            // Old usage
            // var services = MauiUIApplicationDelegate.Services;
            // New usage
            var services = IPlatformApplication.Current.Services;
#endif
        }
    }
}
