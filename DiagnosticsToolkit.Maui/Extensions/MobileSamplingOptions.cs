using System;

namespace DiagnosticsToolkit.Maui.Extensions;

/// <summary>
/// Options to control mobile sampling cadence and throttling via MAUI integration.
/// </summary>
public sealed class MobileSamplingOptions
{
    /// <summary>
    /// Base sampling interval used while app is in foreground.
    /// When backgrounded, Android provider applies adaptive backoff.
    /// </summary>
    public TimeSpan? BaseInterval { get; set; }
}
