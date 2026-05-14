namespace Election.Core.Models;

public class EventoAuditoriaSrM6
{
    public string Actor { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string ObjetoTipo { get; set; } = string.Empty;
    public string ObjetoId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpOrigen { get; set; }
    public string? UserAgent { get; set; }
    public string PayloadJson { get; set; } = "{}";
}
