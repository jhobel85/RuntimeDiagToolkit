using DiagnosticsToolkit.AspNetCore.Extensions;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiagnosticsToolkit.Tests;

public class DiagnosticsToolkitDependencyInjectionTests
{
    [Fact]
    public void AddDiagnosticsToolkit_RegistersProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDiagnosticsToolkit();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetRequiredService<IRuntimeMetricsProvider>();
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddDiagnosticsToolkit_ProviderIsNotNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDiagnosticsToolkit();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var provider = serviceProvider.GetRequiredService<IRuntimeMetricsProvider>();

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<IRuntimeMetricsProvider>(provider);
    }

    [Fact]
    public async Task RegisteredProvider_CanCollectMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDiagnosticsToolkit();
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IRuntimeMetricsProvider>();

        // Act
        var cpu = await provider.GetCpuUsageAsync();
        var memory = await provider.GetMemorySnapshotAsync();
        var gc = await provider.GetGcStatsAsync();
        var threadPool = await provider.GetThreadPoolStatsAsync();

        // Assert
        Assert.NotEqual(default(CpuUsage), cpu);
        Assert.NotEqual(default(MemorySnapshot), memory);
        Assert.NotEqual(default(GcStats), gc);
        Assert.NotEqual(default(ThreadPoolStats), threadPool);
    }
}
