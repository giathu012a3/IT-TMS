# 🛡️ Zero-Trust Backend Security & OWASP API Top 10 (`#Argon-Omega`)

Adversarial defense-in-depth patterns (`#Gamma-Prime #053/#070`). Never rely on developer memory for security; bake security invariants directly into the type system, ORM query pipeline, and middleware bulkheads.

## 1. Structural Defense Against BOLA / IDOR (`API1:2023`)
- Fatal Flaw of Manual Checks: Relying on developers to write `if (entity.TenantId != currentUser.TenantId)` in every handler guarantees a breach when a single new endpoint forgets the check.
- Constructive Mechanism:
  1. Implement `ITenantScoped { Guid TenantId { get; } }` on all multi-tenant entities.
  2. Configure an automatic EF Core Global Query Filter in `OnModelCreating` bound to `ICurrentTenantProvider`.
  3. Stamp `TenantId` automatically inside `SaveChangesInterceptor` from verified cryptographic claims.
  4. Expose non-sequential public identifiers (`UUIDv7` or cryptographically random IDs).

## 2. Cryptographic Auth & Refresh Token Family Compromise Detection (`API2:2023`)
- JWT Hardening: Explicit ValidAlgorithms, ClockSkew <= 30s, Access Token TTL <= 10m.
- Refresh Token Rotation with Reuse Detection: SHA-256 hash stored, family revoke on reuse.

## 3. Partitioned Rate Limiting & Resource Exhaustion Bulkheads (`API4:2023`)
- Partitioned token bucket / sliding window by AuthenticatedUserId or ClientIP+NormalizedUsername.
- MaxRequestBodySize, JSON MaxDepth, concurrency limits on heavy endpoints.

## 4. SSRF Socket Guard & Webhook Signature Verification (`API7:2023` & `API10:2023`)
- ConnectCallback DNS resolution & IP check before socket open.
- CryptographicOperations.FixedTimeEquals, signed timestamp window.

## 5. Secret Hygiene, Redaction & Verification (`#051` / `#068`)
- Structured log redaction, Goodhart counter-metric: cross-tenant automated integration tests for 403/404.
