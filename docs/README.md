# Menu Studio — Architecture Documentation

Status: **Revision 0.3 — approved architecture baseline (2026-11-21).** Baseline summary and kickoff checklist: [doc 14](14-rev-0.3-baseline-and-kickoff.md). ADR register: [doc 11](11-architecture-review-and-open-questions.md).

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
| 10 | [12-Week Implementation Roadmap](10-implementation-roadmap.md) (approved plan: §12) | — | updated |
| 11 | [ADR Register, Missing Requirements, Open Decisions](11-architecture-review-and-open-questions.md) | — | updated |
| 12 | [Architecture Sign-off Review](12-architecture-signoff.md) (record) | — | approved |
| 13 | [Decision Confirmation](13-decision-confirmation.md) (record) | — | approved |
| 14 | [Rev 0.3 Baseline and Implementation Kickoff](14-rev-0.3-baseline-and-kickoff.md) | — | new |

`docs/api/` will hold OpenAPI contracts generated from week 2.

## Key positions (Rev 0.3)

1. **Hybrid database** (ADR-03): DynamoDB for Catalog, Media, Menu Authoring, Publishing; PostgreSQL for tenancy, identity, audit and deletion tracking.
2. **Fully static guest path** on a separate public hostname (ADR-04/05): prerendered Angular shell + JSON projections on CloudFront/S3.
3. **One authoritative publication pointer** per menu with a per-menu job lock, a guarded atomic commit and lease-based manifest rebuilds (ADR-10).
4. **Complete tenant deletion** via TenantIndex, a single tagging writer, a tenant guard, verification passes and a supervisor (ADR-09).
5. **No SQS or DynamoDB Streams in MVP-1:** work items + transactional outbox, polled by the worker (ADR-18).
6. **BFF cookie session** with Keycloak locally and Cognito in AWS (ADR-06).
7. **US launch** (ADR-17); region `us-east-1` proposed.
8. **MVP-1 excludes** ordering, online payments, SaaS billing, retail inventory and AI (AI menu import is the first phase-2 capability).
9. **12 weeks at ~20 h/week:** local MVP-1 + first AWS environment; protected tests never cut (doc 10 §12).

## Revision history

| Rev | Date | Summary |
|-----|------|---------|
| 0.1 | 2026-11-19 | Initial draft: PRD, requirements, journeys, HLD, modules, hybrid DB, DynamoDB access patterns, PostgreSQL ERD, security, roadmap, review and open questions. |
| 0.2 | 2026-11-19 | Architecture Review 0.2 applied. Hybrid DB accepted. Tenant enumeration/deletion redesigned (TenantIndex GSI). Single authoritative publication pointer with a publish state machine and failure handling. Public app = static prerendered shell + client-side data. QR IDs 128-bit + rate limiting. .NET 10 LTS. Ordering and online payments out of MVP-1. Multiple locations. Food first. Menu import first proposed AI capability. Single-developer, local-first roadmap. |
| 0.3 (draft) | 2026-11-19 | Sign-off and implementation-readiness review (doc 12). Seven proposed ADRs: "Approve with changes". TenantIndex findings TI-1…13, publishing findings PUB-1…14, conflicts X-1…10, risks R-1…15, defaults U3–U13, proposed roadmap. No statuses changed. |
| 0.3 (draft) | 2026-11-21 | Decision confirmation prepared (doc 13). Provisional agreement recorded; U12 answered (~20 h/week, learning priorities, AI to phase 2, protected tests); roadmap revised (doc 10 §12). |
| **0.3** | **2026-11-21** | **Approved baseline.** Product owner approved P1–P21 (no exceptions), US launch market, roadmap doc 10 §12. ADR-04/05/06/08/09/10/11/15 → Accepted; ADR-17 (US market; `us-east-1` proposed) and ADR-18 (work items + outbox, no SQS/Streams) added. Doc 12 §4/§3 changes applied to docs 04, 05, 06, 07, 08, 09; product defaults to 01, 02, 03; roadmap §12 authoritative; "⚠ Sign-off review" notes removed. Refinements F1–F7 listed in doc 14 §3. U13's 30-day grace period kept provisional pending legal review. |

### What changed in Rev 0.3, by document

| Doc | Changes | Why |
|-----|---------|-----|
| 01 PRD | US market; AI to phase 2; billing deferred; outlets/hierarchy decisions; release plan | Approved decisions C4, P12–P19 |
| 02 Requirements | Priorities for FR-CAT/FR-MENU/FR-QR per U3–U6; unpublish; AI/billing/analytics phase 2; US NFRs; protected tests | Approved defaults; US market |
| 03 Journeys | Open questions resolved; publish lock, unpublish, full-state rollback; sold-out via publish; AI journey phase 2 | U4, U6, U11, PUB-1/6/7 |
| 04 HLD | Two hostnames; work items instead of SQS; publish flow rewritten (lock, guard, MANIFEST lease, UNPUBLISH, rollback, cache table); AI constraints; region; BFF | ADR-04/06/08/10, X-5, PUB-*, F1–F3 |
| 05 Modules | Media table; outbox dispatch in the worker; event names | X-1, PUB-11, F3 |
| 06 Hybrid DB | Media table; outbox without Streams; manifest TTL; retention; lifecycle | X-1, X-10, F1–F3 |
| 07 DynamoDB | Writer rules, key rules, outbox/WorkIndex; Publishing table redesign; Media table; deletion procedure rewritten | TI-1…13, PUB-1…14, F1–F6 |
| 08 PostgreSQL | `idp_issuer`/`idp_subject`; `plan_key`; deletion-module leases; retention | X-2, U7, TI-10, U13 |
| 09 Security | S3 tenant-prefixed layout and version purge; BFF decided; QR edge check; alarm-only miss detection; WAF region; AI constraints; US privacy | X-3, TI-11, ADR-06/08/11 |
| 10 Roadmap | §12 approved and authoritative; SQS removed; AI phase 2 | C5, F1 |
| 11 Register | Statuses, ADR-17, ADR-18, decision status | C1 |
