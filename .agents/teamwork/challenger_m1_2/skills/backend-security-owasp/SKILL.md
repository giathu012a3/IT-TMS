---
name: backend-security-owasp
description: >-
  Use this skill when implementing authentication, authorization, input handling, rate limiting, webhooks, or external HTTP calls in backend services. Enforces OWASP API Security Top 10 defenses including Structural BOLA via Global Query Filters, Refresh Token Reuse Detection, SSRF Socket Guards, and Partitioned Rate Limiting.
---

# 🛡️ Zero-Trust Backend Security & OWASP API Top 10 (`#Argon-Omega`)

Adversarial defense-in-depth patterns. Never rely on developer memory for security; bake security invariants directly into the type system, ORM query pipeline, and middleware bulkheads.

## 1. Structural Defense Against BOLA / IDOR
Enforce authorization checks and invariant validations at domain entity boundaries.

## 2. Password Hashing & Cryptographic Auth
Ensure constant-time comparison, salt uniqueness per password, resistance to timing attacks, and handling of edge-case inputs (empty, unicode, extremely long).

## 3. Boundary & Invariant Validation
Prevent invalid types, negative priority, overflowing attachment payloads, malformed email inputs from compromising system state.
