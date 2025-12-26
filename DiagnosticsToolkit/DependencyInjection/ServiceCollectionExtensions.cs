using DiagnosticsToolkit.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace DiagnosticsToolkit.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers runtime diagnostics services and a platform-specific metrics provider.
    /// </summary>
    public static IServiceCollection AddRuntimeDiagnostics(this IServiceCollection services)
    {
        // Register the runtime metrics provider via factory
        services.AddSingleton<IRuntimeMetricsProvider>(_ => RuntimeMetricsProviderFactory.Create());
        return services;
    }
}
