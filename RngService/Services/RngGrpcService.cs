using Grpc.Core;
using RngService.Protos;

namespace RngService.Services;

public class RngGrpcService : RngGenerator.RngGeneratorBase
{
    private readonly ILogger<RngGrpcService> _logger;
    private readonly IRngService _rngService;

    public RngGrpcService(
        ILogger<RngGrpcService> logger,
        IRngService rngService)
    {
        _logger = logger;
        _rngService = rngService;
    }

    public override Task<PrepareRoundResponse> PrepareRound(PrepareRoundRequest request, ServerCallContext context)
    {
        var (serverSeed, serverSeedHash) = _rngService.GenerateServerSeed();

        return Task.FromResult(new PrepareRoundResponse
        {
            ServerSeed = serverSeed,
            ServerSeedHash = serverSeedHash
        });
    }

    public override Task<PlayRoundResponse> PlayRound(PlayRoundRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ServerSeed) || string.IsNullOrEmpty(request.ClientSeed))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Seeds cannot be empty."));
        }

        int result = _rngService.CalculateResult(
            request.ServerSeed,
            request.ClientSeed,
            request.Nonce,
            request.MaxRange
        );

        string roundHash = _rngService.CalculateHmacSha256(
            request.ServerSeed,
            $"{request.ClientSeed}:{request.Nonce}"
        );

        return Task.FromResult(new PlayRoundResponse
        {
            Result = result,
            RoundHash = roundHash
        });
    }
}