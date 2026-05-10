using System.Security.Cryptography;
using System.Text;
using Election.VoteVault.Models;
using Election.VoteVault.Interfaces;

namespace Election.VoteVault.Services;

public class SealService : ISealService
{
    private readonly VoteVaultService _vault;

    private readonly List<VaultSeal> _seals = new();

    public SealService(VoteVaultService vault)
    {
        _vault = vault;
    }

    public VaultSeal GenerateSeal()
    {
        var votes = _vault.GetCustodiedVotesInternal();

        var builder = new StringBuilder();

        foreach (var vote in votes.OrderBy(v => v.Id))
        {
            builder.Append(vote.EncryptedPayload);
        }

        string rootHash = ComputeSha256(builder.ToString());

        var seal = new VaultSeal
        {
            CreatedAt = DateTime.UtcNow,
            RootHash = rootHash,
            TotalVotes = votes.Count()
        };

        _seals.Add(seal);

        return seal;
    }

    public IEnumerable<VaultSeal> GetSeals()
    {
        return _seals;
    }

    private string ComputeSha256(string rawData)
    {
        using var sha256 = SHA256.Create();

        byte[] bytes = sha256.ComputeHash(
            Encoding.UTF8.GetBytes(rawData)
        );

        return Convert.ToHexString(bytes);
    }
}