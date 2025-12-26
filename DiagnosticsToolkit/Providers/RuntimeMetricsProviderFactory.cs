namespace DiagnosticsToolkit.Providers;

/// <summary>
/// Factory for creating platform-specific runtime metrics providers.
/// </summary>
public static class RuntimeMetricsProviderFactory
{
    /// <summary>
    /// Creates a platform-appropriate runtime metrics provider.
    /// </summary>
    /// <returns>An implementation of IRuntimeMetricsProvider for the current platform.</returns>
    public static IRuntimeMetricsProvider Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsProvider();
        }
        else if (OperatingSystem.IsLinux())
        {
            return CreateLinuxProvider();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return CreateMacOsProvider();
        }
        else if (OperatingSystem.IsAndroid())
        {
            return CreateAndroidProvider();
        }
        else if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
        {
            return CreateIosProvider();
        }

        throw new PlatformNotSupportedException("Current platform is not supported.");
    }

    private static IRuntimeMetricsProvider CreateWindowsProvider()
    {
        // Create Windows provider; Windows-specific interop is guarded inside the implementation.
        return new Platforms.DefaultWindowsMetricsProvider();
    }

    private static IRuntimeMetricsProvider CreateLinuxProvider()
    {
        throw new NotImplementedException("Linux provider is not yet implemented.");
    }

    private static IRuntimeMetricsProvider CreateMacOsProvider()
    {
        throw new NotImplementedException("macOS provider is not yet implemented.");
    }

    private static IRuntimeMetricsProvider CreateAndroidProvider()
    {
        throw new NotImplementedException("Android provider is not yet implemented.");
    }

    private static IRuntimeMetricsProvider CreateIosProvider()
    {
        throw new NotImplementedException("iOS provider is not yet implemented.");
    }
}
