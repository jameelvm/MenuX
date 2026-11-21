# Menu Studio — Progress Log

Purpose: restore context quickly if a working session is lost. Read this first, then `docs/14-rev-0.3-baseline-and-kickoff.md` and `docs/README.md`.

Last updated: 2026-11-21

---

## 1. Project in one paragraph

Menu Studio is a multi-tenant SaaS platform where US food businesses (restaurants, cafes, hotel outlets, food shops) create digital menus that guests open by scanning QR codes. Stack: Angular, ASP.NET Core (.NET 10 LTS), DynamoDB (catalog, media, menus, publishing), PostgreSQL (tenancy, identity, audit, deletion tracking), S3 + CloudFront, Terraform on AWS. The Claude API comes in phase 2 (AI menu import). Single-developer learning project, ~20 h/week, local-first.

## 2. Current phase

| Item | State |
|------|-------|
| Phase | **Rev 0.3 architecture baseline approved and published (2026-11-21).** Ready for implementation kickoff. |
| Application code | **Not started.** Start week 1 only when the user gives the go-ahead. |
| Plan | `docs/10-implementation-roadmap.md` §12 (12 weeks: local MVP-1 in weeks 1–8, first AWS environment in weeks 9–12) |
| Kickoff checklist | `docs/14-rev-0.3-baseline-and-kickoff.md` §6 |
| Repo folders | `docs/` (complete), `src/backend`, `src/frontend`, `infrastructure/terraform`, `tests/` (empty) |
| Git | Not a git repository yet (first kickoff item) |

## 3. Session history

| # | Date | What was done |
|---|------|---------------|
| 1 | 2026-11-19 | Rev 0.1: docs 01–11 written. |
| 2 | 2026-11-19 | Rev 0.2: Architecture Review 0.2 applied (hybrid DB accepted, TenantIndex, authoritative pointer, static public app, 128-bit QR, .NET 10, ordering/payments out, multiple locations, food first, single-developer local-first roadmap). |
| 3 | 2026-11-19 | Rev 0.3 draft: sign-off review (doc 12): findings TI-1…13, PUB-1…14, X-1…10, R-1…15; U3–U13 defaults; proposed roadmap. |
| 4 | 2026-11-19 | Created `progress.md`. |
| 5 | 2026-11-19 | Created `CLAUDE.md`. |
| 6 | 2026-11-21 | Provisional agreement recorded; doc 13 (decision confirmation) created. |
| 7 | 2026-11-21 | U12: ~20 h/week; learning priorities; AI → phase 2; protected tests rule; roadmap doc 10 §12. |
| 8 | 2026-11-21 | **Formal approval:** P1–P21 (no exceptions), launch market **United States**, roadmap §12. Applied all doc 13 §4 changes across docs 01–11. ADR-04/05/06/08/09/10/11/15 → Accepted. Added ADR-17 (US market; `us-east-1` proposed) and ADR-18 (work items + outbox; no SQS/Streams). Created doc 14 (baseline + final kickoff checklist). Removed all "⚠ Sign-off review" notes. Updated README revision history, `CLAUDE.md` gate. |

## 4. Decisions

- **Accepted:** ADR-01 … ADR-16, ADR-18; ADR-17 market (United States).
- **Proposed:** ADR-17 region `us-east-1` (confirm before week 9).
- **Refinements made while applying the approval (doc 14 §3, F1–F7):** no SQS (work items + leases); manifest builds serialized by a MANIFEST lease (FIFO removed, PUB-2 resolved); no DynamoDB Streams (transactional outbox items); 90 locations per menu; explicit intentional upserts; GSI1 name truncation; US defaults (USD, en-US, FDA nine allergens, optional calories field).

## 5. Open items (none blocks week 1)

- L1–L2: legal review of the 30-day grace period (**provisional**) and retention periods.
- L3: US food-information rules (allergen notices, calorie labeling for chains with 20+ locations), ideally before week 3.
- L4: ADA Title III accessibility. L5: US state privacy laws, privacy policy, sub-processors. L6: library licences (week 1).
- ADR-17 region confirmation (before week 9). U8, U9, U10, T1, T3, T4, T5 (doc 11 Part C.2).

## 6. Next steps

1. Wait for the user's go-ahead for week 1.
2. Week 1 per doc 14 §6: `git init`, version verification (`docs/architecture/versions.md`), library licences, .NET 10 solution skeleton + architecture tests, docker compose (PostgreSQL, DynamoDB Local, LocalStack S3, Keycloak, Mailpit), GitHub Actions CI, Angular workspace (`admin`, `public`), AWS hardening + budget alarms + Terraform state bucket.
3. Update this file at the end of each week.

## 7. Key files

| File | What it holds |
|------|---------------|
| `docs/14-rev-0.3-baseline-and-kickoff.md` | Baseline summary, refinements F1–F7, final MVP-1 scope, open items, kickoff checklist |
| `docs/11-architecture-review-and-open-questions.md` | ADR register (01–18), missing requirements, decision status |
| `docs/10-implementation-roadmap.md` | §12 approved plan, protected tests (§12.2), flex scope (§12.3) |
| `docs/architecture/04` | HLD; publish flow §4.1; QR resolution §4.2; hosting §6.2 |
| `docs/database/07` | DynamoDB principles, tables (catalog, menus, publishing, media), TenantIndex and deletion §7 |
| `docs/database/06`, `08` | Hybrid DB, outbox/work items; PostgreSQL ERD and retention |
| `docs/architecture/09` | Security, S3 layout, QR and rate limiting |
| `docs/12`, `docs/13` | Review and confirmation records |

## 8. Working conventions

- Requirement tags: [S] stated · [D] derived · [A] assumption · [R] recommendation.
- Don't invent business requirements; don't silently change accepted ADRs; record every change set in the README revision history with the reason.
- Never cut protected tests (doc 10 §12.2); cut flex scope or slip the timeline instead.
