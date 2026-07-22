using System.Security.Cryptography;
using System.Text;

namespace RngService.Services;

public class ProvablyFairRngService : IRngService
{
    public (string ServerSeed, string ServerSeedHash) GenerateServerSeed()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var serverSeed = Convert.ToHexString(randomBytes).ToLower();
            
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(serverSeed));
        var serverSeedHash = Convert.ToHexString(hashBytes).ToLower();

        return (serverSeed, serverSeedHash);
    }

    public int CalculateResult(string serverSeed, string clientSeed, int nonce, int minRange, int maxRange)
    {
        var message = $"{clientSeed}:{nonce}";
        
        var roundHash = CalculateHmacSha256(serverSeed, message);

        var hexSubstring = roundHash.Substring(0, 5);
        var decimalValue = Convert.ToInt32(hexSubstring, 16);

        int range = maxRange - minRange + 1;
        return minRange + (decimalValue % range);
    }

    public string CalculateHmacSha256(string serverSeed, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(serverSeed);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}