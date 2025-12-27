using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Providers;
using DiagnosticsToolkit.Maui.Sample.ViewModels;

namespace DiagnosticsToolkit.Maui.Sample;

public partial class MainPage : ContentPage
{
    private readonly IRuntimeMetricsProvider _metrics;
    private readonly DiagnosticsViewModel _vm;
    private bool _isRunning = true;

    public MainPage(IRuntimeMetricsProvider metrics, DiagnosticsViewModel vm)
    {
        InitializeComponent();
        _metrics = metrics;
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Auto-start periodic refresh
        _isRunning = true;
        _vm.AutoRefresh = true;
        PauseButton.Text = "Pause";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop refresh when page closes
        _vm.AutoRefresh = false;
        _isRunning = false;
    }

    private void OnTogglePause(object sender, EventArgs e)
    {
        _isRunning = !_isRunning;
        _vm.AutoRefresh = _isRunning;
        PauseButton.Text = _isRunning ? "Pause" : "Resume";
    }

    private async void OnCpuClicked(object sender, EventArgs e)
    {
        var cpu = await _metrics.GetCpuUsageAsync();
        Append($"CPU: {cpu.PercentageUsed:F1}% | Procs: {cpu.ProcessorCount} | Total: {cpu.TotalProcessorTimeMs} ms");
    }

    private async void OnMemClicked(object sender, EventArgs e)
    {
        var mem = await _metrics.GetMemorySnapshotAsync();
        Append($"MEM: WS={mem.ProcessWorkingSetBytes:n0} B | Private={mem.ProcessPrivateMemoryBytes:n0} B | Pressure={mem.MemoryPressurePercentage}%");
    }

    private async void OnGcClicked(object sender, EventArgs e)
    {
        var gc = await _metrics.GetGcStatsAsync();
        Append($"GC: G0={gc.Gen0CollectionCount}, G1={gc.Gen1CollectionCount}, G2={gc.Gen2CollectionCount} | Pause%={gc.TotalGcPauseMsPercentage:F2}");
    }

    private async void OnTpClicked(object sender, EventArgs e)
    {
        var tp = await _metrics.GetThreadPoolStatsAsync();
        Append($"TP: Workers={tp.WorkerThreadCount} (avail {tp.AvailableWorkerThreads}) | Queue={tp.QueuedWorkItemCount} | Done={tp.CompletedWorkItemCount}");
    }

    private void Append(string line)
    {
        var ts = DateTimeOffset.Now.ToString("T");
        Output.Text = string.IsNullOrEmpty(Output.Text)
            ? $"[{ts}] {line}"
            : Output.Text + Environment.NewLine + $"[{ts}] {line}";
    }
}
