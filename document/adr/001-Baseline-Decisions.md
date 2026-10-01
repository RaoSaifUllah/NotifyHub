# ADR-001: Baseline decisions
Date: 2026-10-01. Status: accepted implementation baseline.
Retain the five-layer modular monolith, separate API/worker, EF aggregate writes
with one Unit of Work, Dapper scoped projections, and PostgreSQL durable queue.
Retain memory-only JWT plus rotating cookie refresh, responsive web without native
mobile, and Web Push within v1. These consolidate ADR-001 through ADR-006 named in
the design without changing their architecture.
Pin SDK 10.0.401 and Microsoft packages 10.0.12. Explicit EF Core/Relational pins
avoid transitive 10.0.0 assemblies conflicting with Design 10.0.12.
Stable Npgsql EF 10.0.0 supports EF major 10; runtime compatibility must be proven
against PostgreSQL before its milestone is verified.
Docker is absent on the development machine; PostgreSQL is supplied by the owner.
No SQLite replacement. No Git initialization, publication or license selection.
References: https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md
