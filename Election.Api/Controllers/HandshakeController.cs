using Election.Core.Interfaces;
using Election.VoteVault.Ceremony.Interfaces;
using Election.VoteVault.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

/// <summary>
/// Handshake protocol controller for terminal pairing and session management.
/// Implements US-SR-M6-03: Handshake Audit.
/// 
/// Workflow:
/// 1. POST /api/handshake/emit - Terminal initiates handshake (gets pairing code)
/// 2. POST /api/handshake/activate - Terminal confirms pairing with code (session starts)
/// 3. POST /api/handshake/close - Terminal closes session (vote cast or timeout)
/// </summary>
[ApiController]
[Route("api/handshake")]
public class HandshakeController : ControllerBase
{
    private readonly IHandshakeService _handshakeService;
    private readonly IVoteVaultService _voteVault;
    private readonly ILogger<HandshakeController> _logger;

    public HandshakeController(
        IHandshakeService handshakeService,
        IVoteVaultService voteVault,
        ILogger<HandshakeController> logger
    )
    {
        _handshakeService = handshakeService;
        _voteVault = voteVault;
        _logger = logger;
    }

    /// <summary>
    /// US-SR-M6-03: Emit handshake with pairing code
    /// 
    /// Request: { "terminal_id": "TERM-001" }
    /// Response: { "pairing_code": "123456" }
    /// 
    /// Audit Event: HANDSHAKE_EMITTED (INFO)
    /// Zero-Identity: Only terminal_id, no voter data
    /// </summary>
    [HttpPost("emit")]
    public async Task<IActionResult> EmitHandshake(
        [FromBody] EmitHandshakeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Handshake emitted for terminal {TerminalId}",
                request.TerminalId
            );

            var pairingCode = await _handshakeService.EmitHandshakeAsync(
                request.TerminalId,
                cancellationToken
            );

            // Audit event is emitted automatically by HandshakeService
            // Event: HANDSHAKE_EMITTED
            // Severity: INFO
            // Details: { terminal_id, timestamp, pairing_code_issued }

            return Ok(new
            {
                pairing_code = pairingCode,
                timestamp = DateTime.UtcNow.ToString("O"),
                message = "Handshake emitted. Terminal must confirm pairing code."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting handshake");
            return StatusCode(500, new { error = "Failed to emit handshake" });
        }
    }

    /// <summary>
    /// US-SR-M6-03: Activate session with pairing code verification
    /// 
    /// Request: { "terminal_id": "TERM-001", "session_id": "SESS-123", "pairing_code": "123456" }
    /// Response: { "success": true, "session_id": "SESS-123" }
    /// 
    /// Audit Event: SESSION_ACTIVATED (INFO)
    /// Zero-Identity: Only terminal_id and session_id, no voter data
    /// </summary>
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateSession(
        [FromBody] ActivateSessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await _handshakeService.ActivateSessionAsync(
                request.TerminalId,
                request.SessionId,
                request.PairingCode,
                cancellationToken
            );

            if (!success)
            {
                return BadRequest(new { error = "Invalid pairing code" });
            }

            _logger.LogInformation(
                "Session {SessionId} activated for terminal {TerminalId}",
                request.SessionId,
                request.TerminalId
            );

            // Audit event is emitted automatically by HandshakeService
            // Event: SESSION_ACTIVATED
            // Severity: INFO
            // Details: { terminal_id, session_id, timestamp, pairing_successful }

            return Ok(new
            {
                success = true,
                session_id = request.SessionId,
                message = "Session activated. Terminal is ready to vote."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating session");
            return StatusCode(500, new { error = "Failed to activate session" });
        }
    }

    /// <summary>
    /// US-SR-M6-03: Close session and custody vote
    /// 
    /// Request: { "terminal_id": "TERM-001", "session_id": "SESS-123", "vote_payload": "...", "reason": "VOTE_CAST" }
    /// Response: { "success": true, "vote_id": "..." }
    /// 
    /// Audit Events: 
    /// - SESSION_CLOSED_VOTE (INFO) if reason == "VOTE_CAST"
    /// - SESSION_CLOSED_TIMEOUT (INFO) if reason == "TIMEOUT"
    /// 
    /// Zero-Identity: Vote payload is encrypted; details only contain IDs
    /// </summary>
    [HttpPost("close")]
    public async Task<IActionResult> CloseSession(
        [FromBody] CloseSessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var reason = request.Reason; // "VOTE_CAST" or "TIMEOUT"

            if (reason == "VOTE_CAST")
            {
                // Custody the vote with audit event
                var custodiedVote = await _voteVault.CustodyVoteAsync(
                    request.VotePayload!,
                    request.TerminalId,
                    request.SessionId,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Vote custodied and session {SessionId} closed for terminal {TerminalId}",
                    request.SessionId,
                    request.TerminalId
                );

                // Audit event is emitted automatically by VoteVaultService
                // Event: SESSION_CLOSED_VOTE
                // Severity: INFO
                // Details: { terminal_id, session_id, vote_id, timestamp, custodied_at }

                return Ok(new
                {
                    success = true,
                    vote_id = custodiedVote.Id,
                    message = "Vote custodied securely. Session closed."
                });
            }
            else if (reason == "TIMEOUT")
            {
                // Close session without vote
                await _handshakeService.CloseSessionAsync(
                    request.TerminalId,
                    request.SessionId,
                    reason,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Session {SessionId} closed due to timeout for terminal {TerminalId}",
                    request.SessionId,
                    request.TerminalId
                );

                // Audit event is emitted automatically by HandshakeService
                // Event: SESSION_CLOSED_TIMEOUT
                // Severity: INFO
                // Details: { terminal_id, session_id, timestamp, reason }

                return Ok(new
                {
                    success = true,
                    message = "Session closed due to timeout."
                });
            }
            else
            {
                return BadRequest(new { error = "Invalid reason" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing session");
            return StatusCode(500, new { error = "Failed to close session" });
        }
    }

    // Request DTOs
    public record EmitHandshakeRequest(string TerminalId);

    public record ActivateSessionRequest(
        string TerminalId,
        string SessionId,
        string PairingCode
    );

    public record CloseSessionRequest(
        string TerminalId,
        string SessionId,
        string Reason, // "VOTE_CAST" or "TIMEOUT"
        string? VotePayload = null
    );
}
