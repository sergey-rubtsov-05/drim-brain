using System.Reflection;
using ApiGateway.Clients;
using ApiGateway.Features.Chat;
using ApiGateway.Features.Deposits;
using ApiGateway.Health;
using ApiGateway.Metrics;
using Common.Validation;
using Common.Web.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Connections;

const string readyTag = "ready";
const string liveTag = "live";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<StartupHostedService>();

builder.Services.AddSingleton<LivenessHealthCheck>();
builder.Services.AddSingleton<ReadinessHealthCheck>();

builder.Services.AddHealthChecks()
    .AddCheck<LivenessHealthCheck>("liveness", tags: [liveTag])
    .AddCheck<ReadinessHealthCheck>("readiness", tags: [readyTag]);

builder.Services.AddMediatR(cfg => cfg
    .RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
    .AddOpenBehavior(typeof(ValidationBehavior<,>)));

builder.Services.AddSignalR();
    //.AddMessagePackProtocol();

builder.Services.AddSingleton<ApiGatewayMetrics>();

var configuration = builder.Configuration
    .AddJsonFile(Path.Combine("extraSettings", "appsettings.json"), optional: false, reloadOnChange: true)
    .Build();

var clientsOptions = configuration.GetSection(ClientsOptions.SectionName).Get<ClientsOptions>();

builder.Services.AddGrpcClient<BankingService.Client.Deposits.DepositsClient>(o =>
{
    o.Address = new Uri(clientsOptions!.BankingService);
});
builder.Services.AddGrpcClient<BankingService.Client.Withdrawals.WithdrawalsClient>(o =>
{
    o.Address = new Uri(clientsOptions!.BankingService);
});

var app = builder.Build();

app.UseStaticFiles();

app.MapHub<ChatHub>("/chatHub", options =>
{
    //options.Transports = HttpTransportType.ServerSentEvents;
});
app.MapHub<DepositsHub>("/depositsHub");

app.MapEndpoints();

app
    .UseHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains(liveTag),
    })
    .UseHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains(readyTag),
    });

app.Run();
