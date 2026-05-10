using Election.VoteVault.Models;

namespace Election.VoteVault.Interfaces;

public interface ISealService
{
    VaultSeal GenerateSeal();

    IEnumerable<VaultSeal> GetSeals();
}