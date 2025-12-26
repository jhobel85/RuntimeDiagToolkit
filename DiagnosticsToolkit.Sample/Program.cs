using DiagnosticsToolkit.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register runtime diagnostics toolkit
builder.Services.AddDiagnosticsToolkit();

var app = builder.Build();

// Basic health endpoint
app.MapGet("/", () => "DiagnosticsToolkit sample is running.");

// Expose runtime metrics at /_diagnostics/runtime
app.MapRuntimeDiagnostics();

app.Run();
