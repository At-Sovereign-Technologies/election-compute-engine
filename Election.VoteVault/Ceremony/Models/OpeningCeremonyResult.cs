namespace Election.VoteVault.Ceremony.Models;

public class OpeningCeremonyResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }
}