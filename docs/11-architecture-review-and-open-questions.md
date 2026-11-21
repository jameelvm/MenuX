# 11 — Architecture Review: ADRs, Missing Requirements and Unresolved Decisions

Revision **0.3 — DRAFT, pending approval**. This document is the ADR (Architecture Decision Record) register. It records each decision with its status, then the gaps and decisions still open.
History: Rev 0.1 drafted the ADRs; Rev 0.2 accepted the review decisions; the Rev 0.3 sign-off review ([doc 12](12-architecture-signoff.md)) recommended "Approve with changes" for the proposed ADRs; approval of P1–P21, the launch market and the roadmap is **not yet confirmed** (correction 2026-11-21, [doc 13](13-decision-confirmation.md)). Draft summary: [doc 14](14-rev-0.3-baseline-and-kickoff.md).

ADR statuses: **Accepted** · **Proposed** · **Superseded**.

---

## Part A — Architecture Decision Records

### ADR index

| ADR | Title | Status | Last changed |
|-----|-------|--------|--------------|
| 01 | Runtime: .NET 10 LTS | **Accepted** (Rev 0.2) | 0.2 |
| 02 | Modular monolith | **Accepted** (original brief) | 0.2 |
| 03 | Hybrid DynamoDB + PostgreSQL | **Accepted** (Rev 0.2) | 0.2 |
| 04 | Customers read static projections, never the database | **Proposed** (Rev 0.3 draft; recommended with amendments) | 0.3 |
| 05 | Public app: static prerendered shell + client-side data | **Proposed** (Rev 0.3 draft; recommended with amendments) | 0.3 |
| 06 | Fargate; Cognito in AWS, Keycloak locally; BFF session | **Proposed** (Rev 0.3 draft; recommended with amendments) | 0.3 |
| 07 | Catalog and Menu Authoring as separate modules | **Accepted** (Rev 0.2) | 0.2 |
| 08 | AI suggestions require human approval | **Proposed** (Rev 0.3 draft; recommended with amendments) | 0.3 |
| 09 | Tenant data enumeration and deletion via TenantIndex | **Proposed** (Rev 0.3 draft; with TI-1…TI-13) | 0.3 |
| 10 | Authoritative publication pointer + publish state machine | **Proposed** (Rev 0.3 draft; with PUB-1…PUB-14) | 0.3 |
| 11 | QR identifiers (128-bit) and rate limiting | **Proposed** (Rev 0.3 draft; recommended with amendments) | 0.3 |
| 12 | Ordering and online payments out of MVP-1 | **Accepted** (Rev 0.2) | 0.2 |
| 13 | Multiple locations per tenant | **Accepted** (Rev 0.2) | 0.2 |
| 14 | Food businesses first; no retail inventory | **Accepted** (Rev 0.2) | 0.2 |
| 15 | First AI capability: menu import (behind human review) — **phase 2** | **Proposed** (pending product confirmation) | 0.3 |
| 16 | Single-developer, local-first delivery | **Accepted** (Rev 0.2) | 0.2 |
| 17 | Markets (US pilot → UAE launch) and hosting region | **Proposed** (draft 2026-11-21): US pilot, UAE target; `me-central-1` proposed (region decision before week 9) | new 0.3 |
| 18 | Background work and events without SQS or DynamoDB Streams in MVP-1 | **Proposed** (follows from P8; pending confirmation) | new 0.3 |
| 19 | Market-neutral core with market packs | **Proposed** (draft 2026-11-21) | new 0.3 |

### ADR-01: Runtime — .NET 10 LTS
- **Status:** Accepted (Rev 0.2). Superseded the brief's ASP.NET Core 8.
- **Context:** .NET 8 LTS reached end of support on 2026-11-10. .NET 10 is the current LTS (released November 2025, supported to November 2028).
- **Decision:** .NET 10 / ASP.NET Core 10 / EF Core 10. PostgreSQL and Angular versions are verified when preparing the implementation environment (doc 10 §3).

### ADR-02: Modular monolith
- **Status:** Accepted (requested in the original brief).
- **Decision:** One solution with two hosts (API, worker). Module boundaries are enforced by architecture tests. Each module owns its data store (a PostgreSQL schema or a DynamoDB table), and modules communicate through contracts and an outbox. Architecture tests in CI are mandatory.

### ADR-03: Hybrid DynamoDB + PostgreSQL
- **Status:** Accepted (Rev 0.2 #1).
- **Decision:** DynamoDB for Catalog, Media, Menu Authoring and Publishing metadata (and AI jobs in phase 2). PostgreSQL for tenancy, identity, audit, deletion tracking, and later billing, AI usage, ordering and payments.
- **Context:** the Rev 0.1 challenge (doc 06 §2) is kept. Its costs are accepted, with the mitigations in doc 06 §2.5.

### ADR-04: Customers read static projections, never the database
- **Status:** Proposed (Rev 0.3 draft; recommended with amendments, pending confirmation).
- **Decision:** The full guest path is static: CloudFront serves the app shell, QR and route files, location manifests and content-addressed snapshots from S3. MVP-1 has no public API endpoint. DynamoDB holds the authoritative state; every public file is a rebuildable projection.
- **Rev 0.3 amendments:** **separate hostnames** for the public menu (no cookies) and the admin app + API (X-5); a cache-control table per file type (doc 04 §4.1); **TTL-based manifest refresh (~30 s)** with invalidation only on revocation, slug rename, suspension and deletion (PUB-12); the CloudFront Function rejects malformed QR codes at the edge.

### ADR-05: Public app hosting
- **Status:** Proposed (Rev 0.3 draft; recommended with amendments, pending confirmation).
- **Decision:** Angular static build: a prerendered app shell plus client-side rendering of CDN JSON, hosted on S3 + CloudFront. A CloudFront Function rewrites `/q/*` and `/m/*` to the shell. No SSR server. Static prerendering (build time, shell only) and dynamic SSR (per request, rejected) are distinguished in doc 04 §6.2.
- **Rev 0.3 amendments:** guest-facing UI labels come from **runtime JSON dictionaries**, not per-locale builds; a supported-browser matrix that includes **in-app browsers**; an architecture lint rule so the public project can't import admin libraries or large UI kits.
- **Revisit trigger:** if the performance budget fails, or SEO/link previews become requirements → evaluate publish-time rendering first, dynamic SSR last.

### ADR-06: Hosting and identity
- **Status:** Proposed (Rev 0.3 draft; recommended with amendments, pending confirmation).
- **Decision:** ECS Fargate for the API and worker. Cognito in AWS, Keycloak locally, both used only through standard OIDC.
- **Rev 0.3 amendments:** **BFF cookie session** for the admin portal (server-side authorization code + PKCE; HttpOnly session cookie on the admin hostname; no tokens in the browser). **Provider-neutral identity** (`users.idp_issuer` + `users.idp_subject`, X-2). The application reads only `iss` + `sub` from tokens; everything else comes from the database. Token-validation contract tests use Keycloak tokens and Cognito-shaped fixtures.
- **Re-open if:** enterprise per-tenant SSO becomes a near-term requirement (U9).

### ADR-07: Catalog and Menu Authoring as separate modules
- **Status:** Accepted (Rev 0.2). Rev 0.3 draft adds a separate **Media** table (X-1), which doesn't change this split.

### ADR-08: AI suggestions require human approval
- **Status:** Proposed (Rev 0.3 draft; recommended with amendments, pending confirmation). Applies when AI work starts (phase 2, ADR-15).
- **Decision:** AI never writes published content; all output is a reviewable suggestion that, once accepted, becomes a draft change. Asynchronous, metered per tenant, quota-limited, one tenant per request, no action-taking tools.
- **Rev 0.3 amendments:** uploaded source files at `t/{tenantId}/ai-inputs/…` with a 30-day lifecycle; import **extracts allergens only when printed** and flags them "unverified", never inferring them; JSON through **structured outputs** (forced `tool_choice` isn't supported on the default model); handle `refusal` stop reasons; set effort explicitly; at most one concurrent AI job per tenant; limits on file size and pages. Default model `claude-opus-5-5`; a cheaper model only after measured evaluation.

### ADR-09: Tenant data enumeration and deletion via TenantIndex
- **Status:** Proposed (Rev 0.3 draft, with TI-1 … TI-13; pending confirmation).
- **Decision:** Every tenant-owned item in every module table carries `TENANT_PK`/`TENANT_SK`, indexed by a keys-only `TenantIndex`. All writes go through one `DynamoItemWriter` that always sets the tags and blocks accidental upserts. Deletion: tenant guard → quiet period → projections first → resumable TenantIndex purge → verification passes (30 min and 24 h) → S3 versions and delete markers → identities → finalization by a supervisor. Full design: doc 07 §1, §7.

### ADR-10: Authoritative publication pointer + publish state machine
- **Status:** Proposed (Rev 0.3 draft, with PUB-1 … PUB-14; pending confirmation).
- **Decision:** `T#{t}#M#{m} / CURRENT` is the single source of truth for what is live. A per-menu `ACTIVEJOB` lock serializes PUBLISH, ROLLBACK and UNPUBLISH jobs. The commit is one transaction (tenant guard, pointer, immutable full-state version, location index, job, lock release, outbox event). Per-location `MANIFEST` state drives rebuilding of manifests, which are rebuilt under a lease. Full design: doc 04 §4.1, doc 07 §3.3.
- **Rev 0.3 note:** with poller-only dispatch (P8), per-location serialization uses a lease on the `MANIFEST` item instead of an SQS FIFO queue. PUB-2 (the FIFO dedup-ID defect) is resolved by removing the FIFO (ADR-18).

### ADR-11: QR identifiers and rate limiting
- **Status:** Proposed (Rev 0.3 draft; recommended with amendments, pending confirmation).
- **Decision:** 128 random bits (base64url, 22 characters) from a cryptographically secure generator. WAF per-IP rate rules on public paths. Edge rejection of malformed codes. 403/404 negative caching. **QR-miss alarms in MVP-1, automatic IP blocking in phase 2.** The CloudFront-scope WAF web ACL lives in `us-east-1`. ASP.NET rate limiter on the admin API.

### ADR-12: Ordering and online payments out of MVP-1
- **Status:** Accepted (Rev 0.2 #7). Rev 0.3 draft proposes **SaaS billing deferred** (U7, pending confirmation), with static plan limits in MVP-1.

### ADR-13: Multiple locations per tenant
- **Status:** Accepted (Rev 0.2 #8). Rev 0.3 draft (U4, pending confirmation): hotel outlets are locations; no hierarchy above the tenant in MVP-1.

### ADR-14: Food businesses first
- **Status:** Accepted (Rev 0.2 #9).

### ADR-15: First AI capability — menu import from photo/PDF
- **Status:** Proposed (pending product confirmation). Behind human review (ADR-08). **Timing: phase 2**, together with the Claude API integration (P19, doc 10 §12.4).

### ADR-16: Single-developer, local-first delivery
- **Status:** Accepted (Rev 0.2 #11). Rev 0.3 draft (pending confirmation): ~20 h/week; 12 weeks = local MVP-1 + first AWS environment; protected tests never cut (doc 10 §12).

### ADR-17: Markets and hosting region
- **Status:** **Proposed** (draft 2026-11-21; part of C4, pending final approval).
- **Decision:** **Pilot/validation market: United States** (US restaurants, pilot terms). **Target commercial launch market: United Arab Emirates.** Hosting (**proposed**): a single environment in **`me-central-1`** (UAE) from week 9 for the US pilot and then the UAE launch. Week 1 is **feasibility/service verification only** (checking that every required AWS service and version is available there), with no region commitment; the **region decision is due before week 9**. If anything is missing, the specific blocker and the fallback (Option A: `us-east-1` for the pilot, a UAE deployment later) go to the product owner before the decision changes.
- **Requirements regardless of region:** Terraform takes the region as a parameter, nothing hard-coded; `tenants.home_region`; CloudFront certificate and CloudFront-scope WAF in `us-east-1` (AWS requirement); no region-specific features.
- **Disclosure:** US pilot users are told their data is stored in the UAE.
- **Trade-offs:** validates the target region early and avoids a migration; admin portal latency for US pilot staff is roughly 200–250 ms; AWS prices are somewhat higher; guests are served from CloudFront edges, so guest latency is unaffected.
- **Needed by:** week 1 — feasibility/service check only (no region commitment); before week 9 — region decision, ahead of deployment.

### ADR-18: Background work and events without SQS or DynamoDB Streams in MVP-1
- **Status:** Proposed (Rev 0.3 draft). Follows from poller-only dispatch (P8), which is pending confirmation.
- **Decision:** All background work is represented as **work items** claimed with a lease: DynamoDB items in a sparse `WorkIndex` (publish jobs, dirty manifests, pending QR/route projections, media processing) and PostgreSQL rows (`FOR UPDATE SKIP LOCKED`; tenant purge modules). Integration events use a **transactional outbox** in both stores, dispatched by worker pollers to in-process handlers. **No SQS and no DynamoDB Streams in MVP-1.**
- **Why:** one dispatch mechanism locally and in AWS; less infrastructure; no stream-reader limits; purge deletes can't trigger downstream reactions through streams (TI-5); polling cost is negligible at MVP volume.
- **Trade-off:** a few seconds of dispatch latency; polling load grows with worker count. **Revisit trigger:** if job volume or latency needs exceed polling, introduce SQS behind the same work-item interfaces.

### ADR-19: Market-neutral core with market packs
- **Status:** **Proposed** (draft 2026-11-21).
- **Decision:** A **market pack** holds everything market-specific: default currency, languages and text direction, allergen scheme, dietary and compliance attributes, price-display policy, date and number formats, legal notices, region. **No market-specific value is hard-coded.**
  - **US pilot pack:** USD; `en-US`; FDA nine major allergens; prices shown before tax.
  - **UAE pack:** AED (100 fils); `ar-AE` + `en`, right-to-left; GSO/Codex-based allergen scheme (to verify); `containsPork`/`containsAlcohol`/`halalStatus` attributes (rules to verify); VAT-inclusive price display (to verify); Saturday–Sunday weekend.
- **Consequences:** allergens are stored as canonical codes and displayed through the pack's scheme; Arabic + English and right-to-left work end to end from MVP-1; names are normalized Unicode/Arabic-aware (F6); schedules support date-range overrides (Ramadan, holidays); `tenants.market_pack`; `locations.price_display`.
- **Why:** retrofitting right-to-left, allergen codes and market-specific rules after weeks 3–9 would be expensive; doing it now costs about 3–5 days.

---

## Part B — Missing requirements (status after Rev 0.3)

| # | Gap | Status |
|---|-----|--------|
| M1 | Ordering scope | **Resolved:** out of MVP-1 (ADR-12) |
| M2 | Payments model | Deferred with ordering |
| M3 | AI use cases | **Proposed:** menu import first, phase 2 (ADR-15) |
| M4 | Target markets / regions | **Proposed (draft 2026-11-21):** US pilot, UAE commercial launch; region `me-central-1` proposed (region decision before week 9) |
| M5 | Business model | **Proposed:** billing deferred; static plan limits (U7) |
| M6 | Tenant hierarchy | **Proposed for MVP-1:** locations, outlets = locations, no hierarchy (U4) |
| M7 | Definition of "shops" | **Resolved:** food shops; retail inventory out (ADR-14). Specifics open (U10) |
| M8 | Scale expectations | Open; needed before production |
| M9 | Real-time availability | **Proposed:** phase 2; sold-out flag via publish in MVP-1 (U6) |
| M10 | Allergen/dietary compliance | Market-pack allergen schemes (US: FDA nine; UAE: to verify) and optional halal/pork/alcohol fields; legal review on both tracks |
| M11 | Branding / custom domains | Open (U8), phase 2 |
| M12 | Integrations | Open; not in MVP-1 |
| M13 | Publish approval workflow | **Proposed:** none in MVP-1 (U11) |
| M14 | SLA commitments | Open; before production |
| M15 | Compliance targets | Open; legal items L4–L5 |
| M16 | Analytics | Phase 2 |
| M17 | Support model | Phase 2 (support console) |
| M18 | Retention and deletion periods | **Provisional defaults** (U13), legal review pending |
| M19 | Success metrics | Open |
| M20 | Team, budget, launch date | **Partly resolved:** one developer (ADR-16); ~20 h/week proposed (U12) |

---

## Part C — Decision status

### C.0 Confirmed 2026-11-21 (week-1 start)
W1: week-1 foundation work may start while C1 and C5 remain open. W2: week-1 scope approved (week 1 only; no running AWS application resources, no real tenant data). W3: plain Docker Compose. Later weeks are gated by the decisions in doc 13 §1c. **No ADR status changed.**

### C.1 Provisionally agreed in Rev 0.3 (not yet confirmed)

| ID | Decision | Proposed resolution |
|----|----------|-----------|
| U1 | AI menu import as first AI capability | Accepted; phase 2 (ADR-15) |
| U2 | Markets | **Draft (draft 2026-11-21):** US pilot/validation, UAE commercial launch (C4 pending final approval) |
| U3 | Item reuse; location prices | Reuse yes; per-menu override yes; location pricing phase 2 |
| U4 | Outlets; hierarchy | Outlets = locations; no hierarchy above the tenant |
| U5 | Languages | Multilingual model; default + up to 4 languages, manual; no AI translation in MVP-1 |
| U6 | Schedules; sold-out | Schedules in MVP-1; real-time sold-out in phase 2; sold-out flag via publish |
| U7 | Billing | Deferred; static plan limits + `tenants.plan_key` |
| U11 | Approval workflow | None in MVP-1 |
| U12 | Hours; learning goals | ~20 h/week; DynamoDB + consistency → AWS + Terraform → Angular → security fundamentals → testing/CI/CD/observability/production readiness |
| T2 | BFF vs. tokens | BFF cookie session |
| T6 | Version retention | Last 50 versions per menu; unreferenced snapshots deleted after a 7-day grace period |
| T7 | Locations per menu | Validation limit **90 locations per menu** (7 fixed transaction actions + changed locations ≤ 100) |

### C.2 Still open (none blocks the start of implementation)

| ID | Decision | Needed by | Provisional default |
|----|----------|-----------|---------------------|
| U13 / L1–L2 | Deletion grace period and retention periods | Before real tenants | **30-day grace (provisional)**; doc 12 §6 U13 table |
| L3-US | US pilot: allergen information as tenant-entered (FDA nine); state/local notice rules where pilot venues operate | Before pilot users | US pilot market pack |
| L3-AE | UAE: GSO allergen list; Dubai Municipality / ADAFSA rules on allergen and calorie information on menus; halal, pork and alcohol labelling and licensing; VAT-inclusive price display; Arabic consumer-information requirements | Before UAE launch (week 3 if possible for the data fields) | UAE market pack skeleton; optional fields |
| L4 | Accessibility expectations in the US pilot and the UAE | Before real tenants | WCAG 2.2 AA target, including RTL |
| L5-US | US pilot: pilot terms, privacy notice, state-law applicability, disclosure of storage in the UAE | Before pilot users | No guest personal data in MVP-1 |
| L5-AE | UAE: PDPL (Federal Decree-Law 45/2021) and executive regulations; DIFC/ADGM if applicable; cross-border transfers to sub-processors; Arabic notices | Before UAE launch | Sub-processor register with data locations |
| L6 | Image/QR library licences | Week 1 | MIT-licensed choices |
| ADR-17 | Verify `me-central-1` services and versions | Week 1 | `me-central-1`; blocker and fallback presented if anything is missing |
| U8 | Branding depth; custom domains | Phase 2 | — |
| U9 | Enterprise SSO in year one | Before phase 2 | Not needed |
| U10 | Food-shop specifics (price per weight, prepacked labelling) | Week 3 | Not in MVP-1 |
| T1 | DynamoDB LeadingKeys session policies | **Required gate before onboarding any real pilot tenant** (draft 2026-11-21; doc 10 §12.5) | — |
| T3 | ~~.NET Aspire AppHost~~ | — | **Decided (W3, 2026-11-21): plain Docker Compose** |
| T4 | Search beyond in-memory filtering | When item counts exceed ~2–5k per tenant | Not needed |
| T5 | Publish-time HTML rendering | After the week-7 performance budget | Not needed |
| T8 | Version verification results | Week 1 | Must be recorded |

---

## Part D — Trade-off summary

| Decision | Gain | Cost | Reversible? |
|----------|------|------|-------------|
| Modular monolith | Speed, simplicity, one deploy | Discipline; one scaling unit | Yes |
| Hybrid DB | Purpose-fit stores | Two technologies, eventual consistency, modelling skill | Per module, with effort |
| Fully static public path | Speed, cost, resilience | Publish delay (~30 s); several small fetches | Yes |
| Single authoritative pointer + projections | One truth; safe retries | State machine and reconciler | Yes |
| Per-menu job lock | Simple concurrency; retry and rollback just work | One publish per menu at a time (409) | Yes |
| TenantIndex on every table | Complete enumeration | Extra index write per item write | Yes |
| Work items + outbox instead of SQS/Streams | One mechanism; less infrastructure | Seconds of latency; polling | Yes (SQS behind interfaces) |
| Pooled tenancy | Low cost per tenant | Layered controls needed | Partially |
| Cognito via BFF (Keycloak locally) | Managed MFA; no tokens in the browser | UX limits; migration effort | Hard |
| ECS Fargate | No cold starts | Always-on minimum cost | Yes |
| Async AI + review (phase 2) | Safety, cost control | Not instant | Yes |
| Local-first, single account | Fast learning, low cost | Production topology proven later | Yes |
