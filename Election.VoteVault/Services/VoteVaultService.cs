using System.Security.Cryptography;
using System.Text;
using Election.Core.Interfaces;
using Election.VoteVault.Interfaces;
using Election.VoteVault.Models;
using Microsoft.Extensions.Logging;

namespace Election.VoteVault.Services;

/// <summary>
/// Manages secure vote custody with audit event emission.
/// Emits events for handshake-related actions (session opening/closing).
/// </summary>
public class VoteVaultService : IVoteVaultService
{
    private readonly List<CustodiedVote> _vault = new();
    private readonly RSA _rsa;
    private readonly ITransparencyAuditService _auditService;
    private readonly ILogger<VoteVaultService> _logger;

    public VoteVaultService(
        ITransparencyAuditService auditService,
        ILogger<VoteVaultService> logger
    )
    {
        _rsa = RSA.Create(2048);
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Custodies a vote and emits audit event.
    /// Implements SESSION_CLOSED_VOTE event for US-SR-M6-03.
    /// </summary>
    public async Task<CustodiedVote> CustodyVoteAsync(
        string payload,
        string terminalId,
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(payload);

            byte[] encrypted = _rsa.Encrypt(
                data,
                RSAEncryptionPadding.OaepSHA256
            );

            var vote = new CustodiedVote
            {
                EncryptedPayload = Convert.ToBase64String(encrypted)
            };

            _vault.Add(vote);

            _logger.LogInformation(
                "Vote custodied from terminal {TerminalId}, session {SessionId}",
                terminalId,
                sessionId
            );

            // Emit audit event: SESSION_CLOSED_VOTE
            // This indicates a successful vote was cast
            await _auditService.EmitHandshakeEventAsync(
                eventType: "SESSION_CLOSED_VOTE",
                terminalId: terminalId,
                sessionId: sessionId,
                additionalDetails: new()
                {
                    { "vote_id", vote.Id.ToString() },
                    { "custodied_at", vote.CreatedAt.ToString("O") }
                },
                cancellationToken: cancellationToken
            );

            return vote;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error custodying vote from terminal {TerminalId}",
                terminalId
            );

            throw;
        }
    }

    // Legacy sync version for compatibility
    public CustodiedVote CustodyVote(string payload)
    {
        byte[] data = Encoding.UTF8.GetBytes(payload);

        byte[] encrypted = _rsa.Encrypt(
            data,
            RSAEncryptionPadding.OaepSHA256
        );

        var vote = new CustodiedVote
        {
            EncryptedPayload = Convert.ToBase64String(encrypted)
        };

        _vault.Add(vote);

        return vote;
    }

    public int GetCustodiedVotesCount()
    {
        return _vault.Count;
    }

    public IEnumerable<CustodiedVote> GetVotes()
    {
        throw new UnauthorizedAccessException(
            "Access to custodial payloads is blocked until election closure."
        );
    }

    public IEnumerable<CustodiedVote> GetCustodiedVotesInternal()
    {
        return _vault;
    }
}