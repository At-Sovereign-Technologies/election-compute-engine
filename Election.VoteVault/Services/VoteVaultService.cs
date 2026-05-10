using System.Security.Cryptography;
using System.Text;
using Election.VoteVault.Interfaces;
using Election.VoteVault.Models;

namespace Election.VoteVault.Services;

public class VoteVaultService : IVoteVaultService
{
    private readonly List<CustodiedVote> _vault = new();

    private readonly RSA _rsa;

    public VoteVaultService()
    {
        _rsa = RSA.Create(2048);
    }

    public CustodiedVote CustodyVote(string payload)
    {
        byte[] data = Encoding.UTF8.GetBytes(payload);

        byte[] encrypted = _rsa.Encrypt(
            data,
            RSAEncryptionPadding.OaepSHA256
        );

        var vote = new CustodiedVote
        {
            EncryptedPayload = Convert.ToBase64String(encrypted)
        };

        _vault.Add(vote);

        return vote;
    }

    public int GetCustodiedVotesCount()
    {
        return _vault.Count;
    }

    public IEnumerable<CustodiedVote> GetVotes()
    {
        throw new UnauthorizedAccessException(
            "Access to custodial payloads is blocked until election closure."
        );
    }

    public IEnumerable<CustodiedVote> GetCustodiedVotesInternal()
    {
        return _vault;
    }
}