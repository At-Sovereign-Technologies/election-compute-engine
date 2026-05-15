namespace Election.Core.Models;

// Devuelto por GET /api/v1/asistencia/acta/{mesaId}.
// Cumple US-SE-M3-05 CA #5: "Reflejar en el acta de mesa el conteo de votos
// asistidos por tipo, sin identidades."
public class ConteoAsistenciasActa
{
    public string MesaId { get; set; } = string.Empty;
    public int TotalAsistidos { get; set; }
    public Dictionary<TipoAsistencia, int> PorTipo { get; set; } = new();
    public int TotalFamiliares { get; set; }
    public int TotalNoFamiliares { get; set; }
}
