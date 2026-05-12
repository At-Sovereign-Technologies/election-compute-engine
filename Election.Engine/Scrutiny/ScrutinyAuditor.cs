using Election.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Election.Engine.Scrutiny;

/// <summary>
/// Implementation of scrutiny auditing for double truth verification.
/// </summary>
public class ScrutinyAuditor : IScrutinyAuditor
{
    private readonly ITransparencyAuditService _auditService;
    private readonly ILogger<ScrutinyAuditor> _logger;
    private readonly Dictionary<string, int> _qrScanHistory = new();

    public ScrutinyAuditor(
        ITransparencyAuditService auditService,
        ILogger<ScrutinyAuditor> logger
    )
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task RecordQrScanAsync(
        string juroId,
        string status, // "legitimate", "duplicate", "invalid"
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Track scan history for duplicate detection
            var scanKey = $"qr_{juroId}";
            if (!_qrScanHistory.ContainsKey(scanKey))
            {
                _qrScanHistory[scanKey] = 0;
            }

            _qrScanHistory[scanKey]++;

            _logger.LogInformation(
                "QR Scan recorded for jury {JuroId}: {Status} (Scan #{ScanCount})",
                juroId,
                status,
                _qrScanHistory[scanKey]
            );

            // Emit audit event with appropriate severity
            await _auditService.EmitQrScannedEventAsync(
                status: status,
                juroId: juroId,
                additionalDetails: additionalDetails,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error recording QR scan for jury {JuroId}",
                juroId
            );

            throw;
        }
    }

    public async Task RecordConciliationAttemptAsync(
        int digitalTotal,
        int physicalTotal,
        int juryCount,
        bool success,
        Dictionary<string, object>? additionalDetails = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Conciliation Attempt: Digital={Digital}, Physical={Physical}, Juries={Juries}, Success={Success}",
                digitalTotal,
                physicalTotal,
                juryCount,
                success
            );

            var details = additionalDetails ?? new();

            // Add variance analysis if totals don't match
            if (digitalTotal != physicalTotal)
            {
                var variance = Math.Abs(digitalTotal - physicalTotal);
                var variancePercentage = (variance * 100m) / Math.Max(digitalTotal, physicalTotal);

                details["variance"] = variance;
                details["variance_percentage"] = variancePercentage;
                details["discrepancy_flag"] = true;

                _logger.LogWarning(
                    "Conciliation variance detected: {Variance} votes ({VariancePercentage}%)",
                    variance,
                    variancePercentage
                );
            }

            // Emit audit event
            await _auditService.EmitConciliationAttemptEventAsync(
                digitalTotal: digitalTotal,
                physicalTotal: physicalTotal,
                juryCount: juryCount,
                success: success,
                additionalDetails: details,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error recording conciliation attempt"
            );

            throw;
        }
    }
}
