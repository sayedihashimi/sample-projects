using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddHttpClient("GitHub", client =>
    {
        // GitHub requires a User-Agent header on API requests.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FunctionApp1-PrGatekeeper");
    });

builder.Build().Run();
