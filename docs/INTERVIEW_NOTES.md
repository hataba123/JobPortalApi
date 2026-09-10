# Interview Notes

These notes are a concise portfolio aid, not a script. They are grounded in the current JobPortal source and intentionally separate implemented behavior from future improvements.

## 1. Why a Modular Layered Monolith?

### Problem

Recruitment workflows cross applications, interviews, payments, credits, notifications, audit records, and reporting. Splitting these boundaries too early would add network and deployment complexity before the product needs it.

### Decision

The backend uses one ASP.NET Core 8 deployable process with separated service areas for authentication, recruitment, interviews, payments, matching, media, reporting, audit, and outbox processing.

### Why this decision

- Related writes can share one SQL Server transaction.
- Deployment and local development remain straightforward.
- Service boundaries remain visible without pretending they are independent services.
- The architecture can evolve if scale or team boundaries later justify extraction.

### Alternative and trade-off

Microservices, a broker, or Kubernetes could isolate scaling and deployment, but they would introduce distributed consistency, operational overhead, and more failure modes. The current worker also runs inside the API process, so multiple replicas would need an explicit ownership strategy.

### Failure handling

Business mutations validate state and ownership, use EF Core transactions or explicit serializable transactions where required, and enqueue durable outbox messages. Concurrency conflicts are returned as API conflicts instead of silently overwriting another user's change.

Source: [`Program.cs`](../JobPortalApi/Program.cs), [`ApplyService.cs`](../JobPortalApi/Services/User/ApplyService.cs), [`BackgroundProcessingService.cs`](../JobPortalApi/Services/Infrastructure/BackgroundProcessingService.cs).

## 2. Why Is ASP.NET Core the Canonical Backend?

### Problem

The workspace contains both an older NestJS/PostgreSQL implementation and the current ASP.NET Core/SQL Server implementation. Portfolio documentation must not describe the wrong runtime.

### Decision

The current path is Browser → Next.js → Next.js BFF → ASP.NET Core 8 → EF Core → SQL Server. The backend is a layered modular monolith with controllers, service areas, EF Core persistence, and a hosted worker.

### Why this decision

The source, migrations, runtime composition, Docker topology, API tests, and latest local verification all describe the ASP.NET Core path. The BFF keeps backend URL and bearer-token handling on the server side.

### Alternative and trade-off

The NestJS/PostgreSQL code can help explain historical context, but presenting it as active would create a false architecture claim. Keeping it as reference avoids unnecessary migration work while making the canonical boundary clear.

### Failure handling

The BFF forwards selected headers such as `If-Match` and correlation IDs and propagates response metadata such as `ETag`. If the backend is unavailable, the request fails through the normal proxy path; there is no second hidden backend fallback.

Source: [`Program.cs`](../JobPortalApi/Program.cs), [`docker-compose.yml`](../docker-compose.yml), [frontend BFF route](https://github.com/hataba123/jobportal-fe/blob/master/src/app/api/backend/%5B...path%5D/route.ts).

## 3. How Does the Recruitment State Machine Prevent Invalid Changes?

### Problem

An application status cannot be treated as an arbitrary string. For example, a rejected application should not silently move to an interview, and candidates should not withdraw a hired application.

### Decision

The service validates the current status, actor role, ownership, and requested transition. The normal states are `Applied`, `Screening`, `Interview`, `Offer`, `Hired`, `Rejected`, and `Withdrawn`.

### Why this decision

- Invalid transitions are rejected at the backend boundary.
- Candidates have a narrower withdrawal rule than recruiters.
- Administrative overrides remain possible but require a reason and audit trail.
- Every accepted change records status history and an outbox event.

### Alternative and trade-off

A generic CRUD update would be shorter but would move business rules into clients and make audit behavior inconsistent. A formal workflow library could be useful at greater scale, but the current transition matrix is small enough to remain readable in the service.

### Failure handling

Concurrent updates catch `DbUpdateConcurrencyException` and return a conflict. A failed mutation does not intentionally leave a partial application/history/audit/outbox unit because the related database writes commit together.

Source: [`ApplyService.cs`](../JobPortalApi/Services/User/ApplyService.cs).

## 4. How Does RowVersion/ETag Concurrency Work?

### Problem

Two users can load the same company, interview, job, application, or report and then submit edits based on different versions.

### Decision

SQL Server `rowversion` is mapped as the concurrency token. The API encodes it as an opaque ETag and requires `If-Match` for protected updates.

### Why this decision

The client does not need to know database internals. It only returns the version it last read, allowing the database to reject a stale write safely.

### Alternative and trade-off

Last-write-wins is simpler but can discard another user's change. Pessimistic locking would hold database locks across user interaction and is a poor fit for normal HTTP editing.

### Failure handling

- Missing precondition: `428 Precondition Required`.
- Stale version: `409 Conflict`.
- Successful update: response includes the new ETag where the endpoint supports it.

The token is opaque and should not be parsed by the frontend.

Source: [`ConcurrencyToken.cs`](../JobPortalApi/Services/Infrastructure/ConcurrencyToken.cs), [`ApplicationDbContext.cs`](../JobPortalApi/Data/ApplicationDbContext.cs), [`Program.cs`](../JobPortalApi/Program.cs).

## 5. What Is the Transactional Outbox Solving?

### Problem

An application status update may need a notification or email. Sending the email first can produce a message for a database change that later rolls back; saving the database first and sending directly can lose the notification if the process stops.

### Decision

The business service saves an outbox message in the same database unit of work as the business mutation. A hosted worker later claims and handles eligible messages.

### Why this decision

The database records the durable intent before the API reports success. The worker can retry failures and deduplicate supported notification effects.

### Alternative and trade-off

A broker could provide independent scaling and richer delivery features, but it would add infrastructure that the current scope explicitly does not need. The in-process worker is simpler but requires operational coordination if the API is replicated.

### Failure handling

Messages are claimed atomically in batches of up to 50. Failures record the last error, use capped exponential backoff, and are dead-lettered after ten attempts. Processing is at-least-once, not exactly-once; a crash after sending an email and before saving `ProcessedAt` can cause a duplicate send.

Source: [`OutboxService.cs`](../JobPortalApi/Services/Infrastructure/OutboxService.cs), [`BackgroundProcessingService.cs`](../JobPortalApi/Services/Infrastructure/BackgroundProcessingService.cs).

## 6. How Is VNPay Payment Confirmation Bound and Made Idempotent?

### Problem

The browser return URL can be replayed or forged, and payment providers can retry notifications. A frontend redirect must not be trusted to grant credits.

### Decision

The backend creates a local pending order, builds the provider URL, and treats the signed server-to-server IPN as the source of truth. IPN handling is idempotent for a repeated provider transaction.

### Validation and transaction

- Verify the HMAC-SHA512 signature using a fixed-time comparison.
- Validate merchant, VND currency, positive amount, local transaction reference, response/status fields, and a numeric transaction number.
- Compare the provider amount with the stored order snapshot and reject expired orders.
- In a serializable transaction, protect duplicate transaction numbers, mark the order paid, and create the entitlement grants.

### Failure handling

A paid order receiving the same transaction is treated as an idempotent repeat. A paid order receiving a different transaction is rejected. Unique-constraint races are converted to a provider failure response. The browser return endpoint is informational and cannot grant credit.

Source: [`PaymentService.cs`](../JobPortalApi/Services/Payments/PaymentService.cs), [`PaymentController.cs`](../JobPortalApi/Controllers/Payments/PaymentController.cs).

## 7. Why Snapshot Payment Entitlements?

### Problem

If a plan's name, price, credit quantity, or expiry policy changes after checkout, a later IPN should still settle the order that the customer actually purchased.

### Decision

The pending payment order stores a snapshot of plan name, price, currency-relevant amount, credit type, quantity, and expiry configuration. IPN processing grants from that snapshot rather than re-reading mutable plan terms.

### Why this decision

- The order remains auditable.
- A plan edit cannot retroactively change the purchased entitlement.
- Signature and amount validation can compare against a stable local value.

### Alternative and trade-off

Re-reading the current plan is less data but creates a time-of-check/time-of-use risk. Snapshots add columns/payload but make settlement deterministic.

### Failure handling

The IPN rejects mismatched amount or invalid snapshot data before marking the order paid. Payment and grants commit together or neither becomes effective.

Source: [`PaymentService.cs`](../JobPortalApi/Services/Payments/PaymentService.cs).

## 8. Why Use an Append-Only Credit Ledger?

### Problem

A single mutable balance cannot adequately explain grants, consumption, refunds, expiry, or administrative adjustments.

### Decision

Credits are represented by append-only entries: `Grant`, `Debit`, `Refund`, and `Adjustment`. Available balance is derived from valid entries and expiry rules.

### Why this decision

- Every balance change has a source and an audit trail.
- Payment grants can be tied to a payment order and credit type.
- Refunds can point back to a source debit.
- Administrative adjustments require actor, reason, and idempotency information.

### Failure handling

Debit rejects insufficient balance. Refund rejects an absent or over-refunded source and preserves expiry/source data. Adjustment rejects zero quantity, missing actor/reason, negative resulting balance, or a conflicting idempotency payload. Repeated identical idempotent requests do not append a second effect.

### Trade-off

Ledger queries are more involved than reading one balance column, but the auditability and recovery properties are more valuable for payment-related data.

Source: [`CreditLedgerService.cs`](../JobPortalApi/Services/Payments/CreditLedgerService.cs), [`ApplicationDbContext.cs`](../JobPortalApi/Data/ApplicationDbContext.cs).

## 9. How Does Interview Scheduling Handle Concurrency?

### Problem

Two requests can attempt to schedule the same interviewer in overlapping time windows. A simple pre-check without isolation can allow both requests to pass.

### Decision

Create and reschedule operations run in a serializable transaction. The service validates application state and ownership, queries overlapping scheduled interviews for the interviewer, and persists the interview plus related application/history/audit/outbox changes.

### Why this decision

The important invariant is “one interviewer cannot have overlapping scheduled interviews.” Stronger isolation reduces the race window around the overlap check.

### Failure handling

Invalid interview details are rejected: online requires an HTTPS meeting URL, onsite requires a location, and phone requires a location or meeting URL. Only scheduled interviews can be updated, completed, or cancelled. Concurrency conflicts are surfaced as API conflicts.

### Trade-off

Serializable isolation can increase contention. It is used for this invariant because accepting an overlap is more harmful than the cost of a short, focused transaction.

Source: [`InterviewService.cs`](../JobPortalApi/Services/User/InterviewService.cs).

## 10. How Are Private CVs Protected?

### Problem

CVs contain personal data and should not be directly enumerable or publicly served as ordinary static media.

### Decision

CV files are stored under `current/private-data/cv`, outside `wwwroot`, with a GUID-based `.pdf` filename. The API authorizes every read based on candidate ownership, administrator role, or recruiter/application relationship.

### Validation

- Extension must be `.pdf`.
- Size must be greater than zero and no more than 5 MB.
- The first five bytes must be `%PDF-`.
- Path resolution accepts only the expected basename and a path under the configured root.
- Uploads use a temporary `.uploading` file and cleanup compensation on failure.

### Alternative and trade-off

Public static hosting is easy but inappropriate for sensitive documents. Object storage with signed URLs could scale better, but private local storage makes authorization explicit for the current deployment shape.

### Failure handling

Direct `/uploads/cv` access is blocked. If the database/file coordination fails, temporary and final files are removed where possible, and old-file cleanup occurs only after a successful replacement commit. The implementation does not claim antivirus scanning.

Source: [`PrivateCvStorage.cs`](../JobPortalApi/Services/Helpers/PrivateCvStorage.cs), [`RecruiterCandidateProfileController.cs`](../JobPortalApi/Controllers/User/RecruiterCandidateProfileController.cs), [`Program.cs`](../JobPortalApi/Program.cs).

## Quick Facts for a Portfolio Discussion

- Canonical stack: Next.js BFF → ASP.NET Core 8 → EF Core → SQL Server.
- Backend shape: layered modular monolith, one deployable process.
- Matching: deterministic algorithm `v1`; weights are 60/20/10/10 for skills/experience/education/preferences.
- Local matching observations: 7 jobs returned in 196/46/17/16/12/14 ms samples; 1,007 jobs in 559/271/170/122/117/103 ms samples.
- Local report observation: 1,000 synthetic records returned in 1,515/19/17/17/28/17 ms samples.
- Backend build: pass locally with 0 errors and 176 warnings.
- Backend tests: 10/10 pass locally.
- Frontend build: pass with 87 pages; contract check: 39 paths and 38 schemas.
- Docker runtime and OAuth provider runtime were environment-dependent and not fully verified.
- Latest backend GitHub Actions run failed during build; no green-CI claim is made.

## Honest Boundary Statements

Use these statements when discussing scope:

- “This is a modular monolith with explicit service areas, not microservices.”
- “The outbox gives durable, at-least-once processing with deduplication where implemented; it is not exactly-once delivery.”
- “The payment IPN is the server-side source of truth; the browser return page does not grant credits.”
- “The matching engine is deterministic and explainable; it is not AI/ML.”
- “CV validation checks type, size, signature, path safety, and authorization; it does not claim antivirus scanning.”
- “Local checks passed, while Docker runtime, provider credentials, and the latest external CI failure still require environment-specific follow-up.”
