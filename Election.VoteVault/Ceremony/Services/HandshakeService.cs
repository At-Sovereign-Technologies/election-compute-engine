using Election.Core.Interfaces;
using Election.VoteVault.Ceremony.Interfaces;
using Microsoft.Extensions.Logging;

namespace Election.VoteVault.Ceremony.Services;

/// <summary>
/// Implementation of handshake protocol with audit event emission.
/// </summary>
public class HandshakeService : IHandshakeService
{
    private readonly ITransparencyAuditService _auditService;
    private readonly ILogger<HandshakeService> _logger;
    private readonly Dictionary<string, (string SessionId, DateTime CreatedAt)> _activeSessions = new();

    public HandshakeService(
        ITransparencyAuditService auditService,
        ILogger<HandshakeService> logger
    )
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<string> EmitHandshakeAsync(
        string terminalId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Generate pairing code
            var pairingCode = GeneratePairingCode();

            _logger.LogInformation(
                "Emitting handshake for terminal {TerminalId} with pairing code",
                terminalId
            );

            // Emit audit event: HANDSHAKE_EMITTED
            await _auditService.EmitHandshakeEventAsync(
                eventType: "HANDSHAKE_EMITTED",
                terminalId: terminalId,
                additionalDetails: new()
                {
                    { "pairing_code_issued", true }
                },
                cancellationToken: cancellationToken
            );

            return pairingCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error emitting handshake for terminal {TerminalId}",
                terminalId
            );

            throw;
        }
    }

    public async Task<bool> ActivateSessionAsync(
        string terminalId,
        string sessionId,
        string pairingCode,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Validate pairing code (simplified for this example)
            if (string.IsNullOrWhiteSpace(pairingCode))
            {
                _logger.LogWarning(
                    "Invalid pairing code for terminal {TerminalId}",
                    terminalId
                );

                return false;
            }

            // Store active session
            _activeSessions[terminalId] = (sessionId, DateTime.UtcNow);

            _logger.LogInformation(
                "Session {SessionId} activated for terminal {TerminalId}",
                sessionId,
                terminalId
            );

            // Emit audit event: SESSION_ACTIVATED
            await _auditService.EmitHandshakeEventAsync(
                eventType: "SESSION_ACTIVATED",
                terminalId: terminalId,
                sessionId: sessionId,
                additionalDetails: new()
                {
                    { "pairing_successful", true }
                },
                cancellationToken: cancellationToken
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error activating session {SessionId} for terminal {TerminalId}",
                sessionId,
                terminalId
            );

            throw;
        }
    }

    public async Task CloseSessionAsync(
        string terminalId,
        string sessionId,
        string reason, // "VOTE_CAST" or "TIMEOUT"
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var eventType = reason switch
            {
                "VOTE_CAST" => "SESSION_CLOSED_VOTE",
                "TIMEOUT" => "SESSION_CLOSED_TIMEOUT",
                _ => "SESSION_CLOSED"
            };

            _logger.LogInformation(
                "Closing session {SessionId} for terminal {TerminalId} with reason: {Reason}",
                sessionId,
                terminalId,
                reason
            );

            // Remove from active sessions
            _activeSessions.Remove(terminalId);

            // Emit audit event
            await _auditService.EmitHandshakeEventAsync(
                eventType: eventType,
                terminalId: terminalId,
                sessionId: sessionId,
                additionalDetails: new()
                {
                    { "reason", reason }
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error closing session {SessionId} for terminal {TerminalId}",
                sessionId,
                terminalId
            );

            throw;
        }
    }

    private string GeneratePairingCode()
    {
        // Generate a 6-digit pairing code
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
