# 13 — Decision Confirmation (pre-baseline)

Revision 0.3 (draft) · Date: 2026-11-21 · Basis: [doc 12](12-architecture-signoff.md)

> **Status: provisional — awaiting explicit confirmation.** Correction 2026-11-21: an earlier session note recorded P1–P21, the US launch market and doc 10 §12 as approved. The product owner has stated these approvals were not explicitly given, so they are **unconfirmed**. Apart from the week-1 start decisions in §1b (W1–W3), nothing in this document is approved, and ADR statuses in doc 11 are Proposed for these items.
>
> The §4 changes have been drafted into docs 01–11 as proposals (Rev 0.3 draft); they take effect only after confirmation.
>
> **Week 1:** week-1 foundation work is approved (W1–W3, §1b). **Week-2 gate decided 2026-11-21 (§1d):** C5, ADR-06, ADR-18 (SQS), U4, U7. C1 stays open for the other P-items; later work waits only for the specific decisions it needs (§1c).

---

## 1. Provisionally agreed (product owner, 2026-11-21)

| # | Decision | Reference | Note |
|---|----------|-----------|------|
| P1 | ADR-04 static public projections, **with doc 12 changes** | doc 12 §2 | Separate hostnames (X-5), cache-control table |
| P2 | ADR-05 static Angular shell + CSR, **with changes** | doc 12 §2 | Runtime UI dictionaries (`en` + `ar`), in-app browser matrix; **Arabic/RTL from day one** (draft 2026-11-21) |
| P3 | **Approved 2026-11-21.** ADR-06 Fargate + Cognito (Keycloak locally), **with changes** | doc 12 §2 | Provider-neutral identity columns (X-2), token contract tests |
| P4 | ADR-08 AI suggestions need human approval, **with changes** | doc 12 §2 | No allergen inference; AI inputs under the tenant prefix |
| P5 | ADR-09 TenantIndex deletion, **with TI-1…TI-13** | doc 12 §3 | |
| P6 | ADR-10 authoritative pointer, **with PUB-1…PUB-8** | doc 12 §4 | |
| P7 | ADR-11 QR IDs + rate limiting, **with changes** | doc 12 §2 | Edge format check; WAF in `us-east-1` |
| P8 | ~~Poller-only job dispatch (PUB-9)~~ **Superseded 2026-11-21 by ADR-18 (Amazon SQS via outbox relay).** | doc 12 §4.3 | Streams → Pipes dropped from MVP-1 |
| P9 | TTL-based manifest refresh, ~30 s (PUB-12) | doc 12 §4.4 | Invalidate only on revoke, rename and suspension |
| P10 | QR automatic IP blocking → phase 2; alarms in MVP-1 | doc 12 §2 ADR-11 | |
| P11 | **Approved 2026-11-21.** BFF cookie session for the admin portal (T2) | doc 12 §2 ADR-06 | |
| P12 | U3: reuse items across menus; per-menu price override; no location pricing | doc 12 §6 | |
| P13 | **Approved 2026-11-21.** U4: hotel outlets = locations; no hierarchy above the tenant | doc 12 §6 | |
| P14 | U5: multilingual model; up to 4 extra languages, entered manually; no AI translation | doc 12 §6 | **Arabic + English end to end in MVP-1**; full multi-language UI required before the UAE launch (draft 2026-11-21) |
| P15 | U6: schedules in MVP-1; real-time sold-out in phase 2 (sold-out flag via publish) | doc 12 §6 | **Plus date-range overrides** (Ramadan, holidays) (draft 2026-11-21) |
| P16 | **Approved 2026-11-21.** U7: SaaS billing deferred; static plan limits + `tenants.plan_key` | doc 12 §6 | |
| P17 | U11: no publish approval workflow | doc 12 §6 | |
| P18 | Ordering, online payments, SaaS billing outside MVP-1 | ADR-12, doc 12 §8 | Already accepted for ordering/payments; billing confirmed as deferred |
| P19 | AI menu import as the first AI capability, **behind human review** (ADR-15) | ADR-08, ADR-15 | Supported. **Timing: phase 2** (2026-11-21), together with the Claude API integration. |
| P20 | U12: **about 20 h/week**. Learning priorities: (1) DynamoDB + consistency, (2) AWS + Terraform, (3) Angular, (4) security fundamentals, (5) testing, CI/CD, observability, production readiness | doc 10 §12 | Provided 2026-11-21 |
| P21 | 12 weeks = local MVP-1 + first AWS environment; **critical consistency, isolation and recovery tests are never cut** to fit the schedule | doc 10 §12.2 | Provided 2026-11-21 |

### 1a. Market-strategy items adopted as drafts (draft 2026-11-21, Option B chosen)

| # | Draft item | Where |
|---|-----------|-------|
| D1 | C4: pilot/validation market **United States**; target commercial launch market **UAE** | C4 below |
| D2 | ADR-17 rewritten: `me-central-1` proposed from week 9; **week-1 feasibility/service check only**, region decision before week 9; blocker and fallback presented before any change; storage in the UAE disclosed to US pilot users | doc 11 ADR-17 |
| D3 | ADR-19 (new): market-neutral core with market packs; nothing market-specific hard-coded | doc 11 ADR-19 |
| D4 | P2, P14, P15 updated; F6 (Unicode/Arabic normalization) and F7 (market packs) revised; about 3–5 days of work **without squeezing the protected tests or the 12-week frame** | doc 14 §3, doc 10 §12 |
| D5 | Optional `containsPork`, `containsAlcohol`, `halalStatus` fields; rules pending UAE research | doc 02 FR-CAT-09, doc 07 |
| D6 | C1 remains **unconfirmed** except P3, P11, P13, P16 (approved) and P8 (superseded); C5 answered 2026-11-21 | §1d, §2 |
| D7 | Doc-by-doc changes and split legal tracks, all as Rev 0.3 draft; **not committed or pushed** | docs 01–14, README |

ADR-17 and ADR-19 remain **Proposed** until the final approval step.

### 1b. Confirmed week-1 start decisions (product owner, 2026-11-21)

| # | Decision | Answer | Limits |
|---|----------|--------|--------|
| W1 | Allow week-1 foundation work while C1 and C5 remain open | **Yes** | Later work stays gated by the specific decisions in §1c |
| W2 | Approve the week-1 scope (doc 10 §12.1 week 1; doc 14 §6) | **Approved** | Week 1 only. **Doesn't** approve the full architecture or roadmap. **No running AWS application resources and no real tenant data** in this phase |
| W3 | Local orchestration | **Plain Docker Compose** | — |

### 1d. Confirmed week-2 gate decisions (product owner, 2026-11-21)

| # | Decision | Answer | Notes |
|---|----------|--------|-------|
| C5 | Roadmap doc 10 §12, weeks 2–12 | **Approved as written** | Planned work isn't moved, deferred or removed because of time. If behind, the end date slips and the order is kept; §12.3 is a priority order, not a cut list. Protected tests unchanged |
| ADR-06 | Hosting and identity (P3, P11) | **Accepted** | ECS Fargate for API and Worker; Cognito in AWS; Keycloak locally; Admin BFF cookie session; provider-neutral `idp_issuer` + `idp_subject` |
| ADR-18 | Background jobs | **Accepted (rewritten)** | **Amazon SQS from MVP-1**; no database polling as the job queue; transactional outbox relay for reliable enqueue; DLQs; idempotent handlers; SNS later when fan-out is needed. **Supersedes P8 and F1**; F2 (no FIFO) and F3 (no Streams) kept. Local: same LocalStack 4.14.0 image with `sqs` |
| U4 / P13 | Outlets; hierarchy | **Approved** | Each hotel outlet is a location under its tenant; no hierarchy above the tenant in MVP-1 |
| U7 / P16 | SaaS billing | **Approved** | Stripe subscription billing deferred beyond MVP-1; `tenants.plan_key` + static plan limits; `plan_key` stays linkable to the deferred `billing` schema |

| ADR-18 settings | DLQ policy | **Confirmed** | 5 failed receives → DLQ; 14-day DLQ retention; queues `publishing`, `media`, `tenancy`, `events`; DLQ alerting/inspection/redrive documented (doc 06 §5.1) |
| SQS schedule | Build order | **Confirmed** | Week 2: minimal LocalStack SQS + outbox relay + consumer (with the deletion skeleton); week 3: media processing; week 5: publishing-specific work |
| Test deployments | Region and lifecycle | **Confirmed** | `us-east-1`, temporary (deploy, test, tear down; data discardable; not production-ready); only state storage and the budget alert persist; teardown limited to MenuX test resources (doc 10 §12.6). ADR-17 unchanged (Proposed) |
| Budget | AWS budget alert | **Confirmed** | USD 25/month alert, not a cap; recipient = product owner's email, kept out of the repository |
| State bucket | Terraform state | **Confirmed** | `us-east-1`; encryption, versioning, public access blocked |
| L6 | Libraries | **Confirmed** | SkiaSharp (images), QRCoder (QR codes) |
| Signup | Tenant creation (doc 03 J1 open item) | **Confirmed 2026-11-23** | **Self-service:** anyone can create an account; after verifying their email they can create a tenant and become its Owner |
| Email verification | Where it is enforced | **Confirmed 2026-11-23** | **Both** the identity provider (login completes only for verified emails; Keycloak `verifyEmail`, Cognito confirmation) **and** the API (no MenuX user without `email_verified`; tenant creation re-checks; DB constraint) |
| First-login profile | Source of profile data | **Confirmed 2026-11-23** | Email, email-verified and name read from the ID token **once**, at first login, to create the MenuX user; afterwards the MenuX database is the source of truth for profile, memberships and permissions; the session keeps only `iss` + `sub` (ADR-06 unchanged) |
| Billing readiness | Subscription or trial required for restaurant use | **Confirmed 2026-11-23, rules pending** | Architecture ready (commercial-state fields next to `plan_key`, one access-policy seam); **no trial duration, pricing or access rules assumed** — finalised in the billing phase. U7 unchanged (billing deferred) |
| AWS timing | When AWS work happens | **Confirmed 2026-11-22** | No AWS deployment until the final AWS phase (weeks 9–12): no state bucket, budget, OIDC provider, deployer role or test environment before then; no SSO profile or account ID needed yet; Terraform prepared locally (`fmt`/`validate` only); AWS-only checks pending (doc 10 §12.7) |

C1 is therefore decided for **P3, P11, P13, P16** (approved) and **P8** (superseded). The other P-items stay open until their week in §1c.

### 1c. Decision gates by week (from the 2026-11-21 decision list)

| Before week | Decisions needed first (each answered separately) |
|-------------|---------------------------------------------------|
| 2 | **Decided 2026-11-21 (§1d):** C5 roadmap weeks 2–12; ADR-06 (P3, P11); ADR-18 rewritten to SQS via outbox relay (supersedes P8, F1); P13/U4; P16/U7 |
| 3 | ADR-09 (TenantIndex, single writer, deletion — P5, TI-1…13, F5); C4 + ADR-17 + ADR-19 (US pilot → UAE launch, market packs — F6, F7); P12/U3 (item reuse, per-menu prices); P14/U5 (languages, Arabic + English); U10 (food-shop extras) |
| 4 | P15/U6 (schedules + date overrides, sold-out via publish); ADR-05 for admin right-to-left (P2) |
| 5 | ADR-10 (publishing — P6, PUB-1…14, F4); P17/U11 (no approval workflow); ADR-04 (static guest path, manifest TTL — P1, P9) |
| 6 | ADR-11 (QR, rate limits, alarm-only blocking — P7, P10) |
| 7 | ADR-05 public app (P2) |
| 9 | Test deployments in `us-east-1` (decided 2026-11-21, doc 10 §12.6); G2 pilot-account choice (recommended by then) |
| Pilot | **ADR-17 region decision, before the pilot environment** (§12.5 phase A; informed by the week-1 feasibility check and current AWS status); §12.5 schedule; G1–G12 checklist; each residual risk E-3a…E-3e accepted separately (doc 04 §5.3); success measures CM1–CM6 |
| Phase 2 | ADR-08, ADR-15 |

## 2. Requires your explicit confirmation

None of these blocks week-1 foundation work (W1, §1b). Each is needed only by the later work listed in §1c.

| # | Decision | Options / default | Needed by | Why it blocks |
|---|----------|-------------------|-----------|---------------|
| C1 | **Formal approval** of P1–P21 | Approve all / list exceptions, or decide item by item | Not needed for week 1. Each part is needed by the week listed in §1c (from week 2); all of it before the Rev 0.3 baseline is published | Turns proposals into the baseline |
| C2 | ~~U12 weekly hours~~ | **Answered: ~20 h/week** → P20 | — | — |
| C3 | ~~U12 learning priorities~~ | **Answered** → P20 | — | — |
| C4 | **Markets** | **Draft (draft 2026-11-21):** pilot/validation = United States; target commercial launch = UAE; region `me-central-1` proposed (ADR-17; region decision before the pilot environment). Pending final approval | Not needed for week 1 (the `me-central-1` check is a week-1 task under W2). Markets and market packs before week 3; region before the pilot environment (§1c) | Region, data residency, languages, currency, allergen scheme, legal tracks |
| C5 | ~~Roadmap doc 10 §12~~ | **Answered 2026-11-21: weeks 2–12 approved as written** (§1d). The §12.5 pilot schedule is still decided before the pilot (§1c) | — | — |

## 3. Requires legal or market research

| # | Topic | Provisional default | Depends on |
|---|-------|---------------------|-----------|
| L1 | **U13 tenant deletion grace period** | **30 days**, cancellable; public menus offline immediately (provisional, kept) | Legal review, C4 |
| L2 | U13 retention periods (audit, AI usage, exports, backups, logs) | Doc 12 §6 U13 table | Legal review, C4 |
| L3 | Food information: **US pilot** (FDA nine as tenant-entered; state/local notices where pilot venues operate) and **UAE launch** (GSO allergen list; Dubai Municipality / ADAFSA menu rules; halal, pork and alcohol labelling; VAT-inclusive display; Arabic requirements) | Market packs; canonical allergen codes; optional halal/pork/alcohol fields | C4 |
| L4 | Accessibility expectations (US pilot, UAE) | WCAG 2.2 AA including RTL (NFR-ACC-01) | C4 |
| L5 | Privacy: **US pilot** (pilot terms, privacy notice, state-law applicability, UAE storage disclosure) and **UAE launch** (PDPL and executive regulations, DIFC/ADGM if applicable, cross-border transfers, Arabic notices); sub-processor register | `me-central-1`; no guest personal data in MVP-1 | C4 |
| L6 | Third-party licences: image library, QR library | MIT-licensed choices preferred | Week-1 verification (technical, no legal advice needed unless a commercial licence applies) |
| L7 | Market check: languages and currencies (US pilot: English, USD; UAE: Arabic + English, AED) | Up to 5 languages per menu (P14) | C4 |

These don't block weeks 1–8 (local development), except that L3 should be known before catalogue fields are finalized in week 3, if possible.

## 4. Technical changes to apply after approval

Drafted into docs 01–11 on 2026-11-21 as proposals (Rev 0.3 draft); they become the baseline only after C1, C4 and C5 are confirmed. Each change removes the matching "⚠ Sign-off review" note.

| Area | Changes | Documents |
|------|---------|-----------|
| Publishing | ACTIVEJOB lock + `JOB#{ulid}` (PUB-1); FIFO dedup ID per location (PUB-2); serialized pointer writes + `liveDraftVersion` (PUB-3); header edits bump META (PUB-4); MANIFEST state items (PUB-5); UNPUBLISH + location deactivation (PUB-6); full-state `VER#` + rollback + snapshot cleanup grace (PUB-7); tenant guard (PUB-8); poller-only dispatch (PUB-9); lease heartbeat (PUB-10); `MenuPublicationCommitted` (PUB-11); TTL-only manifests (PUB-12); single work-index shard (PUB-14) | 04 §4.1, 05 §4, 06 §4–6, 07 §3.3 |
| TenantIndex | No-upsert writer + `if_not_exists` tagging (TI-1); single `DynamoItemWriter` (TI-2); full-table coverage scan (TI-3); media table + outbox/inbox tagging (TI-4); ignore Stream `REMOVE` (TI-5); guard + verification delays (TI-6); key character rules (TI-7); resume/retry semantics (TI-9); supervisor (TI-10); S3 versions + layout (TI-11); identity and other stores (TI-12); bulk pacing (TI-13) | 07 §1, §3, §7; 06 §9; 09 §4.3–4.4 |
| Cross-doc | X-1 media table · X-2 `idp_issuer`/`idp_subject` · X-3 S3 layout · X-4 `SUG#` · X-5 two hostnames · X-6 TENANT_SK wording · X-7 dispatch · X-9 FR-MENU-07 · X-10 retention | 02, 04, 05, 06, 07, 08, 09 |
| Public app / edge | Runtime UI dictionaries; in-app browser matrix; public-project dependency rule; cache-control table; CloudFront Function format check; WAF `us-east-1`; alarm-only miss detection | 04 §4.2, §6.2; 09 §4.6 |
| Identity | BFF cookie session; provider-neutral identity; token contract tests | 04 §6.5, 08 §4.2, 09 §3 |
| AI | AI inputs at `t/{tenantId}/ai-inputs/` (30-day lifecycle); no allergen inference; structured outputs; per-tenant concurrency 1 | 04 §4.4, 09 §6 |
| Product defaults | U3–U7, U11 into requirements; FR-TEN, FR-CAT, FR-MENU, FR-AI priorities; MVP-1 scope (doc 12 §8) into the PRD | 01, 02, 03 |
| Register | ADR-04/05/06/08/09/10/11/15 → Accepted only after approval (currently Proposed); U-items resolved; Part C reduced to the remaining items | 11 |
| Roadmap | Doc 10 §12 replaces §5 (if C5 approved); §11 kept as history | 10 |
| MVP-1 scope | AI menu import and Claude API → phase 2 (P19); learning priorities (P20); protected tests (P21) | 01 §10, 02 FR-AI, 12 §8 (note), 11 ADR-15 |
| Housekeeping | Revision history Rev 0.3 (approved); `progress.md`; `CLAUDE.md` gate line | README, root files |

## 5. Implementation kickoff checklist (draft — superseded by the checklist in doc 14 §6)

- [x] Week-1 start decided (W1–W3, §1b); C1 and C5 remain open and gate later weeks (§1c)
- [x] Week-2 gate decided 2026-11-21 (§1d): C5, ADR-06, ADR-18 (SQS), U4, U7
- [ ] `git init`, `.gitignore`, first commit of `docs/`
- [ ] Version verification recorded in `docs/architecture/versions.md` (.NET 10 SDK, Npgsql, PostgreSQL major on RDS, Angular major, Node LTS, AWS SDK, Terraform, LocalStack features incl. S3 conditional writes, Keycloak)
- [ ] Image and QR library licences checked (L6)
- [ ] .NET 10 solution skeleton: hosts (Api, Worker), BuildingBlocks, module projects, architecture tests
- [ ] `docker compose` stack: PostgreSQL, DynamoDB Local, LocalStack (S3, SQS), Keycloak realm with two test tenants, Mailpit
- [ ] CI (build + test) on GitHub Actions
- [ ] Angular workspace: `admin` and `public` projects, with the dependency rule
- [ ] AWS account hardening (root MFA, IAM Identity Center user), budget alarms, Terraform state bucket (no running resources)
- [ ] Protected test list (doc 10 §12.2) added to the test plan and CI as required checks

Phase-2 preparation (optional, no effort budgeted in the 12 weeks):
- [ ] Collect ~20 real food menus for the future AI evaluation set
- [ ] Anthropic API key with a spend limit (only when phase 2 starts)
