using Ginbet.RngService.V1;
using Grpc.Net.Client;
using NBomber.Contracts;
using NBomber.CSharp;

namespace RngService.PerformanceTests.Scenarios;

public static class PrepareRoundTest
{
    public static ScenarioProps Realistic(string url)
    {
        const int channelCount = 4;
        var channels = new GrpcChannel[channelCount];
        
        var clients = new RngGenerator.RngGeneratorClient[channelCount];

        return Scenario.Create("rng_prepare_round_scenario", async context =>
        {
            var clientIndex = context.ScenarioInfo.InstanceNumber % channelCount;
            var client = clients[clientIndex];

            try
            {
                var response = await client.PrepareRoundAsync(new PrepareRoundRequest(),
                    cancellationToken: context.ScenarioCancellationToken);

                return Response.Ok(sizeBytes: response.CalculateSize());
            }
            catch (Exception e)
            {
                return Response.Fail(statusCode: "InternalError");
            }
        })
        .WithInit(async context =>
        {
            for (int i = 0; i < channelCount; i++)
            {
                var socketsHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    EnableMultipleHttp2Connections = true
                };

                channels[i] = GrpcChannel.ForAddress(url, new GrpcChannelOptions
                {
                    HttpHandler = socketsHandler,
                    MaxReceiveMessageSize = 5 * 1024 * 1024,
                    MaxSendMessageSize = 5 * 1024 * 1024
                });

                clients[i] = new RngGenerator.RngGeneratorClient(channels[i]);
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