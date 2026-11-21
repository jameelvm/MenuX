# 01 — Product Requirements Document (PRD)

Status: **Revision 0.3 — approved baseline (2026-11-21)** · Owner: Product · Changes in 0.3: US launch market, outlets/hierarchy decisions, AI menu import in phase 2, billing deferred, release plan. (0.2: food first, ordering/payments out, multiple locations.)
Tags: [S] stated · [D] derived · [A] assumption, needs confirmation · [R] recommendation (see [README](../README.md))

---

## 1. Summary

Menu Studio is a multi-tenant SaaS platform. Hospitality and retail businesses use it to create, manage and publish digital menus. Their customers open those menus by scanning a QR code. [S]

## 2. Problem statement

> The brief does not state the problem. The text below is an **inferred hypothesis [A]** and must be validated with real customers.

Venues use printed menus that are expensive to reprint, slow to update (prices, sold-out items, seasonal changes) and hard to offer in several languages. Existing digital alternatives (PDF behind a QR code) are hard to read on phones and hard to maintain.

## 3. Target customers (tenants)

**Rev 0.2 decision: food businesses first** (ADR-14). General retail inventory is outside the first release.
**Rev 0.3 decision: launch market is the United States** (ADR-17): USD, `en-US` default, US allergen tag set; legal research items L3–L5 in doc 14 §5.

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

Decided (Rev 0.2 / 0.3):
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
8. Launch market: United States. [S] (Rev 0.3)
9. MVP-1 product defaults (Rev 0.3): item reuse across menus with per-menu price overrides (no location pricing); multilingual content (default + up to 4 languages, manual); weekly menu schedules; sold-out flag applied on publish (real-time overlay in phase 2); no publish approval workflow.

### 6.2 Implied scope — decisions
| Capability | Why it's implied | Status after Rev 0.2 |
|------------|------------------|----------------------|
| Ordering | PostgreSQL "for orders" | **Decided: out of MVP-1.** Channels for a later phase still undefined. |
| Payments / financial transactions | PostgreSQL "financial transactions" | **Decided: out of MVP-1.** PSP and merchant-of-record questions deferred. |
| SaaS subscription billing | Standard for SaaS | **Decided: deferred** (U7). MVP-1 enforces static plan limits from configuration. |
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
| Decision (Rev 0.3) | Launch market: **United States** (ADR-17). Development capacity ~20 h/week (U12). |
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

## 10. Release plan (Rev 0.3 baseline)

- **MVP-1 local (weeks 1–8):** tenants with multiple locations, food catalogue, menus, media, publishing, QR codes, static public menu, tenant export/deletion; all protected tests green.
- **MVP-1 in AWS (weeks 9–12):** one AWS environment, Cognito, CI/CD, observability, restore drills, production-readiness review.
- **Phase 2 (unscheduled):** AI menu import + Claude API integration (first candidate), multi-account production, real-time sold-out, billing, analytics, branding.
- **Future:** ordering (pay at venue first), online payments.
- **Explicitly not in MVP-1:** ordering, online payments, SaaS billing, retail inventory, AI features.

Details: [10-implementation-roadmap.md](../10-implementation-roadmap.md).
