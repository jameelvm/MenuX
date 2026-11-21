# 13 — Decision Confirmation (pre-baseline)

Revision 0.3 (draft) · Date: 2026-11-21 · Basis: [doc 12](12-architecture-signoff.md)

> **Status: approved 2026-11-21.** The product owner formally approved P1–P21 with no exceptions, confirmed the United States as the launch market and approved doc 10 §12. The changes in §4 have been applied; the Rev 0.3 baseline is published (doc 14). U13's 30-day grace period remains provisional pending legal review. This document is kept as the confirmation record.
>
> Original status: provisional.

---

## 1. Provisionally agreed (product owner, 2026-11-21)

| # | Decision | Reference | Note |
|---|----------|-----------|------|
| P1 | ADR-04 static public projections, **with doc 12 changes** | doc 12 §2 | Separate hostnames (X-5), cache-control table |
| P2 | ADR-05 static Angular shell + CSR, **with changes** | doc 12 §2 | Runtime UI dictionaries, in-app browser matrix |
| P3 | ADR-06 Fargate + Cognito (Keycloak locally), **with changes** | doc 12 §2 | Provider-neutral identity columns (X-2), token contract tests |
| P4 | ADR-08 AI suggestions need human approval, **with changes** | doc 12 §2 | No allergen inference; AI inputs under the tenant prefix |
| P5 | ADR-09 TenantIndex deletion, **with TI-1…TI-13** | doc 12 §3 | |
| P6 | ADR-10 authoritative pointer, **with PUB-1…PUB-8** | doc 12 §4 | |
| P7 | ADR-11 QR IDs + rate limiting, **with changes** | doc 12 §2 | Edge format check; WAF in `us-east-1` |
| P8 | Poller-only job dispatch (PUB-9) | doc 12 §4.3 | Streams → Pipes dropped from MVP-1 |
| P9 | TTL-based manifest refresh, ~30 s (PUB-12) | doc 12 §4.4 | Invalidate only on revoke, rename and suspension |
| P10 | QR automatic IP blocking → phase 2; alarms in MVP-1 | doc 12 §2 ADR-11 | |
| P11 | BFF cookie session for the admin portal (T2) | doc 12 §2 ADR-06 | |
| P12 | U3: reuse items across menus; per-menu price override; no location pricing | doc 12 §6 | |
| P13 | U4: hotel outlets = locations; no hierarchy above the tenant | doc 12 §6 | |
| P14 | U5: multilingual model; up to 4 extra languages, entered manually; no AI translation | doc 12 §6 | |
| P15 | U6: schedules in MVP-1; real-time sold-out in phase 2 (sold-out flag via publish) | doc 12 §6 | |
| P16 | U7: SaaS billing deferred; static plan limits + `tenants.plan_key` | doc 12 §6 | |
| P17 | U11: no publish approval workflow | doc 12 §6 | |
| P18 | Ordering, online payments, SaaS billing outside MVP-1 | ADR-12, doc 12 §8 | Already accepted for ordering/payments; billing confirmed as deferred |
| P19 | AI menu import as the first AI capability, **behind human review** (ADR-15) | ADR-08, ADR-15 | Supported. **Timing: phase 2** (2026-11-21), together with the Claude API integration. |
| P20 | U12: **about 20 h/week**. Learning priorities: (1) DynamoDB + consistency, (2) AWS + Terraform, (3) Angular, (4) security fundamentals, (5) testing, CI/CD, observability, production readiness | doc 10 §12 | Provided 2026-11-21 |
| P21 | 12 weeks = local MVP-1 + first AWS environment; **critical consistency, isolation and recovery tests are never cut** to fit the schedule | doc 10 §12.2 | Provided 2026-11-21 |

## 2. Requires your explicit confirmation

| # | Decision | Options / default | Needed by | Why it blocks |
|---|----------|-------------------|-----------|---------------|
| C1 | ~~Formal approval of P1–P21~~ | **Approved 2026-11-21, no exceptions** | — | — |
| C2 | ~~U12 weekly hours~~ | **Answered: ~20 h/week** → P20 | — | — |
| C3 | ~~U12 learning priorities~~ | **Answered** → P20 | — | — |
| C4 | ~~U2 initial launch market~~ | **United States** (ADR-17; region `us-east-1` proposed) | — | — |
| C5 | ~~Roadmap doc 10 §12~~ | **Approved as written** | — | — |

## 3. Requires legal or market research

| # | Topic | Provisional default | Depends on |
|---|-------|---------------------|-----------|
| L1 | **U13 tenant deletion grace period** | **30 days**, cancellable; public menus offline immediately (provisional, kept) | Legal review, C4 |
| L2 | U13 retention periods (audit, AI usage, exports, backups, logs) | Doc 12 §6 U13 table | Legal review, C4 |
| L3 | Allergen and food-information rules for menus in the launch market | Allergen/dietary tags modelled; required fields TBD | C4 |
| L4 | Accessibility law for the public menu | WCAG 2.2 AA target (NFR-ACC-01) | C4 |
| L5 | Privacy: data residency, privacy policy, sub-processor list (AWS, Anthropic, email), DPA terms | Region chosen by market; no guest personal data in MVP-1 | C4 |
| L6 | Third-party licences: image library, QR library | MIT-licensed choices preferred | Week-1 verification (technical, no legal advice needed unless a commercial licence applies) |
| L7 | Market check: languages and currencies needed at launch | Up to 5 languages per menu (P14) | C4 |

These don't block weeks 1–8 (local development), except that L3 should be known before catalogue fields are finalized in week 3, if possible.

## 4. Technical changes to apply after approval

Applied in one consistent change set when the Rev 0.3 baseline is prepared. Each change removes the matching "⚠ Sign-off review" note.

| Area | Changes | Documents |
|------|---------|-----------|
| Publishing | ACTIVEJOB lock + `JOB#{ulid}` (PUB-1); FIFO dedup ID per location (PUB-2); serialized pointer writes + `liveDraftVersion` (PUB-3); header edits bump META (PUB-4); MANIFEST state items (PUB-5); UNPUBLISH + location deactivation (PUB-6); full-state `VER#` + rollback + snapshot cleanup grace (PUB-7); tenant guard (PUB-8); poller-only dispatch (PUB-9); lease heartbeat (PUB-10); `MenuPublicationCommitted` (PUB-11); TTL-only manifests (PUB-12); single work-index shard (PUB-14) | 04 §4.1, 05 §4, 06 §4–6, 07 §3.3 |
| TenantIndex | No-upsert writer + `if_not_exists` tagging (TI-1); single `DynamoItemWriter` (TI-2); full-table coverage scan (TI-3); media table + outbox/inbox tagging (TI-4); ignore Stream `REMOVE` (TI-5); guard + verification delays (TI-6); key character rules (TI-7); resume/retry semantics (TI-9); supervisor (TI-10); S3 versions + layout (TI-11); identity and other stores (TI-12); bulk pacing (TI-13) | 07 §1, §3, §7; 06 §9; 09 §4.3–4.4 |
| Cross-doc | X-1 media table · X-2 `idp_issuer`/`idp_subject` · X-3 S3 layout · X-4 `SUG#` · X-5 two hostnames · X-6 TENANT_SK wording · X-7 dispatch · X-9 FR-MENU-07 · X-10 retention | 02, 04, 05, 06, 07, 08, 09 |
| Public app / edge | Runtime UI dictionaries; in-app browser matrix; public-project dependency rule; cache-control table; CloudFront Function format check; WAF `us-east-1`; alarm-only miss detection | 04 §4.2, §6.2; 09 §4.6 |
| Identity | BFF cookie session; provider-neutral identity; token contract tests | 04 §6.5, 08 §4.2, 09 §3 |
| AI | AI inputs at `t/{tenantId}/ai-inputs/` (30-day lifecycle); no allergen inference; structured outputs; per-tenant concurrency 1 | 04 §4.4, 09 §6 |
| Product defaults | U3–U7, U11 into requirements; FR-TEN, FR-CAT, FR-MENU, FR-AI priorities; MVP-1 scope (doc 12 §8) into the PRD | 01, 02, 03 |
| Register | ADR-04/05/06/08/09/10/11/15 → Accepted (Rev 0.3); U-items resolved; Part C reduced to the remaining items | 11 |
| Roadmap | Doc 10 §12 replaces §5 (if C5 approved); §11 kept as history | 10 |
| MVP-1 scope | AI menu import and Claude API → phase 2 (P19); learning priorities (P20); protected tests (P21) | 01 §10, 02 FR-AI, 12 §8 (note), 11 ADR-15 |
| Housekeeping | Revision history Rev 0.3 (approved); `progress.md`; `CLAUDE.md` gate line | README, root files |

## 5. Implementation kickoff checklist (draft — superseded by the final checklist in doc 14 §6)

- [ ] C1–C5 confirmed; Rev 0.3 baseline published; `CLAUDE.md` gate lifted
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
