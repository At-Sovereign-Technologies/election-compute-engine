namespace Election.VoteVault.Ceremony.Interfaces;

/// <summary>
/// Manages terminal handshake protocol and emits corresponding audit events.
/// Implements US-SR-M6-03: Handshake Audit.
/// </summary>
public interface IHandshakeService
{
    /// <summary>
    /// Emits a handshake with pairing code.
    /// </summary>
    Task<string> EmitHandshakeAsync(
        string terminalId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Activates a session with pairing code verification.
    /// </summary>
    Task<bool> ActivateSessionAsync(
        string terminalId,
        string sessionId,
        string pairingCode,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Closes an active session.
    /// </summary>
    Task CloseSessionAsync(
        string terminalId,
        string sessionId,
        string reason, // "VOTE_CAST" or "TIMEOUT"
        CancellationToken cancellationToken = default
    );
}
