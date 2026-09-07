using System.Security.Cryptography;
using System.Text;

namespace RngService.Services;

public class ProvablyFairRngService : IRngService
{
    public (string ServerSeed, string ServerSeedHash) GenerateServerSeed()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var serverSeed = Convert.ToHexString(bytes).ToLowerInvariant();
        var serverSeedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serverSeed))).ToLowerInvariant();
        
        return (serverSeed, serverSeedHash);
    }

    public string CalculateHmacSha256(string key, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var hmacHex = Convert.ToHexString(hash).ToLowerInvariant();
        
        return hmacHex;
    }

    public int[] CalculateDiceRolls(
        string serverSeed, 
        string clientSeed1, 
        string clientSeed2, 
        int nonce, 
        string stepTag, 
        int diceCount = 3, 
        int diceSides = 6)
    {
        var rolls = new int[diceCount];

        for (var i = 0; i < diceCount; i++)
        {
            var message = $"{clientSeed1}:{clientSeed2}:{nonce}:{stepTag}:die_{i}";
            var hmacHex = CalculateHmacSha256(serverSeed, message);

            rolls[i] = ConvertHashToRange(hmacHex, 1, diceSides);
        }

        return rolls;
    }

    private int ConvertHashToRange(string hexHash, int min, int max)
    {
        var range = max - min + 1;
        var bytes = Convert.FromHexString(hexHash);

        for (var i = 0; i <= bytes.Length - 4; i += 4)
        {
            var randomValue = BitConverter.ToUInt32(bytes, i);
            var remainderLimit = uint.MaxValue - (uint.MaxValue % (uint)range);

            if (randomValue < remainderLimit)
            {
                return (int)(min + (randomValue % range));
            }
        }
        
        var newHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return ConvertHashToRange(newHash, min, max);
    }
}