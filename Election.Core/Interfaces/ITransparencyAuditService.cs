using Election.Core.Models;

namespace Election.Core.Interfaces;

/// <summary>
/// Service for emitting audit events to the Transparency Service (immutable ledger).
/// All operations are non-blocking and fail-safe to prevent disruption to election operations.
/// </summary>
public interface ITransparencyAuditService
{
    /// <summary>
    /// Emits an audit event asynchronously to the Transparency Service.
    /// Non-blocking operation that logs locally if the remote service is unavailable.
    /// </summary>
    /// <param name="eventType">Event identifier (e.g., "HANDSHAKE_EMITTED")</param>
    /// <param name="severity">Severity level: INFO, LOW, MEDIUM, HIGH, CRITICAL</param>
    /// <param name="details">Event-specific details (no voter PII)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Task representing the async operation</returns>
    Task EmitEventAsync(
        string eventType,
        string severity,
        Dictionary<string, object> details,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Emits a handshake-related audit event.
    /// </summary>
    Task EmitHandshakeEventAsync(
        string eventType,
        string terminalId,
        string? sessionId = null,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Emits a QR scan result event with scrutiny information.
    /// </summary>
    Task EmitQrScannedEventAsync(
        string status, // "legitimate", "duplicate", "invalid"
        string juroId,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Emits a conciliation attempt event (digital vs. physical vote count).
    /// </summary>
    Task EmitConciliationAttemptEventAsync(
        int digitalTotal,
        int physicalTotal,
        int juryCount,
        bool success,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    );
}
