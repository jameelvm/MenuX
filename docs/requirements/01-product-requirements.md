# 01 — Product Requirements Document (PRD)

Status: **Revision 0.3 — DRAFT, pending approval (doc 13)** · Owner: Product · Changes in 0.3: markets (US pilot → UAE commercial launch, draft), market packs, outlets/hierarchy decisions, AI menu import in phase 2, billing deferred, release plan. (0.2: food first, ordering/payments out, multiple locations.)
Tags: [S] stated · [D] derived · [A] assumption, needs confirmation · [R] recommendation (see [README](../README.md))

---

## 1. Summary

MenuX is a multi-tenant SaaS platform. Hospitality and retail businesses use it to create, manage and publish digital menus. Their customers open those menus by scanning a QR code. [S]

## 2. Problem statement

> The brief does not state the problem. The text below is an **inferred hypothesis [A]** and must be validated with real customers.

Venues use printed menus that are expensive to reprint, slow to update (prices, sold-out items, seasonal changes) and hard to offer in several languages. Existing digital alternatives (PDF behind a QR code) are hard to read on phones and hard to maintain.

## 3. Target customers (tenants)

**Rev 0.2 decision: food businesses first** (ADR-14). General retail inventory is outside the first release.
**Markets (draft 2026-11-21, C4 pending final approval): pilot/validation in the United States, then commercial launch in the United Arab Emirates.** The product is market-neutral with per-market configuration (market packs, ADR-19).

| Segment | Source | Notes / open questions |
|---------|--------|------------------------|
| Restaurants | [S] | Single venue or several; multiple locations per tenant (ADR-13). **No hierarchy above the tenant in MVP-1** (U4): a chain is one tenant with many locations; independently owned franchisees are separate tenants. |
| Hotels | [S] | Usually have **several outlets** (restaurant, bar, room service). **Each outlet is a location** (U4). Non-food outlets (e.g., spa) are out of the first release. |
| Cafes | [S] | Likely the smallest tenants; probably self-serve onboarding. |
| Shops | [S] | **Rev 0.2:** the first release covers **food shops that sell from a menu** (e.g., bakeries, delis, takeaway counters). General retail inventory (SKUs, stock levels, barcodes, variant matrices) is **out of the first release**. Food-shop specifics such as price per weight: open (U10). |

## 4. Goals

| ID | Goal | Source |
|----|------|--------|
| G1 | Tenants can create and manage digital menus without technical help. | [S] |
| G2 | Customers can open a venue's menu on their phone by scanning a QR code. | [S] |
| G3 | Many tenants share one platform with strict data isolation. | [S]/[D] |
| G4 | AI capabilities via the Claude API. | [S] — first capability: **menu import from photo/PDF, behind human review** (ADR-15, accepted). **Phase 2.** |
| G5 | Orders and financial transactions are handled. | **Out of MVP-1 (Rev 0.2 #7).** Kept as a future goal; PostgreSQL design retained (doc 08). |

### Success metrics
**Not provided.** Candidate metrics for product to choose from [A]: time from signup to first published menu; weekly active tenants; menu scans per location; p95 public menu load time; tenant churn.

## 5. Non-goals

Decided (Rev 0.2) / proposed (Rev 0.3 draft):
- **Ordering and online payments in MVP-1.**
- **General retail inventory** (SKUs, stock levels, barcodes) in the first release.
- **SaaS billing in MVP-1** (U7; static plan limits instead).
- **AI features in MVP-1** (AI menu import is the first phase-2 capability).

Proposed [A], to confirm:
- Point-of-sale (POS) replacement.
- Kitchen display system, unless ordering is confirmed.
- Inventory management.
- Customer loyalty programs.
- Native mobile apps (the customer experience is a mobile web page; no install).

## 6. Scope

### 6.1 Confirmed scope (from brief)
1. Tenant can create and manage menus. [S]
2. Customers access menus through QR codes. [S]
3. Platform supports multiple tenants. [S]
4. Media (images) stored on S3. [S] (implies menu items can have images)
5. AI capabilities through Claude. [S] First capability: menu import from photo/PDF behind human review (ADR-15). **Phase 2.**
6. Multiple locations per tenant. [S] (Rev 0.2 #8)
7. Food businesses first. [S] (Rev 0.2 #9)
8. Markets: US pilot/validation, then UAE commercial launch. [S] (C4, draft 2026-11-21)
9. MVP-1 product defaults (Rev 0.3): item reuse across menus with per-menu price overrides (no location pricing); multilingual content (default + up to 4 languages, manual); weekly menu schedules; sold-out flag applied on publish (real-time overlay in phase 2); no publish approval workflow.

### 6.2 Implied scope — decisions
| Capability | Why it's implied | Status after Rev 0.2 |
|------------|------------------|----------------------|
| Ordering | PostgreSQL "for orders" | **Decided: out of MVP-1.** Channels for a later phase still undefined. |
| Payments / financial transactions | PostgreSQL "financial transactions" | **Decided: out of MVP-1.** PSP and merchant-of-record questions deferred. |
| SaaS subscription billing | Standard for SaaS | **Proposed: deferred** (U7, pending approval). MVP-1 enforces static plan limits from configuration. |
| Multiple locations per tenant | Hotels, chains | **Decided: in the data model** (ADR-13). |
| Multi-language menus | Hotels and tourism | Which languages? Is AI translation wanted? |
| Allergen and dietary information | Legal in many jurisdictions (e.g., EU FIC 1169/2011, UK Natasha's Law for prepacked food) | Which markets? |

### 6.3 Candidate feature backlog (not committed) [A]
Listed so they can be prioritized, not because they are requirements:
- Menu scheduling (breakfast, lunch, dinner; day-of-week).
- Mark an item "sold out" immediately.
- Branding and themes per tenant; custom domains.
- Table-specific QR codes.
- Scan analytics.
- AI (phase 2): **import an existing menu from a photo or PDF (first AI capability, accepted)**; generate item descriptions; translate menus; suggest allergens (with human confirmation).
- Staff roles and permissions.
- Menu version history and rollback.

## 7. Key product principles [R]

1. **The customer menu must load fast on a poor mobile connection.** It is the product's shop window.
2. **Publishing is explicit.** Edits are drafts until published, so customers never see half-finished changes.
3. **AI suggests and a person approves.** Nothing AI-generated (especially allergens or prices) reaches customers without someone accepting it.
4. **Tenant data is isolated by default.** Isolation is enforced at more than one layer.

## 8. Assumptions and constraints

| Type | Item |
|------|------|
| Constraint [S] | Angular, ASP.NET Core, DynamoDB, PostgreSQL, S3, AWS, Terraform, Claude API. |
| Decision (Rev 0.2) | **.NET 10 LTS** (ADR-01). PostgreSQL and Angular versions are verified when preparing the implementation environment (doc 10 §3). |
| Decision (Rev 0.2) | Hybrid DynamoDB + PostgreSQL accepted (ADR-03). |
| Constraint (Rev 0.2) | Primarily a **single-developer learning project**; local-first development, then one AWS account (ADR-16). |
| Proposed (Rev 0.3 draft) | Markets: **US pilot → UAE commercial launch** (C4, draft 2026-11-21). Hosting: `me-central-1` (ADR-17, Proposed; week-1 feasibility check only, region decision before the pilot environment). Development capacity ~20 h/week (U12). |
| Unknown | Budget, launch date, expected scale. |
| Unknown | Compliance targets (GDPR, PCI DSS, SOC 2). |

## 9. Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Ordering/payments creep back into MVP-1 | Roadmap slips, PCI scope | Out of MVP-1 by decision (ADR-12). |
| Two-database design adds work for a single developer | Slower delivery | Accepted (ADR-03); mitigated by local-first setup, pre-specified access patterns and per-module repositories. |
| AI produces incorrect allergen data or prices | Customer harm, legal liability | AI output is always reviewed before it becomes a draft (ADR-08). |
| Pressure to add retail inventory | Data model rework | Out of the first release (ADR-14). |
| Single developer: scope vs. time | Unfinished MVP | Core MVP-1 finishes locally in week 7; cut lines (doc 10 §6). |

## 10. Release plan (Rev 0.3 draft, pending approval)

- **MVP-1 local (weeks 1–8):** tenants with multiple locations, food catalogue, menus, media, publishing, QR codes, static public menu, tenant export/deletion; all protected tests green.
- **MVP-1 in AWS (weeks 9–12):** temporary test deployments in `us-east-1` (deploy, test, tear down; doc 10 §12.6), with Cognito, CI/CD, observability, restore drills, production-readiness review.
- **Market sequence (C4, draft):** US pilot with US restaurants **after week 12**, once the pilot gates are met (doc 10 §12.5) → UAE commercial launch once UAE readiness is done (UAE market pack verified, full multi-language editing UI, UAE legal track, production environment). **Pilot success criteria: proposed in §11 (not approved).**
- **Phase 2 (unscheduled):** AI menu import + Claude API integration (first candidate), multi-account production, real-time sold-out, billing, analytics, branding.
- **Future:** ordering (pay at venue first), online payments.
- **Explicitly not in MVP-1:** ordering, online payments, SaaS billing, retail inventory, AI features.

Details: [10-implementation-roadmap.md](../10-implementation-roadmap.md).

## 11. US pilot success criteria — **PROPOSED, NOT APPROVED** (draft 2026-11-21, revised)

Purpose: decide, with evidence, whether to start preparing the UAE commercial launch. All targets are proposals for the product owner to set.

**Pilot shape (proposed):** 5–10 US food venues, **6–8 weeks**, starting **after week 12** once the pilot gates are met (doc 10 §12.5). With 5–10 venues, every percentage moves in 10–20 % steps, so targets are stated as **counts of venues** where possible, and every venue is also reviewed individually.

### 11.1 Review of the first draft (S1–S14)

| # | Criterion | Measurable? How | Suitable for 5–10 venues? | Verdict |
|---|-----------|-----------------|---------------------------|---------|
| S1 | ≥ 5 venues onboarded in 3 weeks | Yes: tenancy records | It measures sales effort, not the product | **Precondition**, not a success measure |
| S2 | ≥ 80 % publish within 7 days; ≤ 60 min venue effort | 7 days: yes (sign-up and publish timestamps). "Minutes of effort": not reliably (sessions include breaks) | Yes, as a count | **Core** (effort becomes a diagnostic, self-reported) |
| S3 | ≥ 60 % publish every 2 weeks | Yes: publish records | **Unrealistic**: many venues change menus monthly or seasonally | **Revise** into "retained use" |
| S4 | ≥ 2,000 views; scans in ≥ 4 weeks per venue | Yes: CloudFront logs (cached requests are logged too) | Total views depend on venue footfall, not the product | Total = **diagnostic**; "scans in ≥ 4 weeks" = **core** |
| S5 | p75 FCP < 1.5 s | Yes: scheduled synthetic Lighthouse runs | Yes | **Core** (technical health) |
| S6 | p95 publish → visible ≤ 60 s; no stuck jobs | Yes: job timestamps, alarms | Yes (enough publishes for a p95) | **Core** (technical health) |
| S7 | ≥ 99.9 % availability | Yes: 5-minute synthetic checks (about 1 h downtime allowed over 6 weeks) | Yes | **Core** (technical health) |
| S8 | Zero system-caused wrong price/allergen | Yes: incident log with root cause | Yes | **Core** (correctness) |
| S9 | Zero cross-tenant exposure; no open critical/high | Yes: CI, incident log, security review | Yes | **Core** (security) |
| S10 | One restore drill + one full rebuild | Yes: drill records | It's an operational check, not a pilot outcome | **Move to the pilot gates** (G3, doc 04 §5.3) |
| S11 | ≤ 2 support requests per venue per week | Yes, if a support log is kept | Useful, but what counts as a request is subjective | **Diagnostic** |
| S12 | ≥ 70 % would keep using it | Yes: structured interview | Percentages are lumpy; friendly pilots skew positive | **Core**, stated as a count |
| S13 | ≥ 50 % willing to pay | Only with a defined price point; stated intent is unreliable; US willingness to pay says little about UAE pricing | Weak | **Diagnostic** (stronger signal: a signed letter of intent) |
| S14 | ≥ 3 Arabic-speaking reviewers, no blockers | Yes: review sessions | Not a US pilot outcome | **Separate UAE validation track** |

### 11.2 Proposed core success measures (6)

| # | Measure | Proposed target | How it's measured |
|---|---------|-----------------|-------------------|
| CM1 | **Activation** | At least 4 of every 5 onboarded venues publish a complete first menu within 7 days of sign-up, **without engineering help** | Sign-up and publish timestamps; support log marks any engineering intervention |
| CM2 | **Retained use** | At least 2 of every 3 venues still have a live, current menu at the end of the pilot **and** made at least one menu change after initial setup | Publish records; owner confirms the menu is current |
| CM3 | **Real guest use** | Every active venue's menu receives guest scans in at least 4 of the pilot weeks | CloudFront logs aggregated by tenant/location path (IPs truncated, raw logs kept ≤ 30 days) |
| CM4 | **Correctness and security** | Zero system-caused wrong price, allergen or availability incidents; zero cross-tenant exposures; no open critical/high findings | Incident log with root cause; protected tests in CI; security review |
| CM5 | **Technical health** | p95 publish → visible ≤ 60 s; public menu availability ≥ 99.9 %; p75 first contentful paint < 1.5 s on the reference phone over throttled 4G | Job timestamps; synthetic availability checks every 5 minutes; scheduled Lighthouse runs |
| CM6 | **Venue value** | At least 2 of every 3 venue owners say they would keep using it and would recommend it; top missing features captured | Structured end-of-pilot interview (same questions for every venue) |

**Diagnostics** (tracked, no pass/fail): total menu views, support requests per venue per week, self-reported setup effort, willingness to pay at a proposed price (or letters of intent), feature requests.

**Preconditions and gates** (not success measures): ≥ 5 venues onboarded (S1); restore drill and projection rebuild done (gate G3).

**UAE validation track** (separate, before UAE launch preparation): ≥ 3 Arabic-speaking reviewers (e.g., UAE restaurateurs) review Arabic/right-to-left sample menus with no blocking issues.

**Proposed decision rule:** start UAE launch preparation when **CM4 and CM5 are met** and **at least 3 of CM1, CM2, CM3 and CM6** are met. If CM4 fails at any point, the pilot pauses until the cause is fixed.

**What measurement needs (no analytics module, which is phase 2):** CloudFront standard logging in the pilot environment with a small report query (IP truncation, short raw-log retention, mentioned in the privacy notice); a few admin SQL/DynamoDB queries for activation and publishing; synthetic checks (CloudWatch Synthetics or scheduled CI jobs); a simple support log; one interview template.

**Open inputs for the product owner:** final targets, pilot size and length, the price point (if willingness to pay is tracked), and the UAE reviewers.
