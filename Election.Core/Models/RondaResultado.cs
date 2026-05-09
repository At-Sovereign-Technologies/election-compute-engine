namespace Election.Core.Models;

public class RondaResultado
{
    public int NumeroRonda { get; set; }

    public Dictionary<string, decimal> Conteo { get; set; } = new();

    public string? CandidatoEliminado { get; set; }

    public decimal VotosAgotados { get; set; }
}