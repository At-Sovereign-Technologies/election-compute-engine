namespace Election.Core.Models;

/// <summary>
/// DTO for audit events sent to the Transparency Service.
/// Implements the "Zero-Identity" principle: no voter PII is included.
/// </summary>
public class TransparencyEventRequest
{
    /// <summary>
    /// ISO 8601 timestamp when the event occurred
    /// </summary>
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

    /// <summary>
    /// Must always be "COMPUTE_ENGINE" for events originating from this system
    /// </summary>
    public string OriginComponent { get; set; } = "COMPUTE_ENGINE";

    /// <summary>
    /// Event type identifier (e.g., "HANDSHAKE_EMITTED", "SESSION_ACTIVATED", "QR_SCANNED")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: INFO, LOW, MEDIUM, HIGH, CRITICAL
    /// </summary>
    public string Severity { get; set; } = "INFO";

    /// <summary>
    /// Context-specific details. MUST NOT contain voter IDs, names, or document numbers.
    /// Allowed keys: terminal_id, session_id, timestamp, jury_id, status, digital_total, physical_total, etc.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
