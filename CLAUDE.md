# CLAUDE.md — Menu Studio

## Start of every session
1. Read `progress.md` (current phase, open items, next steps).
2. For architecture context, read `docs/README.md` and `docs/14-rev-0.3-baseline-and-kickoff.md`, then the relevant numbered doc.
3. At the end of a meaningful change set, update `progress.md` (session history, decisions, next steps).

## Project
Multi-tenant SaaS: food businesses create digital menus; guests open them via QR codes. Launch market: **United States**.
Stack: Angular · ASP.NET Core on **.NET 10 LTS** · DynamoDB (Catalog, Media, Menu Authoring, Publishing) · PostgreSQL (tenancy, identity, audit, deletion tracking) · S3 + CloudFront · Terraform on AWS. Claude API comes in phase 2 (AI menu import).
Single-developer learning project, **~20 h/week, local-first** (Docker: PostgreSQL, DynamoDB Local, LocalStack S3, Keycloak, Mailpit).

## Current gate
The **Rev 0.3 architecture baseline is approved and published** (2026-11-21). Start application coding only when the user gives the go-ahead for week 1. Follow the kickoff checklist in doc 14 §6 and the plan in `docs/10-implementation-roadmap.md` §12.

## Rules for working on this repo
- **Don't invent business requirements.** Tag them: [S] stated · [D] derived · [A] assumption · [R] recommendation.
- **Don't silently change accepted ADRs** (doc 11). Propose changes and wait for approval; record every change set in the `docs/README.md` revision history with the reason.
- Challenge questionable decisions; give a recommendation, not just options.
- Out of MVP-1: ordering, online payments, SaaS billing, retail inventory, AI/Claude integration (phase 2).
- **Never cut protected tests** (consistency, tenant isolation, recovery; doc 10 §12.2) to fit the schedule. Cut flex scope or slip the timeline instead.

## Architecture invariants
- Modular monolith: modules reference each other only through `*.Contracts`; each module owns its store (PostgreSQL schema or DynamoDB table). Enforced by architecture tests.
- Tenant context comes from the verified membership (`ITenantContext`), never from payloads. PostgreSQL RLS with `SET LOCAL app.tenant_id`; the app role has no BYPASSRLS.
- **All DynamoDB writes go through `DynamoItemWriter`:** it always sets `TENANT_PK`/`TENANT_SK`; no accidental upserts (`attribute_exists` unless the upsert is explicitly intended); key segments `[A-Za-z0-9_-]` only.
- **No SQS and no DynamoDB Streams in MVP-1:** background work = work items claimed with leases (sparse `WorkIndex`, PostgreSQL `SKIP LOCKED`); events = transactional outbox.
- Guest path fully static on a separate public hostname; admin and API on the app hostname with a BFF cookie session.
- `CURRENT` in the Publishing table is the single source of truth for what's live; per-menu `ACTIVEJOB` lock; guarded commit transaction; public files are rebuildable projections.
- Infrastructure only through Terraform; no static AWS keys.

## Key docs
`docs/14-…` (baseline, kickoff) · `docs/11-…` (ADR register) · `docs/10-implementation-roadmap.md` §12 (approved plan, protected tests) · `docs/04-…` §4.1 (publish flow) · `docs/database/07-…` (DynamoDB keys, TenantIndex, deletion).
