using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FunctionApp1.HealthCheck;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddOptions<HealthCheckOptions>()
    .Bind(builder.Configuration.GetSection(HealthCheckOptions.SectionName))
    .PostConfigure(o => o.ApplyDefaults())
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient(HealthCheckService.HttpClientName);
builder.Services.AddSingleton<HealthCheckService>();

builder.Build().Run();
