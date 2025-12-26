the ASP.NET Core project adds a Microsoft.AspNetCore.App framework reference and web dependencies by design (web integration)

**Examples where DiagnosticToolkit.AspNetCore can be used/referenced**

**API: add DI + diagnostics endpoint (sample: DiagnosticsToolkit.AspNetCore.API)**
Code:
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.MapRuntimeDiagnostics("/_diagnostics/runtime");

**MVC/Controllers: identical setup alongside controllers.**
Code:
builder.Services.AddControllers();
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.MapControllers();
app.MapRuntimeDiagnostics();

**gRPC server: expose the diagnostics JSON next to gRPC.**
Code:
builder.Services.AddGrpc();
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.MapGrpcService<MyService>();
app.MapRuntimeDiagnostics("/_internal/runtime");

**Blazor Server**
Add DI, keep UI routes intact.
Code:
builder.Services.AddServerSideBlazor();
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapRuntimeDiagnostics();

**Background/Worker Services (with embedded HTTP)**
Host a small Kestrel endpoint exclusively for diagnostics.
Code:
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(9090));
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.MapRuntimeDiagnostics("/_diagnostics/runtime");
await app.RunAsync();

**Azure App Service / Containers**
Management route on a dedicated port (good for Kubernetes/sidecars).
Code:
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(8080));      // app
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(9090));      // management
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.MapRuntimeDiagnostics("/_diagnostics/runtime").RequireHost("*:9090");

Secure/Scoped Diagnostics

Require auth/role and move under a management path.
Code:
builder.Services.AddAuthentication().AddJwtBearer(...);
builder.Services.AddAuthorization(o => o.AddPolicy("Diagnostics", p => p.RequireRole("Ops")));
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapRuntimeDiagnostics("/management/runtime").RequireAuthorization("Diagnostics");

**OpenAPI/Swagger Integration**
Make the endpoint discoverable in Swagger UI.
Code:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDiagnosticsToolkit();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapRuntimeDiagnostics("/_diagnostics/runtime").WithOpenApi();

**Project Reference vs Package**
Project reference (current repo):
Add to your app csproj: <ProjectReference Include="..\DiagnosticsToolkit.AspNetCore\DiagnosticsToolkit.AspNetCore.csproj" />
NuGet (when published):
Add package and then call AddDiagnosticsToolkit() and MapRuntimeDiagnostics() as above.