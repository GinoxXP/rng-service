namespace RngService.Services;

public interface IRngService
{
    (string ServerSeed, string ServerSeedHash) GenerateServerSeed();
    int CalculateResult(string serverSeed, string clientSeed, int nonce, int minRange, int maxRange);
    string CalculateHmacSha256(string serverSeed, string message);
}