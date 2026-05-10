namespace Election.VoteVault.Models;

public class CustodiedVote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EncryptedPayload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Custodied";
}