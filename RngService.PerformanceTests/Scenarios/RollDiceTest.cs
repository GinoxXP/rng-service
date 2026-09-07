using Ginbet.RngService.V1;
using Grpc.Core;
using Grpc.Net.Client;
using NBomber.Contracts;
using NBomber.CSharp;

namespace RngService.PerformanceTests.Scenarios;

public static class RollDiceTest
{
    public static ScenarioProps Realistic(string url)
    {
        const int channelCount = 4;
        var channels = new GrpcChannel[channelCount];
        var clients = new RngGenerator.RngGeneratorClient[channelCount];

        return Scenario.Create("rng_roll_dice_scenario", async context =>
        {
            var clientIndex = context.ScenarioInfo.InstanceNumber % channelCount;
            var client = clients[clientIndex];

            try
            {
                var response = await client.RollDiceAsync(
                    new RollDiceRequest()
                    {
                        ClientSeed1 = Guid.CreateVersion7().ToString(),
                        ClientSeed2 = Guid.CreateVersion7().ToString(),
                        DiceCount = 3,
                        Nonce = 0,
                        ServerSeed = Guid.CreateVersion7().ToString(),
                        StepTag = "tag",
                        DiceSides = 6,
                    },
                    cancellationToken: context.ScenarioCancellationToken
                );

                return Response.Ok(sizeBytes: response.CalculateSize());
            }
            catch (RpcException ex)
            {
                return Response.Fail(statusCode: ex.StatusCode.ToString(), message: ex.Message);
            }
            catch (Exception ex)
            {
                return Response.Fail(statusCode: "InternalError", message: ex.Message);
            }
        })
        .WithInit(async context =>
        {
            for (var i = 0; i < channelCount; i++)
            {
                var socketsHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    EnableMultipleHttp2Connections = true
                };

                channels[i] = GrpcChannel.ForAddress(url, new GrpcChannelOptions
                {
                    HttpHandler = socketsHandler
                });

                clients[i] = new RngGenerator.RngGeneratorClient(channels[i]);
            }
        })
        .WithClean(async context =>
        {
            foreach (var channel in channels)
            {
                channel?.Dispose();
            }
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 800, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(15)),
            Simulation.Inject(rate: 800, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.RampingInject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(15)),
            Simulation.Inject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(15))
        );
    }
}