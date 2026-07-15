namespace RngService.Services;

public interface IRngService
{
    (string ServerSeed, string ServerSeedHash) GenerateServerSeed();
    int CalculateResult(string serverSeed, string clientSeed, int nonce, int maxRange);
    string CalculateHmacSha256(string serverSeed, string message);
}