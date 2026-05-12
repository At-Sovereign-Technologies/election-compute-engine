using Election.Core.Interfaces;
using Election.Core.Models;
using Microsoft.Extensions.Logging;

namespace Election.Api.Services;

/// <summary>
/// Implements audit event emission to the Transparency Service (immutable ledger).
/// Provides non-blocking, fail-safe operation with local fallback logging.
/// </summary>
public class TransparencyAuditService : ITransparencyAuditService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TransparencyAuditService> _logger;

    public TransparencyAuditService(
        HttpClient httpClient,
        ILogger<TransparencyAuditService> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task EmitEventAsync(
        string eventType,
        string severity,
        Dictionary<string, object> details,
        CancellationToken cancellationToken = default
    )
    {
        var @event = new TransparencyEventRequest
        {
            EventType = eventType,
            Severity = severity,
            Details = details ?? new()
        };

        try
        {
            _logger.LogDebug(
                "Emitting audit event: {EventType} (Severity: {Severity})",
                eventType,
                severity
            );

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/transparency/events",
                @event,
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Audit event {EventType} successfully emitted",
                    eventType
                );
            }
            else
            {
                _logger.LogWarning(
                    "Transparency Service returned {StatusCode} for event {EventType}",
                    response.StatusCode,
                    eventType
                );

                // Log locally as fallback
                await LogEventLocally(@event);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to emit audit event {EventType} to Transparency Service. Falling back to local logging.",
                eventType
            );

            // Log locally as fallback when service is unavailable
            await LogEventLocally(@event);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                ex,
                "Timeout while emitting audit event {EventType}. Falling back to local logging.",
                eventType
            );

            await LogEventLocally(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while emitting audit event {EventType}",
                eventType
            );

            await LogEventLocally(@event);
        }
    }

    public async Task EmitHandshakeEventAsync(
        string eventType,
        string terminalId,
        string? sessionId = null,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    )
    {
        var details = new Dictionary<string, object>
        {
            { "terminal_id", terminalId },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            details["session_id"] = sessionId;
        }

        if (additionalDetails != null)
        {
            foreach (var kvp in additionalDetails)
            {
                details[kvp.Key] = kvp.Value;
            }
        }

        await EmitEventAsync(
            eventType,
            "INFO",
            details,
            cancellationToken
        );
    }

    public async Task EmitQrScannedEventAsync(
        string status, // "legitimate", "duplicate", "invalid"
        string juroId,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    )
    {
        var details = new Dictionary<string, object>
        {
            { "status", status },
            { "jury_id", juroId },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };

        if (additionalDetails != null)
        {
            foreach (var kvp in additionalDetails)
            {
                details[kvp.Key] = kvp.Value;
            }
        }

        // Use CRITICAL severity for duplicates, INFO otherwise
        var severity = status == "duplicate" ? "CRITICAL" : "INFO";

        await EmitEventAsync(
            "QR_SCANNED",
            severity,
            details,
            cancellationToken
        );
    }

    public async Task EmitConciliationAttemptEventAsync(
        int digitalTotal,
        int physicalTotal,
        int juryCount,
        bool success,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    )
    {
        var details = new Dictionary<string, object>
        {
            { "digital_total", digitalTotal },
            { "physical_total", physicalTotal },
            { "jury_count", juryCount },
            { "success", success },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };

        if (additionalDetails != null)
        {
            foreach (var kvp in additionalDetails)
            {
                details[kvp.Key] = kvp.Value;
            }
        }

        await EmitEventAsync(
            "CONCILIATION_ATTEMPT",
            success ? "INFO" : "MEDIUM",
            details,
            cancellationToken
        );
    }

    private async Task LogEventLocally(TransparencyEventRequest @event)
    {
        try
        {
            _logger.LogWarning(
                "LOCAL FALLBACK: Audit event {EventType} with severity {Severity}: {@Details}",
                @event.EventType,
                @event.Severity,
                @event.Details
            );

            // TODO: Persist to local audit queue for later synchronization
            // This could be implemented as:
            // - File-based queue in the bin/audit directory
            // - In-memory queue that syncs on reconnection
            // - Database fallback table

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Failed to log event locally. Event data may be lost: {EventType}",
                @event.EventType
            );
        }
    }
}
