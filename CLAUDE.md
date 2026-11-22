# CLAUDE.md — MenuX

## Start of every session
1. Read `progress.md` (current phase, open items, next steps).
2. For architecture context, read `docs/README.md` and `docs/14-rev-0.3-baseline-and-kickoff.md`, then the relevant numbered doc.
3. At the end of a meaningful change set, update `progress.md` (session history, decisions, next steps).

## Project
Multi-tenant SaaS: food businesses create digital menus; guests open them via QR codes. Markets (draft): **US pilot/validation, then UAE commercial launch**; market packs, nothing market-specific hard-coded; Arabic/right-to-left from day one; region `me-central-1` proposed (week-1 feasibility check only; region decision before week 9).
Stack: Angular · ASP.NET Core on **.NET 10 LTS** · DynamoDB (Catalog, Media, Menu Authoring, Publishing) · PostgreSQL (tenancy, identity, audit, deletion tracking) · S3 + CloudFront · Terraform on AWS. Claude API comes in phase 2 (AI menu import).
Single-developer learning project, **~20 h/week, local-first** (Docker: PostgreSQL, DynamoDB Local, LocalStack S3, Keycloak, Mailpit).

## Current gate
**Week-1 foundation work is approved** (W1–W3, 2026-11-21; doc 13 §1b): week-1 scope only, plain Docker Compose, **no running AWS application resources and no real tenant data**. **Week-2 gate decided (2026-11-21, doc 13 §1d):** C5 (weeks 2–12 approved as written; if behind, the end date slips, nothing is cut or deferred), ADR-06 accepted, ADR-18 accepted (SQS), U4 and U7 approved. C1 remains open for the other P-items, C4 and ADR-17 stay open/Proposed; each later week waits for its decisions in doc 13 §1c. Don't change ADR statuses or mark anything approved without the user's explicit confirmation; restate the exact decisions and get an unambiguous yes first.

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
- **Background jobs on Amazon SQS (ADR-18, accepted 2026-11-21):** state change + outbox record in one transaction; the outbox relay sends to SQS standard queues (each with a DLQ); handlers are idempotent (conditional state transitions, inbox) and never assume ordering; leases only guard against duplicates. No database polling as the job queue. No DynamoDB Streams in MVP-1. SNS only when fan-out is needed.
- Guest path fully static on a separate public hostname; admin and API on the app hostname with a BFF cookie session.
- `CURRENT` in the Publishing table is the single source of truth for what's live; per-menu `ACTIVEJOB` lock; guarded commit transaction; public files are rebuildable projections.
- Infrastructure only through Terraform; no static AWS keys.
- **AWS test deployments (2026-11-21):** `us-east-1`, temporary (deploy, test, tear down; data discardable; not production-ready). Persistent: only the Terraform state bucket and the budget alert (`infrastructure/terraform/bootstrap`). Destroy only the `test` configuration; every test resource tagged `project=menux`, `environment=test`. Not the ADR-17 region decision. Never write personal contact details (e.g. the budget email) into the repository; use git-ignored `*.tfvars`.

## Key docs
`docs/14-…` (baseline, kickoff) · `docs/11-…` (ADR register) · `docs/10-implementation-roadmap.md` §12 (weeks 2–12 approved as written, C5 2026-11-21; if behind, the end date slips; protected tests) · `docs/04-…` §4.1 (publish flow) · `docs/database/07-…` (DynamoDB keys, TenantIndex, deletion).
