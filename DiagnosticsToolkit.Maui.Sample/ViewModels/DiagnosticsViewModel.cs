using System.ComponentModel;
using System.Runtime.CompilerServices;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Providers;

namespace DiagnosticsToolkit.Maui.Sample.ViewModels;

public sealed class DiagnosticsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IRuntimeMetricsProvider _metrics;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private PeriodicTimer? _timer;

    private double _refreshIntervalMs = 500;
    private bool _autoRefresh;
    private bool _isBackground;

    private string? _cpuText;
    private string? _memText;
    private string? _gcText;
    private string? _tpText;
    private double _currentSamplingIntervalMs;

    public DiagnosticsViewModel(IRuntimeMetricsProvider metrics)
    {
        _metrics = metrics;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public double RefreshIntervalMs
    {
        get => _refreshIntervalMs;
        set
        {
            if (Math.Abs(_refreshIntervalMs - value) < 0.1) return;
            _refreshIntervalMs = value;
            OnPropertyChanged();
            ApplyProviderInterval();
            RestartTimerIfNeeded();
        }
    }

    public bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            if (_autoRefresh == value) return;
            _autoRefresh = value;
            OnPropertyChanged();
            if (_autoRefresh) Start(); else Stop();
        }
    }

    public bool IsBackground
    {
        get => _isBackground;
        set
        {
            if (_isBackground == value) return;
            _isBackground = value;
            OnPropertyChanged();
            ToggleProviderBackground(_isBackground);
        }
    }

    public string? CpuText { get => _cpuText; private set { _cpuText = value; OnPropertyChanged(); } }
    public string? MemText { get => _memText; private set { _memText = value; OnPropertyChanged(); } }
    public string? GcText { get => _gcText; private set { _gcText = value; OnPropertyChanged(); } }
    public string? TpText { get => _tpText; private set { _tpText = value; OnPropertyChanged(); } }

    public double CurrentSamplingIntervalMs { get => _currentSamplingIntervalMs; private set { _currentSamplingIntervalMs = value; OnPropertyChanged(); } }

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(1, _refreshIntervalMs)));
        _loop = Task.Run(async () =>
        {
            try
            {
                while (_cts!.IsCancellationRequested == false && await _timer!.WaitForNextTickAsync(_cts.Token))
                {
                    await SampleOnceAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        });
    }

    public void Stop()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _cts?.Dispose();
        _cts = null;
        _timer = null;
        _loop = null;
    }

    private async Task SampleOnceAsync()
    {
        // Fetch CPU & Memory each tick; GC/ThreadPool less frequently if desired
        var cpu = await _metrics.GetCpuUsageAsync();
        var mem = await _metrics.GetMemorySnapshotAsync();
        var gc = await _metrics.GetGcStatsAsync();
        var tp = await _metrics.GetThreadPoolStatsAsync();

        CpuText = $"CPU: {cpu.PercentageUsed:F1}% | Procs: {cpu.ProcessorCount} | Total: {cpu.TotalProcessorTimeMs} ms";
        MemText = $"MEM: WS={mem.ProcessWorkingSetBytes:n0} B | Private={mem.ProcessPrivateMemoryBytes:n0} B | Pressure={mem.MemoryPressurePercentage}%";
        GcText = $"GC: G0={gc.Gen0CollectionCount}, G1={gc.Gen1CollectionCount}, G2={gc.Gen2CollectionCount} | Pause%={gc.TotalGcPauseMsPercentage:F2}";
        TpText = $"TP: Workers={tp.WorkerThreadCount} (avail {tp.AvailableWorkerThreads}) | Queue={tp.QueuedWorkItemCount} | Done={tp.CompletedWorkItemCount}";

        // Read adaptive backoff (Android only)
        if (_metrics is DiagnosticsToolkit.Providers.Platforms.DefaultAndroidMetricsProvider android)
        {
            CurrentSamplingIntervalMs = android.GetCurrentSamplingInterval().TotalMilliseconds;
            // Keep ViewModel IsBackground in sync if necessary
            var bg = android.IsBackground;
            if (bg != _isBackground)
            {
                _isBackground = bg;
                OnPropertyChanged(nameof(IsBackground));
            }
        }
        else
        {
            // For non-Android, reflect the configured refresh interval
            CurrentSamplingIntervalMs = _refreshIntervalMs;
        }
    }

    private void ApplyProviderInterval()
    {
        if (_metrics is DiagnosticsToolkit.Providers.Platforms.DefaultAndroidMetricsProvider android)
        {
            android.SetSamplingInterval(TimeSpan.FromMilliseconds(Math.Max(1, _refreshIntervalMs)));
        }
        else if (_metrics is DiagnosticsToolkit.Providers.Platforms.DefaultAppleMetricsProvider apple)
        {
            apple.SetSamplingInterval(TimeSpan.FromMilliseconds(Math.Max(1, _refreshIntervalMs)));
        }
    }

    private void ToggleProviderBackground(bool isBackground)
    {
        if (_metrics is DiagnosticsToolkit.Providers.Platforms.DefaultAndroidMetricsProvider android)
        {
            if (isBackground) android.OnAppBackgrounded(); else android.OnAppForegrounded();
        }
        else if (_metrics is DiagnosticsToolkit.Providers.Platforms.DefaultAppleMetricsProvider apple)
        {
            if (isBackground) apple.OnAppBackgrounded(); else apple.OnAppForegrounded();
        }
    }

    private void RestartTimerIfNeeded()
    {
        if (_autoRefresh)
        {
            Stop();
            Start();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => Stop();
}
