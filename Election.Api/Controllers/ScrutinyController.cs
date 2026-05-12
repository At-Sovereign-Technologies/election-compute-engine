using Election.Engine.Scrutiny;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

/// <summary>
/// Double truth scrutiny audit controller.
/// Implements US-SR-M6-04: Double Truth Scrutiny Audit.
/// 
/// This controller demonstrates how QR scans and conciliation attempts
/// are recorded and emitted as audit events to the Transparency Service.
/// </summary>
[ApiController]
[Route("api/scrutiny")]
public class ScrutinyController : ControllerBase
{
    private readonly IScrutinyAuditor _scrutinyAuditor;
    private readonly ILogger<ScrutinyController> _logger;

    public ScrutinyController(
        IScrutinyAuditor scrutinyAuditor,
        ILogger<ScrutinyController> logger
    )
    {
        _scrutinyAuditor = scrutinyAuditor;
        _logger = logger;
    }

    /// <summary>
    /// US-SR-M6-04: Record QR code scan result
    /// 
    /// Request: {
    ///   "jury_id": "JURY-001",
    ///   "status": "legitimate|duplicate|invalid",
    ///   "additional_details": { ... }
    /// }
    /// Response: { "success": true, "message": "..." }
    /// 
    /// Audit Event: QR_SCANNED
    /// Severity: CRITICAL for duplicates, INFO otherwise
    /// 
    /// Zero-Identity: Only jury_id (no voter data)
    /// Details: { status, jury_id, timestamp, scan_sequence? }
    /// </summary>
    [HttpPost("qr-scan")]
    public async Task<IActionResult> RecordQrScan(
        [FromBody] QrScanRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Validate status
            if (!new[] { "legitimate", "duplicate", "invalid" }.Contains(request.Status))
            {
                return BadRequest(new { error = "Invalid QR scan status" });
            }

            var additionalDetails = new Dictionary<string, object>();

            if (request.AdditionalDetails != null)
            {
                foreach (var kvp in request.AdditionalDetails)
                {
                    // CRITICAL: Ensure no voter PII is included
                    if (IsVoterPII(kvp.Key))
                    {
                        _logger.LogWarning(
                            "Attempt to include voter PII in QR scan audit event: {Key}",
                            kvp.Key
                        );
                        continue; // Skip this field
                    }

                    additionalDetails[kvp.Key] = kvp.Value;
                }
            }

            await _scrutinyAuditor.RecordQrScanAsync(
                juroId: request.JuryId,
                status: request.Status,
                additionalDetails: additionalDetails,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "QR scan recorded: Jury={JuryId}, Status={Status}",
                request.JuryId,
                request.Status
            );

            // Audit event is emitted automatically by ScrutinyAuditor
            // Event: QR_SCANNED
            // Severity: CRITICAL (if duplicate) or INFO
            // Details: { status, jury_id, timestamp, ...additionalDetails }

            var message = request.Status switch
            {
                "legitimate" => "QR code verified as legitimate",
                "duplicate" => "ALERT: QR code is a duplicate (possible fraud attempt)",
                "invalid" => "QR code is invalid",
                _ => "QR scan recorded"
            };

            return Ok(new
            {
                success = true,
                message,
                status = request.Status,
                severity = request.Status == "duplicate" ? "CRITICAL" : "INFO"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording QR scan");
            return StatusCode(500, new { error = "Failed to record QR scan" });
        }
    }

    /// <summary>
    /// US-SR-M6-04: Record conciliation attempt (digital vs. physical vote counts)
    /// 
    /// Request: {
    ///   "digital_total": 1000,
    ///   "physical_total": 1000,
    ///   "jury_count": 5,
    ///   "success": true,
    ///   "additional_details": { "notes": "..." }
    /// }
    /// Response: { "success": true, "variance": 0, "variance_percentage": 0 }
    /// 
    /// Audit Event: CONCILIATION_ATTEMPT
    /// Severity: INFO (success) or MEDIUM (failure/variance)
    /// 
    /// Zero-Identity: No voter data, only aggregated counts and jury info
    /// Details: {
    ///   digital_total, physical_total, jury_count, success,
    ///   variance?, variance_percentage?, timestamp
    /// }
    /// </summary>
    [HttpPost("conciliation")]
    public async Task<IActionResult> RecordConciliation(
        [FromBody] ConciliationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var additionalDetails = new Dictionary<string, object>();

            if (request.AdditionalDetails != null)
            {
                foreach (var kvp in request.AdditionalDetails)
                {
                    // CRITICAL: Ensure no voter PII is included
                    if (IsVoterPII(kvp.Key))
                    {
                        _logger.LogWarning(
                            "Attempt to include voter PII in conciliation audit event: {Key}",
                            kvp.Key
                        );
                        continue; // Skip this field
                    }

                    additionalDetails[kvp.Key] = kvp.Value;
                }
            }

            await _scrutinyAuditor.RecordConciliationAttemptAsync(
                digitalTotal: request.DigitalTotal,
                physicalTotal: request.PhysicalTotal,
                juryCount: request.JuryCount,
                success: request.Success,
                additionalDetails: additionalDetails,
                cancellationToken: cancellationToken
            );

            // Calculate variance
            var variance = Math.Abs(request.DigitalTotal - request.PhysicalTotal);
            var variancePercentage = request.DigitalTotal > 0
                ? (variance * 100m) / request.DigitalTotal
                : 0m;

            _logger.LogInformation(
                "Conciliation recorded: Digital={Digital}, Physical={Physical}, Variance={Variance} ({VariancePercentage}%), Success={Success}",
                request.DigitalTotal,
                request.PhysicalTotal,
                variance,
                variancePercentage,
                request.Success
            );

            // Audit event is emitted automatically by ScrutinyAuditor
            // Event: CONCILIATION_ATTEMPT
            // Severity: INFO (success) or MEDIUM (variance detected)
            // Details: { digital_total, physical_total, jury_count, success, variance, variance_percentage, timestamp }

            return Ok(new
            {
                success = true,
                digital_total = request.DigitalTotal,
                physical_total = request.PhysicalTotal,
                variance,
                variance_percentage = variancePercentage,
                conciliation_success = request.Success,
                message = request.Success
                    ? "Conciliation successful"
                    : "Conciliation failed - variance detected"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording conciliation");
            return StatusCode(500, new { error = "Failed to record conciliation" });
        }
    }

    /// <summary>
    /// Helper method to detect and prevent voter PII in audit events.
    /// Implements the "Zero-Identity" principle.
    /// </summary>
    private bool IsVoterPII(string fieldName)
    {
        var piiPatterns = new[]
        {
            "voter_id", "voter_name", "voter_document", "voter_email", "voter_phone",
            "cedula", "document_number", "dni", "passport",
            "name", "surname", "email", "phone",
            "personal_id", "identification",
            "cédula" // Spanish variant
        };

        return piiPatterns.Any(
            pattern => fieldName.Equals(pattern, StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains(pattern, StringComparison.OrdinalIgnoreCase)
        );
    }

    // Request DTOs
    public record QrScanRequest(
        string JuryId,
        string Status, // "legitimate", "duplicate", "invalid"
        Dictionary<string, object>? AdditionalDetails = null
    );

    public record ConciliationRequest(
        int DigitalTotal,
        int PhysicalTotal,
        int JuryCount,
        bool Success,
        Dictionary<string, object>? AdditionalDetails = null
    );
}
