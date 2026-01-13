using FunctionApp1.HealthChecks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddOptions<HealthCheckOptions>()
    .Bind(builder.Configuration.GetSection(HealthCheckOptions.SectionName))
    .PostConfigure(o =>
    {
        o.Schedule ??= HealthCheckOptions.DefaultSchedule;
        o.TargetUrl ??= HealthCheckOptions.DefaultTargetUrl;
        o.TimeoutSeconds ??= HealthCheckOptions.DefaultTimeoutSeconds;
    })
    .ValidateDataAnnotations()
    .Validate(o => Uri.TryCreate(o.TargetUrl, UriKind.Absolute, out _), "HealthCheck:TargetUrl must be a valid absolute URL.")
    .ValidateOnStart();

builder.Services.AddHttpClient(HealthCheckService.HttpClientName)
    .ConfigureHttpClient((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<HealthCheckOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds ?? HealthCheckOptions.DefaultTimeoutSeconds);
    });

builder.Services.AddDbContextFactory<HealthCheckDbContext>(options =>
{
    var raw = builder.Configuration["ConnectionStrings:HealthChecks"] ?? "Data Source=healthchecks.db";
    var dbPath = Path.Combine(AppContext.BaseDirectory, "healthchecks.db");
    var rewritten = HealthCheckDbContext.BuildSqliteConnectionString(raw, dbPath);

    options.UseSqlite(rewritten);
});

builder.Services.AddScoped<HealthCheckService>();

builder.Build().Run();
