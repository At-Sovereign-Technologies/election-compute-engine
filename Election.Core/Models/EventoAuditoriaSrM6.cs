namespace Election.Core.Models;

// Espejo del contrato HTTP del transparency-service.
// POST http://<transparency-service>/api/v1/transparency/events
//
// IMPORTANTE — política Zero-Identity:
// `Details` debe contener exclusivamente metadatos criptográficos, hashes
// o identificadores de mesa/elección. Cualquier campo con PII en texto
// plano causa rechazo HTTP 400 por el gateway de transparencia.
public class EventoAuditoriaSrM6
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string OriginComponent { get; set; } = "COMPUTE_ENGINE";

    public string EventType { get; set; } = string.Empty;

    public string Severity { get; set; } = "INFO";

    public Dictionary<string, object> Details { get; set; } = new();
}
