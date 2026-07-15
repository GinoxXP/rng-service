using RngService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRngService, ProvablyFairRngService>();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<RngGrpcService>();

app.Run();
