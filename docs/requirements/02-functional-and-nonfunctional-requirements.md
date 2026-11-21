# 02 — Functional and Non-Functional Requirements

Tags: [S] stated · [D] derived · [A] assumption, needs confirmation · [R] recommendation
Priority (MoSCoW) is **proposed** and applies only to items tagged [S]/[D]. Items tagged [A] have no priority until confirmed.
**Revision 0.3 (approved baseline, 2026-11-21):** priorities set from the U3–U7/U11 decisions; unpublish added (FR-MENU-10); AI, billing, analytics and platform console in phase 2; US market NFRs; protected tests (NFR-OPS-07).
Revision 0.2: multiple locations (FR-TEN-04, FR-MENU-05) accepted; food-first catalogue; FR-AI-01 first proposed AI capability; FR-ORD/FR-PAY out of MVP-1; QR entropy and rate limiting (NFR-SEC); environment and version notes.

---

## 1. Functional requirements

### FR-TEN — Tenancy and accounts
| ID | Requirement | Tag | Priority |
|----|-------------|-----|----------|
| FR-TEN-01 | A business can sign up and gets an isolated tenant. | [D] | Must |
| FR-TEN-02 | A tenant can invite staff users. | [D] | Must (email invitations are flex scope; fallback: the owner creates users directly) |
| FR-TEN-03 | Tenant users have roles that limit what they can do. | [D] | Must |
| FR-TEN-04 | A tenant can have several locations (venues/outlets). Hotel outlets are locations; no hierarchy above the tenant (U4). | [S] Rev 0.2 #8, U4 | Must |
| FR-TEN-05 | Platform staff can view and support tenants, with every access logged. | [D] | Phase 2. MVP-1: suspend/reactivate via an admin endpoint only |
| FR-TEN-06 | A tenant can be suspended and deleted, including all its data in every store (enumeration and deletion: doc 07 §7). | [D] (GDPR-style obligation) | Must |

### FR-CAT — Catalogue (items library)
| ID | Requirement | Tag | Priority |
|----|-------------|-----|----------|
| FR-CAT-01 | Create, edit, archive menu items (name, description, price, image). | [S] | Must |
| FR-CAT-02 | Items can have variants or sizes (e.g., small/large). | [S] MVP-1 scope (doc 12 §8) | Must |
| FR-CAT-03 | Items can have modifier groups (e.g., "choose a sauce", min/max choices, price deltas). | [S] MVP-1 scope | Must |
| FR-CAT-04 | Items carry allergen and dietary tags. Default tag set: FDA's nine major food allergens (US launch). | [S] MVP-1 scope; legal review L3 | Must |
| FR-CAT-05 | One item can be reused across several menus, with per-menu price or visibility overrides. **Location-specific prices: phase 2.** | [S] U3 | Must |
| FR-CAT-06 | Item content in several languages: multilingual data model; default language plus up to 4 more, entered manually; no AI translation in MVP-1. | [S] U5 | Must (data model); additional-language UI is flex scope |
| FR-CAT-07 | Prices in the tenant's currency (USD default); one currency per tenant. | [D] US market | Must |
| FR-CAT-08 | Optional calorie value per item/variant. | [A] pending L3 (US menu labeling for chains with 20+ locations) | Could (data field only) |

Rev 0.2 (ADR-14): the catalogue targets **food and beverage menus**. General retail inventory (SKUs, stock levels, barcodes, variant matrices) is out of the first release.

### FR-MENU — Menu authoring
| ID | Requirement | Tag | Priority |
|----|-------------|-----|----------|
| FR-MENU-01 | Create menus with ordered sections and items. | [S] | Must |
| FR-MENU-02 | Reorder sections and items by drag-and-drop. | [D] | Must |
| FR-MENU-03 | Edits are saved as a draft; publishing makes them visible to customers. | [R] | Must |
| FR-MENU-04 | Keep published versions (last 50); roll back to the full published state of a previous version (content, schedule, locations). | [R] | Must (API); rollback UI is flex scope |
| FR-MENU-05 | Assign menus to locations. Assignment takes effect when the menu is next published. | [D] from Rev 0.2 #8 | Must |
| FR-MENU-06 | Schedule menus with weekly time windows in the location's timezone. | [S] U6 | Must |
| FR-MENU-07 | Mark an item unavailable ("86'd") immediately, without a full republish. | [S] U6 | **Phase 2.** MVP-1: `soldOut` flag on the placement, visible after publish (< 60 s) |
| FR-MENU-08 | Preview the customer view before publishing. | [D] | Should |
| FR-MENU-09 | Two editors cannot silently overwrite each other's changes. | [D] | Must |
| FR-MENU-10 | Take a live menu offline (unpublish); required before archiving or deleting a live menu. Only one publish/rollback/unpublish per menu at a time. | [D] ADR-10 | Must |
| FR-MENU-11 | No publish approval workflow; publishing is controlled by the `menu.publish` permission. | [S] U11 | Must |

### FR-MEDIA — Media
| ID | Requirement | Tag | Priority |
|----|-------------|-----|----------|
| FR-MEDIA-01 | Upload images for items and menus to S3. | [S] | Must |
| FR-MEDIA-02 | Images are resized and optimized automatically for mobile. | [D] | Must |
| FR-MEDIA-03 | Upload limits by size and type; content validation. | [D] | Must |
| FR-MEDIA-04 | Per-tenant storage quota from static plan limits. | [S] U7 | Should |

### FR-QR — QR access and the public menu
| ID | Requirement | Tag | Priority |
|----|-------------|-----|----------|
| FR-QR-01 | Generate a QR code per location/menu that opens the public menu. Codes carry at least 96 random bits (NFR-SEC-01). | [S] | Must |
| FR-QR-02 | QR codes point to a **stable, short URL that can be redirected**, so printed codes never need reprinting when menus change. | [R] | Must |
| FR-QR-03 | Download QR codes as print-ready PNG/SVG/PDF. | [D] | Must |
| FR-QR-04 | QR per table (table number passed to menu/order). | [A] | Future (with ordering) |
| FR-QR-05 | The public menu works without login or app install. | [D] | Must |
| FR-QR-06 | The public menu shows the menus active now (venue timezone), with an "all menus" fallback. | [S] U6 | Must |
| FR-QR-07 | Customer can switch menu language when more than one is published. | [S] U5 | Must |
| FR-QR-08 | Customer can filter by allergen or diet (allergens are always displayed). | [A] | Should (flex scope) |
| FR-QR-09 | Tenant branding (logo, colors) on the public menu. | [A] | Phase 2 (U8) |
| FR-QR-10 | Revoke or disable a QR code. | [D] | Should |

### FR-AI — AI capabilities (Claude)
The brief says "Claude API for AI capabilities" but doesn't name any capability. Rev 0.3: **FR-AI-01 is the accepted first capability (ADR-15), behind human review, scheduled for phase 2.** No AI features in MVP-1. The others remain candidates [A].
| ID | Candidate | Value hypothesis | Risk |
|----|-----------|------------------|------|
| FR-AI-01 | **(First AI capability — accepted, phase 2)** Import a menu from a photo or PDF into suggested sections and items, reviewed before becoming drafts | Fast onboarding | Wrong prices; human review required |
| FR-AI-02 | Generate or improve item descriptions | Content quality | Low |
| FR-AI-03 | Translate menu content | Hotels, tourists | Medium; review recommended |
| FR-AI-04 | Suggest allergens from ingredients | Saves effort | **High (health)**; never publish without confirmation |
| FR-AI-05 | Customer-facing menu Q&A ("is this vegan?") | Customer experience | **High**; can hallucinate. Not recommended for v1 |

Cross-cutting AI requirements [R]: AI output is always a **suggestion stored as a draft**; per-tenant usage quotas; record every AI call (tenant, user, feature, token usage) for cost attribution; customer menu data is sent to Anthropic → disclose in terms/DPA.

### FR-ORD — Ordering — **out of MVP-1 (Rev 0.2 #7)**; kept as future candidates [A]
| ID | Candidate |
|----|-----------|
| FR-ORD-01 | Customer builds a cart from the public menu and submits an order. |
| FR-ORD-02 | Order types: dine-in (table), takeaway, delivery, room service. |
| FR-ORD-03 | Staff see incoming orders in real time and change status. |
| FR-ORD-04 | Customer sees order status. |
| FR-ORD-05 | Prices, taxes and service charges calculated server-side, never trusted from the client. |
| FR-ORD-06 | Order records the menu item snapshot (name, price, modifiers) at the time of order. |

### FR-PAY — Online payments — **out of MVP-1 (Rev 0.2 #7)**; kept as future candidates [A]
| ID | Candidate |
|----|-----------|
| FR-PAY-01 | Customer pays online through a hosted PSP checkout (card data never touches our servers). |
| FR-PAY-02 | Refunds (full/partial). |
| FR-PAY-03 | Tenant payouts — via PSP marketplace (e.g., Stripe Connect) or tenant's own merchant account? |
| FR-PAY-04 | Ledger of all money movements, reconcilable with PSP reports. |

### FR-BILL — SaaS billing — **deferred beyond MVP-1** (U7). MVP-1 enforces static plan limits (`tenants.plan_key` + configuration).
| ID | Candidate |
|----|-----------|
| FR-BILL-01 | Tenants subscribe to a plan; plan determines limits (locations, menus, AI credits, storage). |
| FR-BILL-02 | Trial period; dunning on failed payment; downgrade/suspend. |

### FR-ANL — Analytics — phase 2 [A]
| ID | Candidate |
|----|-----------|
| FR-ANL-01 | Scan counts per QR / location / day. |
| FR-ANL-02 | Most-viewed items. |

### FR-OPS — Platform administration [D] — MVP-1: suspend/reactivate and plan assignment via admin endpoints; console and support access in phase 2
| ID | Requirement |
|----|-------------|
| FR-OPS-01 | Platform admin console: tenant list, status, plan, usage. |
| FR-OPS-02 | Support "view as tenant" (read-only by default), time-limited and recorded in an audit log. |
| FR-OPS-03 | Feature flags per tenant. |

---

## 2. Non-functional requirements

> **No numeric targets were provided.** The values below are **proposed starting targets [R]**. Confirm them, or replace them with business-driven numbers.

### 2.1 Performance
| ID | Requirement | Proposed target |
|----|-------------|-----------------|
| NFR-PERF-01 | Public menu first contentful paint on 4G mid-range phone | < 1.5 s p75 |
| NFR-PERF-02 | Public menu payload (excluding images) | < 150 KB gzipped JS+JSON for initial view |
| NFR-PERF-03 | QR resolution file (`/data/qr/{code}.json`) served from the CDN | < 100 ms p95 at edge, cache hit |
| NFR-PERF-04 | Admin API read latency | < 300 ms p95 |
| NFR-PERF-05 | Publish → visible to customers | < 60 s |
| NFR-PERF-06 | Images served in modern formats (WebP/AVIF) with responsive sizes | — |

### 2.2 Scalability
| ID | Requirement |
|----|-------------|
| NFR-SCAL-01 | Public reads scale independently of the admin API (CDN-served). |
| NFR-SCAL-02 | Handle meal-time spikes (lunch/dinner peaks, many scans at once) with no pre-provisioning. |
| NFR-SCAL-03 | **Scale numbers unknown** — need: tenants at launch / 12 months, menus per tenant, items per menu, scans per day. |

### 2.3 Availability and resilience
| ID | Requirement | Proposed target |
|----|-------------|-----------------|
| NFR-AV-01 | Public menu availability | 99.95 % (fully static: CDN + S3 only; survives API and database outages) |
| NFR-AV-02 | Admin portal / API availability | 99.9 % |
| NFR-AV-03 | Ordering availability (if in scope) | 99.9 %; graceful degradation to "order at counter" |
| NFR-AV-04 | RPO / RTO — PostgreSQL | RPO ≤ 5 min (PITR), RTO ≤ 1 h |
| NFR-AV-05 | RPO / RTO — DynamoDB | PITR enabled; RTO ≤ 2 h |
| NFR-AV-06 | Single region, multi-AZ at launch; multi-region not required unless stated. | |

### 2.4 Security
See [09-security-and-multitenancy.md](../architecture/09-security-and-multitenancy.md). Summary: tenant isolation enforced at several layers; OWASP ASVS L2 as baseline; encryption in transit and at rest; MFA for tenant admins; no card data on our infrastructure.

| ID | Requirement | Tag |
|----|-------------|-----|
| NFR-SEC-01 | QR identifiers contain **at least 96 random bits** from a cryptographically secure generator (design: 128 bits, doc 07 §3.3.1). | [S] Rev 0.2 #5 |
| NFR-SEC-02 | Public paths and the admin API are rate-limited, and QR enumeration attempts are detected (doc 09 §4.6). | [S] Rev 0.2 #5 |
| NFR-SEC-03 | All of a tenant's data can be enumerated for export and deletion in every store (doc 07 §7). | [D] |

### 2.5 Privacy and compliance
| ID | Requirement | Tag |
|----|-------------|-----|
| NFR-PRIV-01 | Personal data inventory; collect as little as possible (public menu collects no personal data). | [D] |
| NFR-PRIV-02 | Data residency: data stored in a US AWS region (US launch, ADR-17); US state privacy laws reviewed (L5). | [S] Rev 0.3 |
| NFR-PRIV-03 | Tenant data export and deletion. | [D] |
| NFR-PRIV-04 | PCI DSS scope kept to SAQ A (hosted payment pages) if payments are in scope. | [R] |
| NFR-PRIV-05 | Sub-processor disclosure (AWS, Anthropic, PSP, email provider). | [D] |

### 2.6 Accessibility and usability
| ID | Requirement |
|----|-------------|
| NFR-ACC-01 | Public menu meets WCAG 2.2 AA. For the US launch, ADA Title III expectations are reviewed (L4). |
| NFR-ACC-02 | Admin portal WCAG 2.2 AA target. |
| NFR-ACC-03 | Public menu supports right-to-left (RTL) languages if Arabic/Hebrew markets are targeted. [A] |

### 2.7 Maintainability and operability
| ID | Requirement |
|----|-------------|
| NFR-OPS-01 | All infrastructure in Terraform; no manual console changes in staging/prod. [S] |
| NFR-OPS-02 | Separate AWS accounts for dev / staging / prod. [R] **Target production setup for phase 2.** The 12-week roadmap develops locally, then uses one AWS account (Rev 0.2 #11, doc 10). |
| NFR-OPS-03 | Structured logs, metrics and traces via OpenTelemetry; every log line carries `tenantId` and `correlationId`. |
| NFR-OPS-04 | Zero-downtime deployments; database migrations backward compatible (expand/contract). |
| NFR-OPS-05 | Module boundaries enforced by automated architecture tests. |
| NFR-OPS-07 | The **protected test suite** (consistency, tenant isolation, recovery; doc 10 §12.2) runs as required CI checks and is never reduced to fit the schedule. |
| NFR-OPS-06 | Supported runtime versions only: **.NET 10 LTS**; Angular active/LTS and the PostgreSQL major are **verified when preparing the implementation environment** (doc 10 §3). |

### 2.8 Cost
| ID | Requirement |
|----|-------------|
| NFR-COST-01 | Cost per tenant measurable (tags, AI usage per tenant). |
| NFR-COST-02 | Small tenants cost very little: serverless/on-demand pricing where possible. |
| NFR-COST-03 | AI spend capped per tenant per plan. |
