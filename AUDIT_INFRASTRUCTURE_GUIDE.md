# Audit Infrastructure Implementation Guide

This document provides comprehensive examples of how to use the audit infrastructure for the "Sello Legítimo" electoral system.

## Overview

The audit infrastructure consists of:

1. **Transparency Service Integration** (`ITransparencyAuditService`)
   - Non-blocking async event emission
   - Fail-safe execution with local fallback logging
   - Automatic retry handling

2. **Handshake Protocol** (`IHandshakeService`)
   - Terminal pairing and session management
   - Automatic audit event emission (US-SR-M6-03)

3. **Double Truth Scrutiny** (`IScrutinyAuditor`)
   - QR scan verification tracking
   - Digital vs. physical vote conciliation
   - Automatic audit event emission (US-SR-M6-04)

## Key Principles

### Zero-Identity (Critical)
All audit events MUST NOT contain voter PII:
- ❌ NO: `voter_id`, `cedula`, `name`, `email`, `phone`
- ✅ YES: `terminal_id`, `session_id`, `jury_id`, `timestamp`

### Fail-Safe Execution
Operations are non-blocking and won't disrupt voting:
- If Transparency Service is down, events are logged locally
- Main application continues functioning
- Events can be synchronized later

### Event Severity Levels
- `INFO`: Normal operations
- `LOW`: Minor anomalies
- `MEDIUM`: Significant discrepancies
- `HIGH`: Major issues
- `CRITICAL`: Fraud indicators (duplicates, invalid QR codes)

---

## Usage Examples

### Example 1: Handshake Protocol (US-SR-M6-03)

#### Step 1: Emit Handshake

```csharp
// In controller or service
private readonly IHandshakeService _handshakeService;

public async Task<IActionResult> StartPairing(string terminalId)
{
    // Terminal sends pairing request
    var pairingCode = await _handshakeService.EmitHandshakeAsync(
        terminalId: terminalId,
        cancellationToken: cancellationToken
    );

    // Emitted Audit Event:
    // {
    //   "timestamp": "2026-05-12T10:30:00Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "HANDSHAKE_EMITTED",
    //   "severity": "INFO",
    //   "details": {
    //     "terminal_id": "TERM-001",
    //     "timestamp": "2026-05-12T10:30:00Z",
    //     "pairing_code_issued": true
    //   }
    // }

    return Ok(new { pairingCode });
}
```

#### Step 2: Activate Session

```csharp
public async Task<IActionResult> ConfirmPairing(
    string terminalId,
    string sessionId,
    string pairingCode
)
{
    var success = await _handshakeService.ActivateSessionAsync(
        terminalId: terminalId,
        sessionId: sessionId,
        pairingCode: pairingCode,
        cancellationToken: cancellationToken
    );

    // Emitted Audit Event:
    // {
    //   "timestamp": "2026-05-12T10:30:15Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "SESSION_ACTIVATED",
    //   "severity": "INFO",
    //   "details": {
    //     "terminal_id": "TERM-001",
    //     "session_id": "SESS-123ABC",
    //     "timestamp": "2026-05-12T10:30:15Z",
    //     "pairing_successful": true
    //   }
    // }

    return Ok(new { success, sessionId });
}
```

#### Step 3: Cast Vote and Close Session

```csharp
public async Task<IActionResult> CastVote(
    string terminalId,
    string sessionId,
    string encryptedVote
)
{
    // Custody the vote (emits SESSION_CLOSED_VOTE event)
    var custodiedVote = await _voteVault.CustodyVoteAsync(
        payload: encryptedVote,
        terminalId: terminalId,
        sessionId: sessionId,
        cancellationToken: cancellationToken
    );

    // Emitted Audit Event:
    // {
    //   "timestamp": "2026-05-12T10:31:00Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "SESSION_CLOSED_VOTE",
    //   "severity": "INFO",
    //   "details": {
    //     "terminal_id": "TERM-001",
    //     "session_id": "SESS-123ABC",
    //     "timestamp": "2026-05-12T10:31:00Z",
    //     "vote_id": "550e8400-e29b-41d4-a716-446655440000",
    //     "custodied_at": "2026-05-12T10:31:00Z"
    //   }
    // }

    return Ok(new { voteId = custodiedVote.Id });
}
```

#### Step 4: Session Timeout

```csharp
public async Task<IActionResult> TimeoutSession(
    string terminalId,
    string sessionId
)
{
    // Close session due to inactivity
    await _handshakeService.CloseSessionAsync(
        terminalId: terminalId,
        sessionId: sessionId,
        reason: "TIMEOUT",
        cancellationToken: cancellationToken
    );

    // Emitted Audit Event:
    // {
    //   "timestamp": "2026-05-12T10:35:00Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "SESSION_CLOSED_TIMEOUT",
    //   "severity": "INFO",
    //   "details": {
    //     "terminal_id": "TERM-001",
    //     "session_id": "SESS-123ABC",
    //     "timestamp": "2026-05-12T10:35:00Z",
    //     "reason": "TIMEOUT"
    //   }
    // }

    return Ok(new { message = "Session closed due to timeout" });
}
```

---

### Example 2: Double Truth Scrutiny (US-SR-M6-04)

#### QR Code Scanning with Duplicate Detection

```csharp
public async Task<IActionResult> ScanQrCode(
    string juryId,
    string qrCodeData
)
{
    // Determine QR status (legitimate, duplicate, invalid)
    string status = DetermineQrStatus(qrCodeData);

    // Record the scan (emits QR_SCANNED event)
    await _scrutinyAuditor.RecordQrScanAsync(
        juroId: juryId,
        status: status,
        additionalDetails: new()
        {
            { "qr_hash", ComputeHash(qrCodeData) },
            { "scan_timestamp", DateTime.UtcNow.ToString("O") }
        },
        cancellationToken: cancellationToken
    );

    // Emitted Audit Event (LEGITIMATE):
    // {
    //   "timestamp": "2026-05-12T11:00:00Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "QR_SCANNED",
    //   "severity": "INFO",
    //   "details": {
    //     "status": "legitimate",
    //     "jury_id": "JURY-001",
    //     "timestamp": "2026-05-12T11:00:00Z",
    //     "qr_hash": "a1b2c3d4e5f6...",
    //     "scan_timestamp": "2026-05-12T11:00:00Z"
    //   }
    // }

    // Emitted Audit Event (DUPLICATE - CRITICAL):
    // {
    //   "timestamp": "2026-05-12T11:00:30Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "QR_SCANNED",
    //   "severity": "CRITICAL",
    //   "details": {
    //     "status": "duplicate",
    //     "jury_id": "JURY-001",
    //     "timestamp": "2026-05-12T11:00:30Z",
    //     "qr_hash": "a1b2c3d4e5f6...",
    //     "scan_timestamp": "2026-05-12T11:00:30Z"
    //   }
    // }

    return Ok(new { status });
}

private string DetermineQrStatus(string qrCodeData)
{
    // Check if QR has been scanned before
    if (QrAlreadyScanned(qrCodeData))
    {
        return "duplicate"; // CRITICAL severity
    }

    // Validate QR code format
    if (!IsValidQrFormat(qrCodeData))
    {
        return "invalid"; // INFO severity
    }

    return "legitimate"; // INFO severity
}
```

#### Vote Conciliation (Digital vs. Physical Count)

```csharp
public async Task<IActionResult> ConciliateVotes(
    int digitalCount,
    int physicalCount,
    int juryCount
)
{
    bool conciliationSuccess = digitalCount == physicalCount;

    // Record conciliation attempt
    await _scrutinyAuditor.RecordConciliationAttemptAsync(
        digitalTotal: digitalCount,
        physicalTotal: physicalCount,
        juryCount: juryCount,
        success: conciliationSuccess,
        additionalDetails: new()
        {
            { "conciliation_method", "automated_count" },
            { "audit_timestamp", DateTime.UtcNow.ToString("O") }
        },
        cancellationToken: cancellationToken
    );

    // Emitted Audit Event (SUCCESS):
    // {
    //   "timestamp": "2026-05-12T12:00:00Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "CONCILIATION_ATTEMPT",
    //   "severity": "INFO",
    //   "details": {
    //     "digital_total": 1000,
    //     "physical_total": 1000,
    //     "jury_count": 5,
    //     "success": true,
    //     "timestamp": "2026-05-12T12:00:00Z",
    //     "conciliation_method": "automated_count",
    //     "audit_timestamp": "2026-05-12T12:00:00Z"
    //   }
    // }

    // Emitted Audit Event (FAILURE with VARIANCE):
    // {
    //   "timestamp": "2026-05-12T12:00:30Z",
    //   "originComponent": "COMPUTE_ENGINE",
    //   "eventType": "CONCILIATION_ATTEMPT",
    //   "severity": "MEDIUM",
    //   "details": {
    //     "digital_total": 1000,
    //     "physical_total": 995,
    //     "jury_count": 5,
    //     "success": false,
    //     "variance": 5,
    //     "variance_percentage": 0.5,
    //     "timestamp": "2026-05-12T12:00:30Z",
    //     "conciliation_method": "automated_count",
    //     "audit_timestamp": "2026-05-12T12:00:30Z"
    //   }
    // }

    return Ok(new
    {
        success = conciliationSuccess,
        digital_count = digitalCount,
        physical_count = physicalCount,
        variance = Math.Abs(digitalCount - physicalCount)
    });
}
```

---

### Example 3: Dependency Injection Setup

```csharp
// In Program.cs

// 1. Configure HttpClient for Transparency Service
builder.Services
    .AddHttpClient<ITransparencyAuditService, TransparencyAuditService>(client =>
    {
        client.BaseAddress = new Uri("http://transparency-service:8080");
        client.Timeout = TimeSpan.FromSeconds(5);
    });

// 2. Register audit services
builder.Services.AddSingleton<IHandshakeService, HandshakeService>();
builder.Services.AddSingleton<IScrutinyAuditor, ScrutinyAuditor>();

// 3. Register vote vault service with audit support
builder.Services.AddSingleton<IVoteVaultService, VoteVaultService>();

// 4. Register election method with audit support
builder.Services.AddSingleton<IMetodoElectoral, AlternativeVoteMethod>();
```

---

### Example 4: Injecting in a Custom Service

```csharp
public class VoteProcessingService
{
    private readonly ITransparencyAuditService _auditService;
    private readonly ILogger<VoteProcessingService> _logger;

    public VoteProcessingService(
        ITransparencyAuditService auditService,
        ILogger<VoteProcessingService> logger
    )
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task ProcessVoteAsync(string voteData)
    {
        try
        {
            // Process vote...

            // Emit custom audit event
            await _auditService.EmitEventAsync(
                eventType: "CUSTOM_VOTE_PROCESSING",
                severity: "INFO",
                details: new()
                {
                    { "processing_timestamp", DateTime.UtcNow.ToString("O") },
                    { "status", "success" }
                },
                cancellationToken: CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing vote");

            // Emit error event
            await _auditService.EmitEventAsync(
                eventType: "VOTE_PROCESSING_ERROR",
                severity: "HIGH",
                details: new()
                {
                    { "error_timestamp", DateTime.UtcNow.ToString("O") },
                    { "error_type", ex.GetType().Name }
                },
                cancellationToken: CancellationToken.None
            );
        }
    }
}
```

---

## Configuration

### appsettings.json

```json
{
  "TransparencyService": {
    "BaseUrl": "http://localhost:8080",
    "Timeout": 5000
  },
  "Logging": {
    "LogLevel": {
      "Election.Api.Services.TransparencyAuditService": "Information",
      "Election.Engine.Scrutiny.ScrutinyAuditor": "Information"
    }
  }
}
```

---

## Testing

### Unit Test Example

```csharp
[TestClass]
public class HandshakeServiceTests
{
    private Mock<ITransparencyAuditService> _mockAuditService;
    private HandshakeService _handshakeService;

    [TestInitialize]
    public void Setup()
    {
        _mockAuditService = new Mock<ITransparencyAuditService>();
        var logger = new Mock<ILogger<HandshakeService>>();

        _handshakeService = new HandshakeService(_mockAuditService.Object, logger.Object);
    }

    [TestMethod]
    public async Task EmitHandshakeAsync_ShouldEmitAuditEvent()
    {
        // Arrange
        var terminalId = "TERM-001";

        // Act
        var pairingCode = await _handshakeService.EmitHandshakeAsync(terminalId);

        // Assert
        Assert.IsNotNull(pairingCode);
        _mockAuditService.Verify(
            x => x.EmitHandshakeEventAsync(
                It.Is<string>(e => e == "HANDSHAKE_EMITTED"),
                It.Is<string>(t => t == terminalId),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
```

---

## API Endpoints Summary

| Endpoint | Method | Use Case |
|----------|--------|----------|
| `/api/handshake/emit` | POST | Start terminal pairing (HANDSHAKE_EMITTED) |
| `/api/handshake/activate` | POST | Confirm pairing code (SESSION_ACTIVATED) |
| `/api/handshake/close` | POST | Close session (SESSION_CLOSED_VOTE or SESSION_CLOSED_TIMEOUT) |
| `/api/scrutiny/qr-scan` | POST | Record QR scan result (QR_SCANNED) |
| `/api/scrutiny/conciliation` | POST | Record vote conciliation (CONCILIATION_ATTEMPT) |

---

## Troubleshooting

### Transparency Service Unavailable
- Events are logged locally as fallback
- Check logs: `LocalFallback: Audit event ...`
- Implement retry synchronization

### Missing Audit Events
- Verify `ITransparencyAuditService` is registered in DI
- Check HttpClient configuration
- Review application logs for errors

### PII Detected in Events
- Verify all `details` dictionaries
- Use helper methods to sanitize input
- Review the ScrutinyController `IsVoterPII()` implementation
