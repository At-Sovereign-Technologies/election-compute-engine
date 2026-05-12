# Quick Reference: Audit Infrastructure

## Key Components

### 1. Core DTO

**File:** `Election.Core/Models/TransparencyEventRequest.cs`

```csharp
public class TransparencyEventRequest
{
    public string Timestamp { get; set; }              // ISO 8601
    public string OriginComponent { get; set; }        // "COMPUTE_ENGINE"
    public string EventType { get; set; }              // "HANDSHAKE_EMITTED", "QR_SCANNED", etc.
    public string Severity { get; set; }               // "INFO", "LOW", "MEDIUM", "HIGH", "CRITICAL"
    public Dictionary<string, object> Details { get; set; }  // Zero-identity details only
}
```

### 2. Core Service Interface

**File:** `Election.Core/Interfaces/ITransparencyAuditService.cs`

```csharp
public interface ITransparencyAuditService
{
    Task EmitEventAsync(string eventType, string severity, Dictionary<string, object> details, CancellationToken ct);
    Task EmitHandshakeEventAsync(string eventType, string terminalId, string? sessionId, Dictionary<string, object>? details, CancellationToken ct);
    Task EmitQrScannedEventAsync(string status, string juroId, Dictionary<string, object>? details, CancellationToken ct);
    Task EmitConciliationAttemptEventAsync(int digitalTotal, int physicalTotal, int juryCount, bool success, Dictionary<string, object>? details, CancellationToken ct);
}
```

### 3. Handshake Service (US-SR-M6-03)

**File:** `Election.VoteVault/Ceremony/Services/HandshakeService.cs`

```csharp
public interface IHandshakeService
{
    Task<string> EmitHandshakeAsync(string terminalId, CancellationToken ct);
    Task<bool> ActivateSessionAsync(string terminalId, string sessionId, string pairingCode, CancellationToken ct);
    Task CloseSessionAsync(string terminalId, string sessionId, string reason, CancellationToken ct);
}
```

**Audit Events Emitted:**

- `HANDSHAKE_EMITTED` (INFO) - when pairing code is generated
- `SESSION_ACTIVATED` (INFO) - when pairing is confirmed
- `SESSION_CLOSED_VOTE` (INFO) - when vote is cast
- `SESSION_CLOSED_TIMEOUT` (INFO) - when session expires

### 4. Scrutiny Auditor (US-SR-M6-04)

**File:** `Election.Engine/Scrutiny/ScrutinyAuditor.cs`

```csharp
public interface IScrutinyAuditor
{
    Task RecordQrScanAsync(string juroId, string status, Dictionary<string, object>? details, CancellationToken ct);
    Task RecordConciliationAttemptAsync(int digitalTotal, int physicalTotal, int juryCount, bool success, Dictionary<string, object>? details, CancellationToken ct);
}
```

**Audit Events Emitted:**

- `QR_SCANNED` (INFO/CRITICAL) - when QR code is scanned (CRITICAL for duplicates)
- `CONCILIATION_ATTEMPT` (INFO/MEDIUM) - when vote counts are reconciled

### 5. Implementation Service

**File:** `Election.Api/Services/TransparencyAuditService.cs`

- Implements `ITransparencyAuditService`
- Uses `HttpClient` to POST to Transparency Service
- Handles failures with local fallback logging
- Non-blocking async operations

---

## Event Emission Examples

### Handshake Workflow

```
Terminal → /api/handshake/emit
           ↓
           HANDSHAKE_EMITTED (terminal_id, timestamp)
           ↓
           [Get pairing code: 123456]

Terminal → /api/handshake/activate (with pairing code)
           ↓
           SESSION_ACTIVATED (terminal_id, session_id, timestamp)
           ↓
           [Session active for voting]

Terminal → /api/handshake/close (with vote_payload)
           ↓
           SESSION_CLOSED_VOTE (terminal_id, session_id, vote_id, timestamp)
           ↓
           [Vote custodied securely]
```

### Scrutiny Workflow

```
Admin → /api/scrutiny/qr-scan (jury_id, status="legitimate")
        ↓
        QR_SCANNED (severity=INFO, jury_id, status, timestamp)

Admin → /api/scrutiny/qr-scan (jury_id, status="duplicate")
        ↓
        QR_SCANNED (severity=CRITICAL, jury_id, status, timestamp)
        ↓
        🚨 FRAUD ALERT

Admin → /api/scrutiny/conciliation (digital=1000, physical=1000)
        ↓
        CONCILIATION_ATTEMPT (severity=INFO, digital_total=1000, physical_total=1000, success=true)
```

---

## Dependency Injection Setup

```csharp
// In Program.cs

// 1. Register HttpClient for Transparency Service
builder.Services
    .AddHttpClient<ITransparencyAuditService, TransparencyAuditService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:8080");
        client.Timeout = TimeSpan.FromSeconds(5);
    });

// 2. Register handshake and scrutiny services
builder.Services.AddSingleton<IHandshakeService, HandshakeService>();
builder.Services.AddSingleton<IScrutinyAuditor, ScrutinyAuditor>();
```

---

## Updated Services

### VoteVaultService

**File:** `Election.VoteVault/Services/VoteVaultService.cs`

NEW: Async method for custody with audit event emission

```csharp
public async Task<CustodiedVote> CustodyVoteAsync(
    string payload,
    string terminalId,
    string sessionId,
    CancellationToken cancellationToken = default
)
{
    // Encrypts and stores vote
    // Emits SESSION_CLOSED_VOTE event automatically
    // Returns: CustodiedVote with ID and timestamp
}
```

### AlternativeVoteMethod

**File:** `Election.Engine/Methods/AlternativeVote/AlternativeVoteMethod.cs`

NEW: Async method for calculation with audit event emission

```csharp
public async Task<Resultado> CalcularResultadoAsync(
    CancellationToken cancellationToken = default
)
{
    // Performs instant runoff voting calculation
    // Emits CONCILIATION_ATTEMPT event automatically
    // Returns: Final result with winner and percentages
}
```

---

## API Endpoints

### Handshake Endpoints

| Method | Path                      | Emits             | Returns             |
| ------ | ------------------------- | ----------------- | ------------------- |
| POST   | `/api/handshake/emit`     | HANDSHAKE_EMITTED | pairing_code        |
| POST   | `/api/handshake/activate` | SESSION_ACTIVATED | success, session_id |
| POST   | `/api/handshake/close`    | SESSION*CLOSED*\* | success, vote_id?   |

### Scrutiny Endpoints

| Method | Path                         | Emits                | Returns                                |
| ------ | ---------------------------- | -------------------- | -------------------------------------- |
| POST   | `/api/scrutiny/qr-scan`      | QR_SCANNED           | success, status, severity              |
| POST   | `/api/scrutiny/conciliation` | CONCILIATION_ATTEMPT | success, variance, variance_percentage |

---

## Configuration

**File:** `Election.Api/appsettings.json`

```json
{
    "TransparencyService": {
        "BaseUrl": "http://localhost:8080", // Transparency Service URL
        "Timeout": 5000 // Request timeout in milliseconds
    }
}
```

---

## Zero-Identity Compliance Checklist

✅ NO voter PII in audit events
✅ Only terminal_id, session_id, jury_id, vote_id allowed
✅ No voter names, documents, emails, phone numbers
✅ No encrypted vote content in details
✅ Only aggregated counts (digital_total, physical_total)
✅ Timestamp always included (ISO 8601 format)

---

## Error Handling & Fallback

If Transparency Service is unavailable:

1. Request fails with HttpRequestException or TaskCanceledException
2. Event is logged locally with `_logger.LogWarning("LOCAL FALLBACK: ...")`
3. Main application continues unaffected
4. Event data available for later synchronization

```csharp
// Local fallback logging location (in logs):
// "LOCAL FALLBACK: Audit event {EventType} with severity {Severity}: {@Details}"
```

---

## Testing Quick Start

Mock the audit service in tests:

```csharp
var mockAuditService = new Mock<ITransparencyAuditService>();
mockAuditService
    .Setup(x => x.EmitHandshakeEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

var service = new HandshakeService(mockAuditService.Object, logger);
```

---

## Documentation Files

| File                            | Purpose                                          |
| ------------------------------- | ------------------------------------------------ |
| `AUDIT_INFRASTRUCTURE_GUIDE.md` | Comprehensive implementation guide with examples |
| `IMPLEMENTATION_COMPLETE.md`    | Full project summary and checklist               |
| `QUICK_REFERENCE.md`            | This file - quick lookup                         |

---

## Troubleshooting

| Issue                 | Solution                                                       |
| --------------------- | -------------------------------------------------------------- |
| Audit events not sent | Check Transparency Service URL and port in appsettings.json    |
| HttpRequestException  | Verify Transparency Service is running and accessible          |
| TaskCanceledException | Increase timeout in appsettings.json                           |
| PII in audit events   | Verify details dictionary doesn't contain voter_id, name, etc. |
| Missing audit events  | Verify ITransparencyAuditService is registered in DI           |

---

**Last Updated:** 2026-05-12
**Branch:** `feature/auditoria-SR-M6`
**Status:** Ready for testing
