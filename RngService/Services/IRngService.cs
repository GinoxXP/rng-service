namespace RngService.Services;

public interface IRngService
{
    (string ServerSeed, string ServerSeedHash) GenerateServerSeed();
    
    int[] CalculateDiceRolls(
        string serverSeed, 
        string clientSeed1, 
        string clientSeed2, 
        int nonce, 
        string stepTag, 
        int diceCount = 3, 
        int diceSides = 6);

    string CalculateHmacSha256(string key, string message);
}