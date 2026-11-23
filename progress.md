# MenuX — Progress Log

Purpose: restore context quickly if a working session is lost. Read this first, then `docs/14-rev-0.3-baseline-and-kickoff.md` and `docs/README.md`.

Last updated: 2026-11-21

---

## 1. Project in one paragraph

MenuX is a multi-tenant SaaS platform where US food businesses (restaurants, cafes, hotel outlets, food shops) create digital menus that guests open by scanning QR codes. Stack: Angular, ASP.NET Core (.NET 10 LTS), DynamoDB (catalog, media, menus, publishing), PostgreSQL (tenancy, identity, audit, deletion tracking), S3 + CloudFront, Terraform on AWS. The Claude API comes in phase 2 (AI menu import). Single-developer learning project, ~20 h/week, local-first.

## 2. Current phase

| Item | State |
|------|-------|
| Phase | Week 1 approved (W1–W3); **week-2 gate decided 2026-11-21** (C5 weeks 2–12, ADR-06, ADR-18 → SQS, U4, U7; doc 13 §1d). C1 open for the other P-items; C4/ADR-17 open. Later weeks gated by doc 13 §1c. |
| Application code | Week 1 pushed (`week1/foundation`, PR #1 open, CI green). **Week 2 in progress** on local branch `week2/identity-tenancy`: slices 1 (PostgreSQL isolation) and 2 (BFF + Keycloak) done; next: tenant resolution/permissions/endpoints/audit/cross-tenant harness, minimal SQS + deletion skeleton, OpenTelemetry. Remaining week 1: required checks (product owner). Budget alert, state bucket and AWS-only checks **moved to the final AWS phase** (weeks 9–12; 2026-11-22, doc 10 §12.7); Terraform prepared locally only. |
| Plan | `docs/10-implementation-roadmap.md` §12 — weeks 2–12 **approved as written** (2026-11-21); if behind, the end date slips, nothing is cut or deferred |
| Kickoff checklist | Draft: `docs/14-rev-0.3-baseline-and-kickoff.md` §6 |
| Repo folders | `docs/` (complete), `src/backend`, `src/frontend`, `infrastructure/terraform`, `tests/` (empty) |
| Git | Repository `https://github.com/jameelvm/MenuX.git`, branch `main`; first commit `c57b5ab` (Rev 0.3 baseline) |

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
| 9 | 2026-11-21 | `git init` (branch `main`), `.gitignore`, `.gitkeep` placeholders; first commit `c57b5ab` pushed to `https://github.com/jameelvm/MenuX.git`. `PROJECT-CONTEXT.md` and `chatgptprompttoresume.txt` are the user's own files; leave them as they are. |
| 10 | 2026-11-21 | **Correction:** the user stated the US launch market, P1–P21 and doc 10 §12 were not explicitly approved. Reverted ADR-04/05/06/08/09/10/11/15 to Proposed; ADR-17 and ADR-18 Proposed; doc 14 and all Rev 0.3 markers marked draft; doc 12/13 status corrected; C1, C4, C5 reopened; correction recorded in the README revision history. Not committed yet (GitHub `c57b5ab` still shows "approved" wording). |
| 11 | 2026-11-21 | User input (not yet a decision): **long-term target market UAE; validate the MVP with US restaurants first (pilot), then launch commercially in the UAE.** Claude presented proposed changes (C4, ADR-17 rewrite, new ADR-19, P2/P14/P15, F6/F7, docs, roadmap) and the US-vs-UAE risk analysis in chat. |
| 12 | 2026-11-21 | User chose **"Yes, Option B"** (via question prompt): applied items 1–7 as **local drafts** — C4 draft (US pilot / UAE target), ADR-17 rewritten (`me-central-1` conditional on week-1 check, UAE storage disclosed) and ADR-19 added (market packs), both **Proposed**; P2/P14/P15, F6/F7 revised; FR-CAT-09 (halal/pork/alcohol optional), FR-MENU-12 (price display), NFR-ACC-03 RTL Must; `tenants.market_pack`/`home_region`, `locations.price_display`; roadmap §12 adjusted (~3–5 days, protected tests not squeezed); legal tracks split US pilot / UAE launch; README draft row. C1 and C5 still unconfirmed. **Not committed.** |
| 13 | 2026-11-21 | Added **proposed** US pilot success criteria (doc 01 §11: S1–S14 + decision rule, not approved); added an explicit note to doc 10 §12.2 that the protected tests are unchanged by localization work. Presented the C1/C5 review checklist in chat (flagged: pilot = real tenants vs "before real tenants" items; pilot not scheduled in the 12 weeks; learning-environment deviations). Local drafts only. |
| 14 | 2026-11-21 | Drafts: T1 = required gate before any real pilot tenant; external pen test = required gate before UAE commercial launch (docs 09, 10 §8, 11 T1, 14). Learning-environment risk assessment E1–E9 + safeguards G1–G12 (doc 04 §5.3, **not accepted**). Post-week-12 schedule (doc 10 §12.5: readiness → legal → onboarding → 6–8-week pilot → evaluation). Pilot criteria reviewed → 6 core measures CM1–CM6, diagnostics, gates, UAE validation track (doc 01 §11, proposed). C1, C5, statuses unchanged; not committed. |
| 15 | 2026-11-21 | Decision list narrowed to week-1 blockers. **User confirmed W1** (week 1 may start while C1/C5 open), **W2** (week-1 scope only; no running AWS app resources, no real tenant data), **W3** (plain Docker Compose). Recorded in CLAUDE.md, doc 10 §12 note + §4, doc 11 C.0 + T3, doc 13 §1b–§1c (gates by week), doc 14 header + §6, README. No ADR status changed. Awaiting approval to commit/push. |
| 16 | 2026-11-21 | Committed and pushed `9a74e0e`. **Week-1 work started** on branch `week1/foundation` (local, **not committed**). Done and verified locally: .NET 10 solution skeleton (8 modules × `X.Contracts` + `X`, hosts, building blocks); architecture tests (36 pass; an injected violation is caught); `compose.yaml` (PostgreSQL 18.6, DynamoDB Local 3.3.2, LocalStack 4.14.0, Mailpit 1.34.0) with smoke checks; `docs/architecture/versions.md`; `docs/architecture/library-licences.md` (research only); `.github/workflows/ci.yml` (not pushed, not run). **Findings:** AWS Health Dashboard says `me-central-1` is unable to reliably support customer applications (since 2026-03; latest update 2026-09-15) → ADR-17 blocker reported, ADR-17 unchanged (Proposed); LocalStack images after 2026-03-23 need an auth token. **Blocked:** Angular workspace waits for Node ≥ 24.15 (product owner updating). No AWS account changes. |
| 17 | 2026-11-21 | Node 24.22.3 installed (winget, UAC by product owner). **Angular 22.3 workspace** (`admin`/`public`, prefixes `adm`/`pub`, strict) verified. ADR-17 evidence re-checked and recorded (versions.md §3.2: AWS 2026-04-30 and 2026-09-15 statements; documented fallback Option A); ADR-17 unchanged. LocalStack kept at 4.14.0 as a **temporary** local/CI test dependency, no token (product owner); limitations and reassessment triggers documented in versions.md §1.1 and the compose comment. Review of src structure; product owner approved: **rename to MenuX (code + docs)**, per-kind project-reference allowlist tests (78 tests pass, negative checks fail as expected), removal of empty layer folders/.gitkeep, `*.zip` ignored, Angular prefixes + explicit strict. `docs.zip` no longer on disk (not removed by Claude). Not committed. |
| 18 | 2026-11-21 | **Week 1:** commit `0d16361` pushed to `origin/week1/foundation`; PR #1 (`week1/foundation` → `main`) open, not merged. **GitHub CI green** for `0d16361`: `Backend build and tests` and `Frontend build and tests` passed in the push and pull-request runs. Required checks recommended for `main` (product owner configures them in GitHub). Terraform 1.17.2 (HashiCorp binary, GPG + SHA-256 verified, user PATH) and AWS CLI 2.42.1 (signed AWS MSI) installed; **no credentials configured, no AWS calls made**. An existing `~/.aws` config/credentials (created 2026-10-22) was found and not opened. Product owner confirmed root MFA and an IAM Identity Center user exist on the existing AWS account (not verified by Claude). |
| 19 | 2026-11-21 | **Week-2 gate decisions confirmed by the product owner** (restated and confirmed via question prompt): C5 approved as written (end date slips if behind; §12.3 = priority order); ADR-06 Accepted; ADR-18 rewritten → Amazon SQS via transactional outbox relay (DLQs, idempotent handlers, SNS later; local SQS on the same LocalStack image), supersedes P8/F1; U4 and U7 approved. Further confirmed: DLQ 5 failed receives / 14-day retention + DLQ procedure (doc 06 §5.1); SQS minimal setup in week 2, media week 3, publishing week 5; temporary AWS test deployments in `us-east-1` with teardown of MenuX test resources only (doc 10 §12.6; ADR-17 unchanged); USD 25/month budget alert (email kept out of the repo); state bucket `us-east-1` encrypted/versioned/no public access; SkiaSharp + QRCoder. Docs 04–11, 13, 14, README, versions.md, library-licences.md, CLAUDE.md updated, on branch `docs/week2-gate-decisions`. |
| 20 | 2026-11-22 | Pushed `36bfebe` (`week1/foundation`) and `8d0869e` (`docs/week2-gate-decisions`; PR into `week1/foundation` to be opened by the product owner). **ADR-17 deadline → "before the pilot environment"** (status Proposed). **Product owner: no AWS deployment until the final AWS phase** (weeks 9–12): Terraform prepared locally only; no state bucket, budget, OIDC provider, deployer role or test environment yet; no SSO profile or account ID needed now; AWS-only checks pending (doc 10 §12.7, A1–A9). Existing `~/.aws` credentials left untouched. |
| 21 | 2026-11-22 | Decision branch committed and pushed (`b60f4ef` ADR-17 deadline, `a77719d` AWS deferral); `review/` deleted (never committed). **Week 2 started** on local branch `week2/identity-tenancy` (stacked on the decision branch; not pushed). **Slice 1** `3f82c00`: PostgreSQL roles (owner/app/platform/rls_definer), SQL-first migrations + `MenuX.Host.Migrator`, `tenancy`/`identity`/`audit` schemas with forced RLS, `ITenantContext` + `SET LOCAL` interceptor, transaction-required guard; integration tests via Testcontainers (found: EF skips the transaction for single-statement SaveChanges → fixed with `TenantScopedDbContext`). **Slice 2** `9ba2ffe`: admin BFF (server-side OIDC code + PKCE/PAR, `__Host-` session cookie with iss+sub only, 401 for API calls, anti-forgery on logout), Keycloak 26.8.2 in compose with synthetic users; end-to-end login tests (found: OIDC handler deletes `iss` → kept). 103 tests pass; Release build clean. |

## 4. Decisions

- **Accepted (Rev 0.2):** ADR-01, 02, 03, 07, 12, 13, 14, 16.
- **Proposed, pending confirmation:** ADR-04, 05, 06, 08, 09, 10, 11, 15, 17 (US pilot → UAE launch; `me-central-1` proposed, region decision before the pilot environment), 18, 19 (market packs); P1–P21; roadmap doc 10 §12.
- **Market strategy (draft, user input 2026-11-21):** US = pilot/validation market; UAE = target commercial launch market.
- **Accepted 2026-11-21:** ADR-06 (Fargate, Cognito/Keycloak, BFF, provider-neutral identity), ADR-18 (Amazon SQS via outbox relay, DLQs, idempotent handlers, SNS later). **Approved:** C5, U4/P13, U7/P16. **Superseded:** P8, F1.
- **Draft refinements awaiting approval (doc 14 §3, F1–F7):** ~~no SQS~~ (F1 superseded by ADR-18); manifest builds serialized by a MANIFEST lease (FIFO removed, PUB-2 resolved); no DynamoDB Streams (transactional outbox items); 90 locations per menu; explicit intentional upserts; GSI1 name truncation; US defaults (USD, en-US, FDA nine allergens, optional calories field).

## 5. Open items (none blocks week 1)

- L1–L2: legal review of the 30-day grace period (**provisional**) and retention periods.
- L3: US food-information rules (allergen notices, calorie labeling for chains with 20+ locations), ideally before week 3.
- L4: ADA Title III accessibility. L5: US state privacy laws, privacy policy, sub-processors. L6: library licences (week 1).
- ADR-17 region confirmation (before the pilot environment). U8, U9, U10, T1, T3, T4, T5 (doc 11 Part C.2).

## 6. Next steps

1. Finish week 1: product owner reviews the `week1/foundation` diff; commit/push only on approval (then CI runs). AWS steps (budget, state bucket, SSO profile, account ID, AWS-only checks) are deferred to the final AWS phase (2026-11-22); nothing is deployed before then. Product owner to note the `me-central-1` blocker (versions.md §3.2) for the ADR-17 decision before the pilot environment. Before week 2, get the week-2 decisions in doc 13 §1c (C5, ADR-06, ADR-18, U4, U7) — each answered separately.
2. Week 1 per doc 14 §6 (git setup already done): version verification (`docs/architecture/versions.md`), library licences, .NET 10 solution skeleton + architecture tests, docker compose (PostgreSQL, DynamoDB Local, LocalStack S3, Keycloak, Mailpit), GitHub Actions CI, Angular workspace (`admin`, `public`), AWS hardening + budget alarms + Terraform state bucket.
3. Update this file at the end of each week.

## 7. Key files

| File | What it holds |
|------|---------------|
| `docs/14-rev-0.3-baseline-and-kickoff.md` | Baseline summary, refinements F1–F7, final MVP-1 scope, open items, kickoff checklist |
| `docs/11-architecture-review-and-open-questions.md` | ADR register (01–18), missing requirements, decision status |
| `docs/10-implementation-roadmap.md` | §12 plan (weeks 2–12 approved), protected tests (§12.2), priority order if behind (§12.3) |
| `docs/architecture/04` | HLD; publish flow §4.1; QR resolution §4.2; hosting §6.2 |
| `docs/database/07` | DynamoDB principles, tables (catalog, menus, publishing, media), TenantIndex and deletion §7 |
| `docs/database/06`, `08` | Hybrid DB, outbox relay → SQS; PostgreSQL ERD and retention |
| `docs/architecture/09` | Security, S3 layout, QR and rate limiting |
| `docs/12`, `docs/13` | Review and confirmation records |

## 8. Working conventions

- Requirement tags: [S] stated · [D] derived · [A] assumption · [R] recommendation.
- Don't invent business requirements; don't silently change accepted ADRs; record every change set in the README revision history with the reason.
- Never cut protected tests (doc 10 §12.2); cut flex scope or slip the timeline instead.
