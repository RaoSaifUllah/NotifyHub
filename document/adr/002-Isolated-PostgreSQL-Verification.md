# ADR-002: PostgreSQL verification isolation and connection handling
Date: 2026-10-01. Status: accepted, PostgreSQL fixture execution still unverified.
The owner supplied a hosted PostgreSQL 18.6 development database rather than
local Docker. Real PostgreSQL remains required; no SQLite substitution.
Each integration run owns a UUID-named schema with schema-specific EF migration
history. It drops only that schema in finally, never an existing database/schema.
Transaction-pooled connections are unsuitable for schema/session operations;
the direct endpoint is used for the hosted service's integration fixture.
Credentials live in ignored local configuration. PostgreSQL URL parsing uses
Npgsql with verified TLS. Parser exceptions are intentionally replaced without
inner exception because malformed connection input can contain credentials.
An initial preflight exception exposed the owner password; rotation is required.
This implementation/testing decision does not change the production queue design.
