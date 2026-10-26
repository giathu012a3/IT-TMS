# 🧪 High-Signal Testing, TDD & Anti-Goodhart Verification (`#Argon-Omega`)

Testing methodology designed to maximize Bug Detection Power while minimizing Refactoring Fragility.

## 1. Kill the "Over-Mocking" Anti-Pattern
- Zero mocks for domain & value objects: test aggregates, entities, validators with real instances.
- Only mock uncontrolled external boundaries.
- Use real databases or high-fidelity test containers for data slices.

## 2. Deterministic Time, Randomness & Concurrency
- Never call DateTime.UtcNow directly, inject TimeProvider.
- Virtual time for async/concurrency.

## 3. Parameterized Boundary & Property-Based Falsification
- Attack boundary invariants with parameterized theories.
- Mandatory edge-case matrix: numeric, collections/strings, concurrency/idempotency.

## 4. Verification & Boundary Conditions
- Mutation testing resistance, Goodhart counter-metric.
