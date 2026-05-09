namespace Election.Core.Models;

public class Resultado
{
    public string Ganador { get; set; } = string.Empty;

    public decimal Porcentaje { get; set; }

    public Dictionary<string, decimal> Totales { get; set; } = new();

    public List<RondaResultado> Rondas { get; set; } = new();
}