using RngService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddSingleton<IRngService, ProvablyFairRngService>();

builder.Services.AddGrpc();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGrpcService<RngGrpcService>();

app.Run();
