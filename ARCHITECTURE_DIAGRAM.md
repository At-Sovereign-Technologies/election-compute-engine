# Audit Infrastructure Architecture

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       SELLO LEGÍTIMO ELECTION SYSTEM                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────────────┐       ┌──────────────────────────┐   │
│  │  COMPUTE ENGINE              │       │  TRANSPARENCY SERVICE    │   │
│  │  (Election.Api)              │       │  (Java - Immutable Log)  │   │
│  ├──────────────────────────────┤       ├──────────────────────────┤   │
│  │                              │       │                          │   │
│  │  Controllers:                │       │  POST /api/v1/           │   │
│  │  ├─ HandshakeController      │──────→│    transparency/events   │   │
│  │  │  └─ emit, activate, close │ HTTP  │                          │   │
│  │  ├─ ScrutinyController       │       │  Stores immutable audit  │   │
│  │  │  └─ qr-scan, conciliation │       │  events (ledger)         │   │
│  │  └─ ElectionController       │       │                          │   │
│  │                              │       │  Features:               │   │
│  │  Services:                   │       │  ├─ Tamper-proof         │   │
│  │  ├─ TransparencyAuditService │       │  ├─ Chronological order  │   │
│  │  │  (HttpClient-based)       │       │  ├─ Cryptographic hash   │   │
│  │  ├─ HandshakeService         │       │  └─ Full auditability    │   │
│  │  │  (US-SR-M6-03)            │       │                          │   │
│  │  ├─ ScrutinyAuditor          │       │                          │   │
│  │  │  (US-SR-M6-04)            │       │                          │   │
│  │  ├─ VoteVaultService         │       │                          │   │
│  │  └─ AlternativeVoteMethod    │       │                          │   │
│  │     (IMetodoElectoral)       │       │                          │   │
│  │                              │       │                          │   │
│  │  Models:                     │       │                          │   │
│  │  ├─ TransparencyEventRequest │       │                          │   │
│  │  ├─ CustodiedVote            │       │                          │   │
│  │  └─ Resultado                │       │                          │   │
│  │                              │       │                          │   │
│  └──────────────────────────────┘       └──────────────────────────┘   │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  AUDIT EVENT FLOW                                               │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │                                                                 │   │
│  │  Terminal      HandshakeController    IHandshakeService        │   │
│  │    │                   │                     │                 │   │
│  │    ├─→ emit pairing    │                     │                 │   │
│  │    │   ────────────────→ EmitHandshakeAsync  │                 │   │
│  │    │                   │                     │                 │   │
│  │    │                   │      ITransparencyAuditService        │   │
│  │    │                   │                │                      │   │
│  │    │                   └─────────────────→ EmitHandshakeEventAsync
│  │    │                                       │                   │   │
│  │    │                                       ├─→ TransparencyEventRequest
│  │    │                                       │   ├─ eventType: "HANDSHAKE_EMITTED"
│  │    │                                       │   ├─ severity: "INFO"
│  │    │                                       │   └─ details: {terminal_id, timestamp}
│  │    │                                       │                   │   │
│  │    │                                       ├─→ HttpClient.PostAsJsonAsync()
│  │    │                                       │   └─→ Transparency Service
│  │    │                                       │                   │   │
│  │    │                    Pairing Code ←────┤                   │   │
│  │    │←───────────────────────────────────────                  │   │
│  │                                                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Dependency Injection Graph

```
Program.cs
│
├─→ HttpClientFactory
│   └─→ ITransparencyAuditService (TransparencyAuditService)
│       ├─ HttpClient (pre-configured with BaseUrl, Timeout)
│       └─ ILogger<TransparencyAuditService>
│
├─→ IHandshakeService (HandshakeService)
│   ├─ ITransparencyAuditService
│   └─ ILogger<HandshakeService>
│
├─→ IScrutinyAuditor (ScrutinyAuditor)
│   ├─ ITransparencyAuditService
│   └─ ILogger<ScrutinyAuditor>
│
├─→ IMetodoElectoral (AlternativeVoteMethod)
│   ├─ IScrutinyAuditor
│   └─ ILogger<AlternativeVoteMethod>
│
├─→ IVoteVaultService (VoteVaultService)
│   ├─ ITransparencyAuditService
│   └─ ILogger<VoteVaultService>
│
├─→ ISealService (SealService)
│
├─→ IOpeningCeremonyService (OpeningCeremonyService)
│
└─→ VaultHeartbeatWorker (Hosted Service)
```

## US-SR-M6-03: Handshake Audit Event Sequence

```
Terminal                Compute Engine           Transparency Service
   │                           │                          │
   │──── POST /emit ────────────│                          │
   │                           │                          │
   │                    HandshakeService                  │
   │                    ├─ Generate pairing code          │
   │                    └─ HANDSHAKE_EMITTED              │
   │                           │                          │
   │                    TransparencyAuditService          │
   │                           │                          │
   │                           ├─ TransparencyEventRequest│
   │                           │  ├─ eventType: "HANDSHAKE_EMITTED"
   │                           │  ├─ severity: "INFO"
   │                           │  └─ details: {terminal_id, timestamp, pairing_code_issued}
   │                           │                          │
   │◄────── pairing code ───────│  POST /api/v1/transparency/events
   │                           │────────────────────────→ │
   │                           │                          │
   │                           │◄──── 200 OK ────────────│
   │                           │                          │
   │                           │                  (Event stored in immutable log)
   │
   │──── POST /activate ───────→│
   │  (with pairing code)       │
   │                    HandshakeService
   │                    ├─ Validate pairing code
   │                    ├─ Create session
   │                    └─ SESSION_ACTIVATED
   │                           │
   │                    TransparencyAuditService
   │                           │
   │                           ├─ TransparencyEventRequest
   │                           │  ├─ eventType: "SESSION_ACTIVATED"
   │                           │  ├─ severity: "INFO"
   │                           │  └─ details: {terminal_id, session_id, timestamp, pairing_successful}
   │                           │
   │◄──── session active ──────│  POST /api/v1/transparency/events
   │                           │────────────────────────→ │
   │
   │──── POST /close (vote) ───→│
   │  (with encrypted vote)     │
   │                    VoteVaultService
   │                    ├─ Encrypt and store vote
   │                    └─ SESSION_CLOSED_VOTE
   │                           │
   │                    TransparencyAuditService
   │                           │
   │                           ├─ TransparencyEventRequest
   │                           │  ├─ eventType: "SESSION_CLOSED_VOTE"
   │                           │  ├─ severity: "INFO"
   │                           │  └─ details: {terminal_id, session_id, vote_id, timestamp, custodied_at}
   │                           │
   │◄──── vote custodied ──────│  POST /api/v1/transparency/events
   │                           │────────────────────────→ │
   │                           │                          │
```

## US-SR-M6-04: Double Truth Scrutiny Event Sequence

```
Admin / Jury              Compute Engine           Transparency Service
   │                           │                          │
   │──── POST /qr-scan ────────→│                          │
   │  (jury_id, status)         │                          │
   │                    ScrutinyAuditor                    │
   │                    ├─ Track scan history              │
   │                    ├─ Detect duplicates               │
   │                    └─ QR_SCANNED (INFO or CRITICAL)   │
   │                           │                          │
   │                    TransparencyAuditService          │
   │                           │                          │
   │        (if status="legitimate" or "invalid")         │
   │                    TransparencyEventRequest          │
   │                    ├─ eventType: "QR_SCANNED"        │
   │                    ├─ severity: "INFO"               │
   │                    └─ details: {status, jury_id, timestamp}
   │                           │
   │                           ├─ POST /api/v1/transparency/events
   │                           │────────────────────────→ │
   │
   │        (if status="duplicate") ⚠️ FRAUD ALERT        │
   │                    TransparencyEventRequest          │
   │                    ├─ eventType: "QR_SCANNED"        │
   │                    ├─ severity: "CRITICAL"           │
   │                    └─ details: {status, jury_id, timestamp}
   │                           │
   │◄────── CRITICAL ALERT ────│  POST /api/v1/transparency/events
   │                           │────────────────────────→ │ (severity=CRITICAL)
   │                           │                          │
   │
   │──── POST /conciliation ───→│
   │  (digital_total,           │
   │   physical_total,          │
   │   jury_count)              │
   │                    ScrutinyAuditor                    │
   │                    ├─ Calculate variance              │
   │                    ├─ Compare counts                  │
   │                    └─ CONCILIATION_ATTEMPT            │
   │                           │                          │
   │                    TransparencyAuditService          │
   │                           │                          │
   │        (if digital == physical)                      │
   │                    TransparencyEventRequest          │
   │                    ├─ eventType: "CONCILIATION_ATTEMPT"
   │                    ├─ severity: "INFO"               │
   │                    ├─ success: true                  │
   │                    └─ details: {digital_total, physical_total, jury_count}
   │                           │
   │                           ├─ POST /api/v1/transparency/events
   │                           │────────────────────────→ │
   │
   │        (if digital != physical)                      │
   │                    TransparencyEventRequest          │
   │                    ├─ eventType: "CONCILIATION_ATTEMPT"
   │                    ├─ severity: "MEDIUM"             │
   │                    ├─ success: false                 │
   │                    ├─ variance: 5                    │
   │                    └─ variance_percentage: 0.5       │
   │                           │
   │◄──── variance detected ────│  POST /api/v1/transparency/events
   │                           │────────────────────────→ │ (severity=MEDIUM)
   │                           │                          │
```

## Data Flow: Zero-Identity Principle

```
Voter Vote (PII-laden)
    │
    ├─ voter_id ❌ EXCLUDED
    ├─ voter_name ❌ EXCLUDED
    ├─ voter_document ❌ EXCLUDED
    │
    ↓
Terminal ID (allowed)
    │
    ├─ terminal_id ✅ INCLUDED
    ├─ session_id ✅ INCLUDED
    ├─ timestamp ✅ INCLUDED
    │
    ↓
Audit Event
    │
    ├─ No voter PII ✅
    ├─ Aggregated counts only ✅
    ├─ Jury member IDs (not voter IDs) ✅
    │
    ↓
Transparency Service (Immutable Log)
    │
    └─ Complete audit trail with zero PII ✅
```

## Error Handling & Fallback Flow

```
Audit Event Emission
    │
    ├─→ TransparencyAuditService.EmitEventAsync()
    │       │
    │       ├─→ Try: HttpClient.PostAsJsonAsync()
    │       │       │
    │       │       ├─ Success (200-299): Log success ✅
    │       │       │
    │       │       └─ Non-2xx response: LogEventLocally() 📝
    │       │
    │       ├─→ Catch HttpRequestException: LogEventLocally() 📝
    │       │   └─ (Service unreachable)
    │       │
    │       ├─→ Catch TaskCanceledException: LogEventLocally() 📝
    │       │   └─ (Timeout)
    │       │
    │       └─→ Catch Exception: LogEventLocally() 📝
    │           └─ (Unknown error)
    │
    └─→ Application continues (non-blocking) ✅
```

## Event Types & Severity Matrix

```
┌──────────────────────────┬──────────────┬────────────────────────────────┐
│ Event Type               │ Severity     │ Details Included               │
├──────────────────────────┼──────────────┼────────────────────────────────┤
│ HANDSHAKE_EMITTED        │ INFO         │ terminal_id, timestamp         │
│ SESSION_ACTIVATED        │ INFO         │ terminal_id, session_id        │
│ SESSION_CLOSED_VOTE      │ INFO         │ terminal_id, session_id, vote_id
│ SESSION_CLOSED_TIMEOUT   │ INFO         │ terminal_id, session_id, reason
├──────────────────────────┼──────────────┼────────────────────────────────┤
│ QR_SCANNED (legitimate)  │ INFO         │ status, jury_id, timestamp     │
│ QR_SCANNED (invalid)     │ INFO         │ status, jury_id, timestamp     │
│ QR_SCANNED (duplicate)   │ CRITICAL 🚨  │ status, jury_id, timestamp     │
├──────────────────────────┼──────────────┼────────────────────────────────┤
│ CONCILIATION_ATTEMPT (✓) │ INFO         │ digital, physical, jury_count  │
│ CONCILIATION_ATTEMPT (✗) │ MEDIUM ⚠️   │ digital, physical, variance    │
└──────────────────────────┴──────────────┴────────────────────────────────┘
```

## Configuration Hierarchy

```
appsettings.json (Base)
    │
    ├─ TransparencyService:BaseUrl
    │  └─ default: "http://localhost:8080"
    │
    ├─ TransparencyService:Timeout
    │  └─ default: 5000 ms
    │
    └─ Logging:LogLevel
       └─ Election.*: Information

            ↓

appsettings.Development.json (Override)
    │
    ├─ Inherits from base
    ├─ Can override TransparencyService settings
    └─ Logging verbosity for debugging
```

---

**Diagram Generated:** 2026-05-12
**Status:** Ready for implementation
