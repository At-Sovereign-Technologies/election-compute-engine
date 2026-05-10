using Election.VoteVault.Ceremony.Interfaces;
using Election.VoteVault.Ceremony.Models;

namespace Election.VoteVault.Ceremony.Services;

public class OpeningCeremonyService
    : IOpeningCeremonyService
{
    private readonly List<KeyShare> _validShares =
    [
        new()
        {
            Owner = "CNE",
            Token = "CNE-KEY"
        },

        new()
        {
            Owner = "AUDITOR",
            Token = "AUDITOR-KEY"
        },

        new()
        {
            Owner = "SUPERADMIN",
            Token = "SUPERADMIN-KEY"
        }
    ];

    private bool _vaultOpen = false;

    private string? _ephemeralKey;

    public OpeningCeremonyResult OpenVault(
        List<KeyShare> shares
    )
    {
        if (_vaultOpen)
        {
            return new OpeningCeremonyResult
            {
                Success = true,
                Message = "Vault already open.",
                OpenedAt = DateTime.UtcNow
            };
        }

        int validShares = shares.Count(
            incoming => _validShares.Any(
                valid =>
                    valid.Owner == incoming.Owner &&
                    valid.Token == incoming.Token
            )
        );

        if (validShares < 2)
        {
            return new OpeningCeremonyResult
            {
                Success = false,
                Message = "Minimum quorum not reached."
            };
        }

        _ephemeralKey = Guid.NewGuid().ToString();

        _vaultOpen = true;

        return new OpeningCeremonyResult
        {
            Success = true,
            Message = "Vault opened successfully.",
            OpenedAt = DateTime.UtcNow
        };
    }

    public bool IsVaultOpen()
    {
        return _vaultOpen;
    }
}