# RngService

Provably fair random number generator exposed as a gRPC microservice. Built with .NET 10 and ASP.NET Core gRPC.

Extracted from a PvP dice-battle game, where it rolls dice for every turn of a battle. Players can check
each roll themselves after the round ends.

## How it works

The scheme is the usual commit–reveal one.

1. **Commit.** Before a round, `PrepareRound` generates a 256-bit server seed with a cryptographic RNG and
   returns it with its SHA-256 hash. The hash goes to players up front; the seed stays secret until the round ends.
2. **Roll.** Each die is computed as
   `HMAC-SHA256(key = serverSeed, message = "{clientSeed1}:{clientSeed2}:{nonce}:{stepTag}:die_{i}")`.
   Both players supply a client seed, so neither the server nor one player alone decides the outcome.
3. **Reveal.** After the round the server seed is published. Anyone can check that it matches the hash they
   got earlier and recompute every roll.

The HMAC output becomes a die value through **rejection sampling**. The service reads the hash as 32-bit chunks
and discards values in the biased tail (`value >= uint.MaxValue - uint.MaxValue % range`). That keeps the
distribution uniform, with no modulo bias. If all chunks are rejected, it re-hashes and tries again.

## API

[`src/RngService/Protos/rng.proto`](src/RngService/Protos/rng.proto)

| RPC            | Description                                                                                   |
|----------------|-----------------------------------------------------------------------------------------------|
| `PrepareRound` | Returns a new `server_seed` and its `server_seed_hash`                                        |
| `RollDice`     | Returns dice `rolls` plus the HMAC hash behind each roll. Defaults: 3 dice, 6 sides, tag `turn` |

Empty seeds are rejected with `InvalidArgument`.

## Project structure

```
src/RngService/           gRPC service
  Services/ProvablyFairRngService.cs   seed generation, HMAC, rejection sampling
  Services/RngGrpcService.cs           gRPC endpoint, input validation
tests/RngService.Tests/   xUnit tests
```

The tests check:
- seed uniqueness and hash correctness
- determinism: the same inputs always give the same rolls
- range bounds over thousands of random rolls
- a known HMAC-SHA256 test vector
- pinned outputs shared with the game's TypeScript verifier, so browser-side verification stays in sync with the server

## Running

```bash
dotnet test
```

```bash
dotnet run --project src/RngService
```

```bash
docker build -f src/RngService/Dockerfile -t rng-service .
docker run -p 8080:8080 rng-service
```

The service uses HTTP/2 only (gRPC) and logs with Serilog to the console and to rolling JSON files.
