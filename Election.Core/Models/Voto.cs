namespace Election.Core.Models;

public class Voto
{
    public string VotanteId { get; set; } = string.Empty;

    public Dictionary<string, int> Preferencias { get; set; } = new();
}