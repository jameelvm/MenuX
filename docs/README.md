# MenuX — Architecture Documentation

Status: **Revision 0.3 — DRAFT, pending approval.** Week-1 foundation work is approved (W1–W3, 2026-11-21). Week-2 gate decided (2026-11-21): C5 roadmap weeks 2–12 approved, ADR-06 and ADR-18 (SQS) accepted, U4 and U7 approved. C1 stays open for the other P-items. Accepted decisions: Rev 0.2 (ADR-01/02/03/07/12/13/14/16) plus ADR-06 and ADR-18. Decisions awaiting your confirmation: [doc 13](13-decision-confirmation.md). Draft summary: [doc 14](14-rev-0.3-baseline-and-kickoff.md). ADR register: [doc 11](11-architecture-review-and-open-questions.md).

## How to read these documents

Every requirement is tagged with where it came from:

| Tag | Meaning |
|-----|---------|
| **[S]** | **Stated** in the product brief or a product-owner decision (e.g., "Rev 0.2 #7", "U6"). |
| **[D]** | **Derived**: a direct technical consequence of a stated requirement. |
| **[A]** | **Assumption**: plausible but not stated; needs confirmation before it is built. |
| **[R]** | **Architect recommendation**: a technical choice, open to challenge. |

## Document index

| # | Document | Folder | Rev 0.3 |
|---|----------|--------|:-:|
| 01 | [Product Requirements Document](requirements/01-product-requirements.md) | requirements | updated |
| 02 | [Functional and Non-Functional Requirements](requirements/02-functional-and-nonfunctional-requirements.md) | requirements | updated |
| 03 | [User Roles and User Journeys](requirements/03-user-roles-and-journeys.md) | requirements | updated |
| 04 | [High-Level Architecture](architecture/04-high-level-architecture.md) | architecture | updated |
| 05 | [Domain Boundaries and Modular Monolith](architecture/05-domain-boundaries-modular-monolith.md) | architecture | updated |
| 06 | [Hybrid Database Architecture](database/06-hybrid-database-architecture.md) | database | updated |
| 07 | [DynamoDB Access Patterns](database/07-dynamodb-access-patterns.md) | database | updated |
| 08 | [PostgreSQL Entity Relationship Design](database/08-postgresql-erd.md) | database | updated |
| 09 | [Security and Multi-Tenancy Strategy](architecture/09-security-and-multitenancy.md) | architecture | updated |
| 10 | [12-Week Implementation Roadmap](10-implementation-roadmap.md) (plan: §12; weeks 2–12 approved 2026-11-21) | — | updated |
| 11 | [ADR Register, Missing Requirements, Open Decisions](11-architecture-review-and-open-questions.md) | — | updated |
| 12 | [Architecture Sign-off Review](12-architecture-signoff.md) (record) | — | not yet approved |
| 13 | [Decision Confirmation](13-decision-confirmation.md) | — | **pending your confirmation** |
| 14 | [Rev 0.3 Baseline and Implementation Kickoff](14-rev-0.3-baseline-and-kickoff.md) | — | draft |

`docs/api/` will hold OpenAPI contracts generated from week 2.

## Key positions (Rev 0.3 draft — proposals pending approval; ADR-01/02/03/07/12/13/14/16 are accepted)

1. **Hybrid database** (ADR-03): DynamoDB for Catalog, Media, Menu Authoring, Publishing; PostgreSQL for tenancy, identity, audit and deletion tracking.
2. **Fully static guest path** on a separate public hostname (ADR-04/05): prerendered Angular shell + JSON projections on CloudFront/S3.
3. **One authoritative publication pointer** per menu with a per-menu job lock, a guarded atomic commit and lease-based manifest rebuilds (ADR-10).
4. **Complete tenant deletion** via TenantIndex, a single tagging writer, a tenant guard, verification passes and a supervisor (ADR-09).
5. **Background jobs on Amazon SQS via a transactional outbox relay; no DynamoDB Streams in MVP-1; SNS later** (ADR-18, accepted 2026-11-21).
6. **BFF cookie session** with Keycloak locally and Cognito in AWS (ADR-06).
7. **Markets (draft):** US pilot/validation → UAE commercial launch; market packs with nothing market-specific hard-coded (ADR-19); region `me-central-1` proposed (ADR-17; week-1 feasibility check only, region decision before the pilot environment).
8. **MVP-1 excludes** ordering, online payments, SaaS billing, retail inventory and AI (AI menu import is the first phase-2 capability).
9. **12 weeks at ~20 h/week:** local MVP-1 + first AWS environment; protected tests never cut (doc 10 §12).

## Revision history

| Rev | Date | Summary |
|-----|------|---------|
| 0.1 | 2026-11-19 | Initial draft: PRD, requirements, journeys, HLD, modules, hybrid DB, DynamoDB access patterns, PostgreSQL ERD, security, roadmap, review and open questions. |
| 0.2 | 2026-11-19 | Architecture Review 0.2 applied. Hybrid DB accepted. Tenant enumeration/deletion redesigned (TenantIndex GSI). Single authoritative publication pointer with a publish state machine and failure handling. Public app = static prerendered shell + client-side data. QR IDs 128-bit + rate limiting. .NET 10 LTS. Ordering and online payments out of MVP-1. Multiple locations. Food first. Menu import first proposed AI capability. Single-developer, local-first roadmap. |
| 0.3 (draft) | 2026-11-19 | Sign-off and implementation-readiness review (doc 12). Seven proposed ADRs: "Approve with changes". TenantIndex findings TI-1…13, publishing findings PUB-1…14, conflicts X-1…10, risks R-1…15, defaults U3–U13, proposed roadmap. No statuses changed. |
| 0.3 (draft) | 2026-11-21 | Decision confirmation prepared (doc 13). Provisional agreement recorded; U12 answered (~20 h/week, learning priorities, AI to phase 2, protected tests); roadmap revised (doc 10 §12). |
| 0.3 (draft) | 2026-11-21 | **Draft changes applied.** First recorded as an "approved baseline" (P1–P21, US launch market, roadmap doc 10 §12); that approval was withdrawn — see the correction row below. The draft set ADR-04/05/06/08/09/10/11/15 to Accepted (since reverted); ADR-17 (US market; `us-east-1` proposed) and ADR-18 (work items + outbox, no SQS/Streams) added. Doc 12 §4/§3 changes applied to docs 04, 05, 06, 07, 08, 09; product defaults to 01, 02, 03; roadmap §12 authoritative; "⚠ Sign-off review" notes removed. Refinements F1–F7 listed in doc 14 §3. U13's 30-day grace period kept provisional pending legal review. |
| 0.3 (draft) | 2026-11-21 | **Correction.** The product owner stated that the US launch market, P1–P21 and doc 10 §12 were **not explicitly approved**. These are treated as unconfirmed: ADR-04/05/06/08/09/10/11/15 reverted to Proposed; ADR-17 (market + region) and ADR-18 marked Proposed; doc 14 and all Rev 0.3 markers in docs 01–11 marked **draft, pending approval**; doc 12/13 status corrected; C1, C4, C5 reopened. Technical content kept as proposals. U13 30-day grace remains provisional pending legal review. No week-1 work or code started. The GitHub commit `c57b5ab` still shows the earlier "approved" wording until the next commit. |
| 0.3 (draft) | 2026-11-21 | **Market split (draft, Option B chosen by the product owner).** US = pilot/validation market, UAE = target commercial launch market (C4 draft). ADR-17 rewritten (`me-central-1` from week 9, conditional on the week-1 service check; UAE storage disclosed to US pilot users) and ADR-19 added (market packs); both **Proposed**. P2/P14/P15 and F6/F7 revised: Arabic + English with right-to-left from day one, date-range schedule overrides, Unicode/Arabic name normalization, canonical allergen codes with per-market schemes, optional halal/pork/alcohol fields, price-display policy. Legal research split into US pilot and UAE launch tracks. Roadmap §12 gains ~3–5 days absorbed by flex scope; protected tests not squeezed. C1 and C5 remain unconfirmed. Local drafts only; not committed. |
| 0.3 (draft) | 2026-11-21 | **Pilot gates and criteria (draft).** T1 tenant-scoped access policies made a required gate before any real pilot tenant; external pen test a required gate before the UAE commercial launch. Learning-environment risk assessment and minimum safeguards G1–G12 added (doc 04 §5.3, **not accepted**). Post-week-12 pilot schedule made explicit (doc 10 §12.5). Pilot success criteria reviewed: 6 proposed core measures, diagnostics, gates and a UAE validation track (doc 01 §11, proposed). C1, C5 and all statuses unchanged; not committed. |
| 0.3 (draft) | 2026-11-21 | **Week-1 start decisions (confirmed by the product owner).** W1: week-1 foundation work may start while C1 and C5 stay open; later weeks gated by specific decisions (doc 13 §1c). W2: week-1 scope approved, week 1 only, with no running AWS application resources and no real tenant data. W3: plain Docker Compose (doc 11 T3). Updated: CLAUDE.md gate, doc 10 §12 note, doc 11 Part C, doc 13 §1b–§1c, doc 14 header and §6 checklist (Keycloak moved to week 2, `DynamoItemWriter` to week 3). No ADR status changed. Not committed. |
| 0.3 (draft) | 2026-11-21 | **Project renamed to MenuX** (product owner request), in code and docs: solution `MenuX.slnx`, projects and namespaces `MenuX.*`, Angular workspace `menux`, compose project and local database `menux`; product name in docs 01, 03, 04, 05, README, CLAUDE.md and progress.md. The README revision history, the progress.md session log and doc 12's review record keep the old name as historical text. Week-1 foundation files (local, branch `week1/foundation`) recorded in doc 14 §6 and `docs/architecture/versions.md`; `me-central-1` evidence recorded there for the ADR-17 decision. No decision, status, scope or roadmap change. Not committed. |
| 0.3 | 2026-11-21 | **Week-2 gate decisions (product owner, confirmed).** C5: roadmap weeks 2–12 approved as written; planned work isn't moved, deferred or removed because of time, so if behind the end date slips and §12.3 becomes a priority order (doc 10). ADR-06 **Accepted** (Fargate API/Worker, Cognito in AWS, Keycloak locally, Admin BFF cookie session, `idp_issuer` + `idp_subject`; P3, P11). ADR-18 **rewritten and Accepted**: Amazon SQS standard queues from MVP-1, transactional outbox relay for reliable enqueue, per-queue DLQs, idempotent handlers, leases only as duplicate guards, reconciler as safety net, SNS later for fan-out; supersedes P8 and F1; F2/F3 kept; local SQS on the same LocalStack 4.14.0 image. U4/P13 and U7/P16 approved (ADR-13/ADR-12 notes; doc 08 `plan_key` ↔ future `billing.plans.key`). Further confirmed: DLQ after 5 failed receives with 14-day retention and a DLQ alerting/inspection/redrive procedure (doc 06 §5.1); SQS built in week 2 (minimal relay + consumer), media in week 3, publishing in week 5; temporary AWS test deployments in `us-east-1` with teardown (doc 10 §12.6; not the ADR-17 decision); USD 25/month budget alert; state bucket in `us-east-1` (encrypted, versioned, public access blocked); SkiaSharp and QRCoder. Updated: docs 04, 05, 06, 07, 08, 09, 10, 11, 13, 14, versions.md, library-licences.md, CLAUDE.md. Still open: C1 (other P-items), C4, ADR-17 (Proposed), other Proposed ADRs. |
| 0.3 | 2026-11-22 | **AWS deployment deferred to the final AWS phase** (product owner). No AWS resources, `terraform apply` or AWS calls before weeks 9–12; no SSO profile or account ID needed until then; Terraform prepared and validated locally. Budget alert and state bucket moved from week 1 to week 9; AWS-only checks (RDS PostgreSQL 18 in `us-east-1`, Terraform plan, LocalStack vs AWS, `me-central-1` account-level) marked pending (doc 10 §12.7). ADR-17 deadline changed to "before the pilot environment" (status still Proposed). Updated: docs 01, 02, 04, 09, 10, 11, 13, 14, README, versions.md, CLAUDE.md, progress.md. |
| 0.3 | 2026-11-23 | **Signup, verification and billing readiness (product owner).** Self-service signup (doc 03 J1 resolved); email verification enforced by the identity provider **and** the API; first-login profile read once from the ID token, then the MenuX database is the source of truth (session `iss` + `sub`, ADR-06 unchanged); restaurant use will require a subscription or eligible trial, with an extensible seam only — trial, pricing and access rules pending the billing phase (U7 unchanged). Implemented in week 2 slice 3. Updated: docs 03, 08, 09, 11, 13. |

### What changed in Rev 0.3, by document

| Doc | Changes | Why |
|-----|---------|-----|
| 01 PRD | US market; AI to phase 2; billing deferred; outlets/hierarchy decisions; release plan | Proposed decisions C4, P12–P19 (unconfirmed) |
| 02 Requirements | Priorities for FR-CAT/FR-MENU/FR-QR per U3–U6; unpublish; AI/billing/analytics phase 2; US NFRs; protected tests | Proposed defaults; US market (unconfirmed) |
| 03 Journeys | Open questions resolved; publish lock, unpublish, full-state rollback; sold-out via publish; AI journey phase 2 | U4, U6, U11, PUB-1/6/7 |
| 04 HLD | Two hostnames; work items instead of SQS; publish flow rewritten (lock, guard, MANIFEST lease, UNPUBLISH, rollback, cache table); AI constraints; region; BFF | ADR-04/06/08/10, X-5, PUB-*, F1–F3 |
| 05 Modules | Media table; outbox dispatch in the worker; event names | X-1, PUB-11, F3 |
| 06 Hybrid DB | Media table; outbox without Streams; manifest TTL; retention; lifecycle | X-1, X-10, F1–F3 |
| 07 DynamoDB | Writer rules, key rules, outbox/WorkIndex; Publishing table redesign; Media table; deletion procedure rewritten | TI-1…13, PUB-1…14, F1–F6 |
| 08 PostgreSQL | `idp_issuer`/`idp_subject`; `plan_key`; deletion-module leases; retention | X-2, U7, TI-10, U13 |
| 09 Security | S3 tenant-prefixed layout and version purge; BFF decided; QR edge check; alarm-only miss detection; WAF region; AI constraints; US privacy | X-3, TI-11, ADR-06/08/11 |
| 10 Roadmap | §12 proposed (pending C5); SQS removed; AI phase 2 | C5, F1 |
| 11 Register | Statuses, ADR-17, ADR-18, decision status | C1 |
