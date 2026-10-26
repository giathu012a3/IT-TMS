# 🧪 High-Signal Testing, TDD & Anti-Goodhart Verification (`#Argon-Omega`)

Testing methodology designed to maximize **Bug Detection Power** while minimizing **Refactoring Fragility** (`#068 Goodhart's Law` & `#051 Popperian Falsification`).

---

## 1. Kill the "Over-Mocking" Anti-Pattern (Classical vs. London School)

- **Fatal Flaw of Mock-Heavy Tests (`#068`)**: Mocking internal collaborators (`mockRepo.Setup(x => x.GetById(...)).Returns(...)`) tests *how the code is written today* (implementation details), not *what the system achieves* (observable behavior). Every internal refactoring breaks 50 tests while real SQL/serialization bugs slip into production.
- **Constructive Rules**:
  1. **Zero Mocks for Domain & Value Objects**: Test Aggregate Roots, Entities, Validators, and pure algorithms with real instances and state assertions (`Assert.Equal(expectedState, aggregate.State)`).
  2. **Only Mock Uncontrolled External Boundaries**: Only use mocks/fakes for third-party payment gateways, external SMTP/SMS providers, or chaos fault injection.
  3. **Use Real Databases via `Testcontainers` for Data/Slice Tests**: Never use `EF Core InMemory` provider (which ignores foreign keys, transactions, and SQL translation bugs). Use `Testcontainers` (PostgreSQL/SQL Server) + `Respawn` for fast, real-DB integration tests.

---

## 2. Deterministic Time, Randomness & Concurrency (`#010 Invariants`)

Flaky tests destroy trust in CI. Eliminate non-determinism at the root:
- **Never Call `DateTime.UtcNow` or `System.currentTimeMillis()` Directly**:
  - Inject `.NET 8+ TimeProvider` (and test with `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`) or a custom `IClock` / Kotlin `Clock`.
- **Virtual Time for Async/Coroutines**:
  - In Kotlin, always wrap coroutine tests in `runTest` using `StandardTestDispatcher` and `advanceTimeBy()` — never use real `Thread.sleep` or `delay` on wall-clock time.

---

## 3. Parameterized Boundary & Property-Based Falsification (`#051`)

One happy-path `[Fact]` proves almost nothing. Attack boundary invariants using parameterized theories.
