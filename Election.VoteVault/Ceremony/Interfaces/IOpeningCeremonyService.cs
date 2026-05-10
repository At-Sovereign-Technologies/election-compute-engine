using Election.VoteVault.Ceremony.Models;

namespace Election.VoteVault.Ceremony.Interfaces;

public interface IOpeningCeremonyService
{
    OpeningCeremonyResult OpenVault(
        List<KeyShare> shares
    );

    bool IsVaultOpen();
}