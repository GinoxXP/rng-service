using Ginbet.RngService.V1;
using Grpc.Core;

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

    public override Task<RollDiceResponse> RollDice(RollDiceRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ServerSeed) ||
            string.IsNullOrWhiteSpace(request.ClientSeed1) ||
            string.IsNullOrWhiteSpace(request.ClientSeed2))
        {
            _logger.LogWarning("RollDice failed: ServerSeed or ClientSeeds are missing.");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ServerSeed and both ClientSeeds cannot be empty."));
        }

        var diceCount = request.DiceCount > 0 ? request.DiceCount : 3;
        var diceSides = request.DiceSides > 0 ? request.DiceSides : 6;
        var stepTag = string.IsNullOrWhiteSpace(request.StepTag) ? "turn" : request.StepTag;

        var rolls = _rngService.CalculateDiceRolls(
            request.ServerSeed,
            request.ClientSeed1,
            request.ClientSeed2,
            request.Nonce,
            stepTag,
            diceCount,
            diceSides
        );

        var response = new RollDiceResponse();
        response.Rolls.AddRange(rolls);

        for (var i = 0; i < diceCount; i++)
        {
            var message = $"{request.ClientSeed1}:{request.ClientSeed2}:{request.Nonce}:{stepTag}:die_{i}";
            var hmacHex = _rngService.CalculateHmacSha256(request.ServerSeed, message);
            response.HmacHashes.Add(hmacHex);
        }
        
        return Task.FromResult(response);
    }
}