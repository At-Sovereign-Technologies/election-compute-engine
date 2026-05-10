namespace Election.VoteVault.Models;

public class VaultSeal
{
    public DateTime CreatedAt { get; set; }

    public string RootHash { get; set; } = string.Empty;

    public int TotalVotes { get; set; }
}