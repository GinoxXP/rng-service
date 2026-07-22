using System.Security.Cryptography;
using System.Text;
using RngService.Services;

namespace RngService.Tests;

public class RngServiceNunitTests
{
    private readonly IRngService _rngService;
    
    public RngServiceNunitTests()
    {
        _rngService = new ProvablyFairRngService();
    }
    
    [Fact]
        public void GenerateServerSeed_ShouldReturnUniqueAndValidSeeds()
        {
            var (seed1, hash1) = _rngService.GenerateServerSeed();
            var (seed2, hash2) = _rngService.GenerateServerSeed();

            Assert.False(string.IsNullOrEmpty(seed1));
            Assert.False(string.IsNullOrEmpty(seed2));
            Assert.NotEqual(seed1, seed2);

            Assert.Equal(64, hash1.Length); 
            
            byte[] computedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed1));
            string computedHash = Convert.ToHexString(computedHashBytes).ToLower();
            Assert.Equal(computedHash, hash1);
        }

        [Theory]
        [InlineData("test_server_seed_1", "client_abc", 1, 1, 6)]
        [InlineData("another_secret_seed", "player_xyz", 42, 1,100)]
        public void CalculateResult_WithSameInputs_ShouldAlwaysReturnSameResult(
            string serverSeed, string clientSeed, int nonce, int minRange, int maxRange)
        {
            int result1 = _rngService.CalculateResult(serverSeed, clientSeed, nonce, minRange, maxRange);
            int result2 = _rngService.CalculateResult(serverSeed, clientSeed, nonce, minRange, maxRange);

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
                string clientSeed = $"client_seed_{nonce}";

                int result = _rngService.CalculateResult(serverSeed, clientSeed, nonce, minRange, maxRange);
                
                Assert.True(result >= 0 && result <= maxRange);
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

