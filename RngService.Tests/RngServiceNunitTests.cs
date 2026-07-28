using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RngService.Services;

namespace RngService.Tests;

public class RngServiceNunitTests
{
    private readonly IRngService _rngService;
    
    public RngServiceNunitTests()
    {
        var logger = NullLogger<ProvablyFairRngService>.Instance;
        _rngService = new ProvablyFairRngService(logger);
    }
    
    [Fact]
    public void GenerateServerSeed_ShouldReturnUniqueAndValidSeeds()
    {
        var (seed1, hash1) = _rngService.GenerateServerSeed();
        var (seed2, _) = _rngService.GenerateServerSeed();

        Assert.False(string.IsNullOrEmpty(seed1));
        Assert.False(string.IsNullOrEmpty(seed2));
        Assert.NotEqual(seed1, seed2);

        Assert.Equal(64, hash1.Length); 
        
        byte[] computedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed1));
        string computedHash = Convert.ToHexString(computedHashBytes).ToLower();
        Assert.Equal(computedHash, hash1);
    }

    [Theory]
    [InlineData("test_server_seed_1", "client_abc", "client_xyz", 1, "stepTag", 1, 6)]
    [InlineData("test_server_seed_2", "client_def", "client_urp", 1, "stepTag", 5, 10)]
    public void CalculateResult_WithSameInputs_ShouldAlwaysReturnSameResult(
        string serverSeed,
        string client1Seed,
        string client2Seed,
        int nonce,
        string stepTag,
        int minRange,
        int maxRange)
    {
        var result1 = _rngService.CalculateDiceRolls(serverSeed, client1Seed, client2Seed, nonce, stepTag, minRange, maxRange);
        var result2 = _rngService.CalculateDiceRolls(serverSeed, client1Seed, client2Seed, nonce, stepTag, minRange, maxRange);


        Assert.Equal(result1, result2);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 100)]
    public void CalculateResult_ShouldNeverExceedMaxRange(int minRange, int maxRange)
    {
        for (int nonce = 1; nonce <= 1000; nonce++)
        {
            var (serverSeed, _) = _rngService.GenerateServerSeed();
            string client1Seed = $"client1_seed_{nonce}";
            string client2Seed = $"client2_seed_{nonce}";
            string stepTag = $"step_tag_{nonce}";

            var result = _rngService.CalculateDiceRolls(serverSeed, client1Seed, client2Seed, nonce, stepTag, minRange, maxRange);
            
            Assert.True(result.All(d => d >= minRange && d <= maxRange));
        }
    }
    
    [Fact]
    public void CalculateHmacSha256_ShouldReturnCorrectHex()
    {
        string serverSeed = "key";
        string message = "message";
        string expectedHmac = "6e9ef29b75fffc5b7abae527d58fdadb2fe42e7219011976917343065f58ed4a";

        string actualHmac = _rngService.CalculateHmacSha256(serverSeed, message);

        Assert.Equal(expectedHmac, actualHmac);
    }
}

