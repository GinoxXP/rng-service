using System.Security.Cryptography;
using System.Text;

namespace RngService.Services;

public class ProvablyFairRngService : IRngService
{
    public (string ServerSeed, string ServerSeedHash) GenerateServerSeed()
    {
        byte[] bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        string serverSeed = Convert.ToHexString(bytes).ToLowerInvariant();
        string serverSeedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serverSeed))).ToLowerInvariant();

        return (serverSeed, serverSeedHash);
    }

    public string CalculateHmacSha256(string key, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
        int[] rolls = new int[diceCount];

        for (int i = 0; i < diceCount; i++)
        {
            string message = $"{clientSeed1}:{clientSeed2}:{nonce}:{stepTag}:die_{i}";
            string hmacHex = CalculateHmacSha256(serverSeed, message);

            rolls[i] = ConvertHashToRange(hmacHex, 1, diceSides);
        }

        return rolls;
    }

    private int ConvertHashToRange(string hexHash, int min, int max)
    {
        int range = max - min + 1;
        byte[] bytes = Convert.FromHexString(hexHash);

        for (int i = 0; i <= bytes.Length - 4; i += 4)
        {
            uint randomValue = BitConverter.ToUInt32(bytes, i);
            uint remainderLimit = uint.MaxValue - (uint.MaxValue % (uint)range);

            if (randomValue < remainderLimit)
            {
                return (int)(min + (randomValue % range));
            }
        }

        string newHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return ConvertHashToRange(newHash, min, max);
    }
}