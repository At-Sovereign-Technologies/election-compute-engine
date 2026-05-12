namespace Election.Engine.Scrutiny;

/// <summary>
/// Service for emitting double truth scrutiny audit events.
/// Implements US-SR-M6-04: Double Truth Scrutiny Audit.
/// </summary>
public interface IScrutinyAuditor
{
    /// <summary>
    /// Records a QR code scan and emits audit event.
    /// </summary>
    Task RecordQrScanAsync(
        string juroId,
        string status, // "legitimate", "duplicate", "invalid"
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Records a conciliation attempt between digital and physical vote counts.
    /// </summary>
    Task RecordConciliationAttemptAsync(
        int digitalTotal,
        int physicalTotal,
        int juryCount,
        bool success,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    );
}
