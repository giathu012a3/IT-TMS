# EF Core & Database Physical Performance Methodology
Source: c:\IT_System\.agents\skills\backend\efcore-database-performance\SKILL.md

Core Methodology:
- Optimistic Concurrency with ConcurrencyToken / DbUpdateConcurrencyException handling
- Index-aligned queries, covering indexes
- Connection pool timeout and cancellation token propagation
- Real database verification (avoiding in-memory fakes that hide concurrency issues)
