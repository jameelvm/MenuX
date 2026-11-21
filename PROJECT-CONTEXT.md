# Menu Studio --- Project Context & Handover

*Last updated: 2026-11-19*

This file is the handover note for continuing Menu Studio work in a new
ChatGPT or Claude session. Keep it in the project repository and update
it after major decisions or milestones.

## 1. Project overview

**Menu Studio** is a multi-tenant SaaS product for food businesses
(initially restaurants, cafes, hotel food-and-beverage outlets, and food
shops that sell from a menu) to create, manage, and publish digital
menus.

Core user flow:

1.  A business signs up and creates one or more locations.
2.  Users manage a food catalog, modifiers, allergens/dietary tags, and
    media.
3.  Users create menus with sections and item placements.
4.  A menu is published.
5.  Customers scan a QR code and view the public menu from a static,
    CDN-backed experience.

**MVP-1 excludes ordering and online payments.** General retail
inventory is also out of the first release.

## 2. User / delivery context

-   Primarily a **single-developer** project and a practical learning
    project.
-   Delivery strategy: local-first, then one AWS account, then
    multi-account production in Phase 2.
-   The roadmap is sized roughly for full-time focus; actual weekly
    availability is still unknown (U12).
-   The user wants to understand the architecture and implementation,
    not simply receive a large code dump.
-   Explain technical concepts step by step and use practical C#/.NET
    examples where useful.

## 3. Accepted architecture and technology direction

### Application architecture

-   Modular monolith.
-   One solution with API and worker hosts.
-   Enforce module boundaries with automated architecture tests in CI.
-   Modules communicate through contracts and an outbox rather than
    directly reaching into each other's internals.

### Technology choices

-   Backend: .NET 10 / ASP.NET Core 10 / EF Core 10.
-   Admin and public frontend: Angular.
-   Hybrid persistence:
    -   DynamoDB: Catalog, Menu Authoring, Publishing metadata, Media
        metadata, AI jobs/suggestions.
    -   PostgreSQL: tenancy, identity, billing domain when introduced,
        AI usage ledger, audit, and deletion tracking; later
        ordering/payments if added.
-   S3 for media and published artifacts/snapshots.
-   CloudFront for public delivery.
-   Terraform for AWS infrastructure.
-   Local-first development with Docker Compose (or optional .NET
    Aspire).

PostgreSQL and Angular versions are deliberately not pinned in the
documents; verify supported versions in Week 1 and record them in
`docs/architecture/versions.md`.

## 4. Important architecture decisions (from Rev 0.2)

ADR statuses below reflect
`docs/11-architecture-review-and-open-questions.md` in the latest
uploaded ZIP. Do not silently change statuses.

  -----------------------------------------------------------------------
  ADR                     Decision                Status
  ----------------------- ----------------------- -----------------------
  ADR-01                  .NET 10 LTS             Accepted

  ADR-02                  Modular monolith        Accepted

  ADR-03                  Hybrid DynamoDB +       Accepted
                          PostgreSQL              

  ADR-04                  Public customers read   Proposed
                          static projections, not 
                          databases               

  ADR-05                  Static Angular shell +  Proposed
                          client-side CDN data;   
                          no SSR server           

  ADR-06                  ECS Fargate for         Proposed
                          API/worker; Cognito in  
                          AWS, Keycloak locally   

  ADR-07                  Catalog and Menu        Accepted
                          Authoring are separate  
                          modules                 

  ADR-08                  AI output is a          Proposed
                          suggestion requiring    
                          human approval          

  ADR-09                  Tenant-wide             Proposed
                          enumeration/deletion    
                          via TenantIndex GSI     

  ADR-10                  One authoritative       Proposed
                          publication pointer +   
                          publish state machine   

  ADR-11                  QR security requirement Requirement accepted;
                          (at least 96 random     design proposed
                          bits) accepted; 128-bit 
                          design proposed         

  ADR-12                  Ordering and online     Accepted
                          payments excluded from  
                          MVP-1                   

  ADR-13                  Multiple locations per  Accepted
                          tenant                  

  ADR-14                  Food businesses first;  Accepted
                          general retail          
                          inventory excluded      

  ADR-15                  First AI capability:    Proposed; product
                          menu import from        confirmation pending
                          photo/PDF               

  ADR-16                  Single-developer,       Accepted
                          local-first delivery;   
                          one AWS account before  
                          multi-account           
  -----------------------------------------------------------------------

### Public menu delivery

-   Public path is static: CloudFront serves app shell, QR/route files,
    location manifests, and content-addressed menu snapshots from S3.
-   MVP-1 has no public API endpoint for menu reads.
-   Angular app is a prerendered shell plus client-side rendering of CDN
    JSON.
-   If performance budget fails, evaluate publish-time HTML rendering
    before dynamic SSR.

### TenantIndex design (ADR-09)

Every tenant-owned DynamoDB item in every module table carries:

-   `TENANT_PK = T#{tenantId}`
-   `TENANT_SK = {PK}|{SK}`

A keys-only GSI named `TenantIndex` on each table uses those attributes
to enumerate a tenant's items. The index is intended for tenant
export/deletion and other tenant-wide administration.

Deletion design described in Rev 0.2: - Delete public QR/route
projections first. - Purge DynamoDB records in resumable batches. - Run
a verification pass. - Remove S3 objects, including object versions. -
Track progress per module in PostgreSQL. - Run a monthly audit for
leftovers.

Important implementation checks: - Every create and update path must
maintain `TENANT_PK` and `TENANT_SK`. - GSI reads are eventually
consistent; deletion must be resumable and verified. - Handle DynamoDB
pagination, `UnprocessedItems`, retries, and worker crashes. - Ensure
the `{PK}|{SK}` construction cannot collide (IDs/keys must not contain
the delimiter, or use an unambiguous encoding). - `KEYS_ONLY` projection
is intended to provide base-table keys needed to delete the original
items. - Confirm the exact key attribute names and index definitions
against the latest `docs/database/07-dynamodb-access-patterns.md` and
Terraform/table definitions before coding.

### Publishing consistency (ADR-10)

-   The Publishing table item `T#{tenantId}#M#{menuId} / CURRENT` is the
    single source of truth for what is live.
-   Location index items `T#{tenantId}#L#{locationId} / PUB#{menuId}`
    are written in the same transaction.
-   Location manifests, menu header display information, and CDN caches
    are projections/rebuildable copies.
-   Publish job states include `REQUESTED`, `BUILDING`, `COMMITTED`,
    `LIVE`, plus `SUPERSEDED` and `FAILED`.
-   Design includes leases, idempotent content-addressed snapshot
    writes, commit transaction, FIFO-serialized manifest rebuilds, and a
    reconciler.
-   Snapshot build checks draft version before and after multi-page
    reads to detect inconsistent snapshots.

### QR security (ADR-11)

-   128-bit cryptographically random QR identifiers, base64url encoded
    (22 characters).
-   Never derive identifiers from business IDs, make them sequential, or
    reuse them.
-   WAF per-IP rules, CDN-log-based QR miss-rate detection, negative
    caching, and ASP.NET admin API rate limiting are described.

## 5. MVP-1 scope

Included: - Tenant registration and tenancy. - Multiple locations per
tenant. - User memberships, roles, permissions. - Food catalog and
modifier groups. - Menu authoring: menus, sections, placements,
ordering/reordering. - Media uploads and processing. - Menu
publishing. - QR codes and route/manifest projections. - Static public
menu experience. - Tenant export and deletion. - Security,
tenant-isolation tests, publishing failure tests, and basic
observability.

Excluded or deferred: - Ordering and online payments. - SaaS billing
(roadmap defers beyond Week 12; user confirmation still needed). -
General retail inventory. - Multi-account production AWS setup. -
POS/PMS/delivery integrations. - Custom domains and advanced branding. -
Advanced analytics and support operations. - AI menu import is proposed
and gated on product confirmation, not yet approved.

## 6. Latest Claude documentation ZIP

Latest uploaded archive: `docs(1).zip`, containing Rev 0.2
documentation.

Important files: - `docs/README.md` -
`docs/requirements/01-product-requirements.md` -
`docs/requirements/02-functional-and-nonfunctional-requirements.md` -
`docs/requirements/03-user-roles-and-journeys.md` -
`docs/architecture/04-high-level-architecture.md` -
`docs/architecture/05-domain-boundaries-modular-monolith.md` -
`docs/architecture/09-security-and-multitenancy.md` -
`docs/database/06-hybrid-database-architecture.md` -
`docs/database/07-dynamodb-access-patterns.md` -
`docs/database/08-postgresql-erd.md` -
`docs/10-implementation-roadmap.md` -
`docs/11-architecture-review-and-open-questions.md`

Rev 0.2's review summary says the previous review items were addressed:
hybrid DB accepted, tenant-wide enumeration/deletion, authoritative
publication pointer and recovery, static public app, 128-bit QR
identifiers, .NET 10 LTS, MVP exclusions, multiple locations, food
businesses first, proposed AI menu import, and a single-developer
local-first roadmap.

## 7. Open decisions requiring the user's input

### Proposed ADRs awaiting sign-off

-   ADR-04: static public read path.
-   ADR-05: static Angular hosting.
-   ADR-06: ECS Fargate and Cognito (Keycloak locally).
-   ADR-08: AI suggestions require human approval.
-   ADR-09: TenantIndex.
-   ADR-10: authoritative publication pointer and publish state machine.
-   ADR-11: QR identifier/rate-limiting design.
-   ADR-15: confirm AI menu import as the first AI capability.

### Product/business questions in doc 11 Part C

  -----------------------------------------------------------------------------
  ID                      Question                     Needed by / impact
  ----------------------- ---------------------------- ------------------------
  U1                      Confirm AI menu import as    Before Weeks 10--11
                          first AI capability          

  U2                      Launch country/region, data  Before AWS setup
                          residency, AWS region,       
                          languages, currency,         
                          allergen/accessibility law   

  U3                      Item reuse across menus;     Affects placement model;
                          per-location price overrides Week 3

  U4                      Hotel outlets as locations?  Affects tenancy model;
                          Chains/franchises/agencies   Week 2
                          above tenant?                

  U5                      Languages per menu; AI       Affects translation
                          translation?                 storage; Week 3

  U6                      Menu schedules and instant   Affects
                          sold-out in MVP-1?           menu/availability model;
                                                       Week 4

  U7                      SaaS billing timing; roadmap Phase-2 plan
                          defers beyond Week 12        

  U8                      Branding depth/custom        Phase 2
                          domains                      

  U9                      Enterprise SSO needed in     Could affect Cognito
                          year one?                    decision

  U10                     Food-shop specifics: price   Affects catalog fields
                          by weight, prepacked         
                          labelling                    

  U11                     Publish approval workflow?   Affects
                                                       permissions/publishing

  U12                     Developer hours per week and Affects roadmap
                          learning goals               estimates

  U13                     Tenant deletion grace period Affects deletion/audit
                          and retention periods        design
  -----------------------------------------------------------------------------

Other open technical decisions are listed in Part C.3 of
`docs/11-architecture-review-and-open-questions.md` (T1--T8), including
LeadingKeys timing, BFF vs SPA tokens, Aspire, search, publish-time
HTML, snapshot retention, max locations per publish, and version
verification.

## 8. Roadmap from Rev 0.2

-   Weeks 1--7: local implementation.
    -   Week 1: versions, repo, .NET solution, architecture tests,
        Docker Compose, telemetry, CI, Angular workspace.
    -   Week 2: OIDC, tenancy, multiple locations, roles/permissions,
        PostgreSQL RLS, cross-tenant tests.
    -   Week 3: DynamoDB tables with TenantIndex, catalog, modifiers,
        media.
    -   Week 4: menu authoring, sections, placements, overrides,
        reorder.
    -   Week 5: publishing state machine, consistent snapshot,
        authoritative pointer, manifest builder, reconciler,
        failure-injection tests.
    -   Week 6: QR, route projections, static public app,
        performance/accessibility checks.
    -   Week 7: tenant export/deletion, security review, refactoring;
        local MVP-1 complete.
-   Weeks 8--12: one AWS account, Terraform, ECS, Cognito, deployment,
    WAF/alarms, then gated AI menu import and hardening.
-   Multi-account production is Phase 2.
-   Roadmap assumes roughly full-time effort; stretch goals should be
    cut first.
-   Never cut: tenant isolation tests, publishing failure handling,
    tenant deletion, and AWS budget alarms.

## 9. Current checkpoint --- where the conversation stopped

1.  The latest Claude Rev 0.2 ZIP was shared and reviewed.
2.  The review found the direction substantially improved, especially
    TenantIndex, publishing consistency, QR security, static public
    delivery, and single-developer roadmap.
3.  A next-step prompt was prepared asking Claude to perform a final
    architecture sign-off and implementation-readiness review, without
    writing application code.
4.  **That final sign-off has not yet been completed or approved by the
    user.**
5.  The immediate next action is to give Claude the final sign-off
    prompt, get the updated checklist/decision summary, then review the
    result before approving ADRs or starting implementation.

## 10. How to resume in a new ChatGPT session

Upload: 1. This `PROJECT-CONTEXT.md`. 2. The latest project
documentation ZIP (currently Rev 0.2, `docs(1).zip`). 3. Any newer ZIP
or source code if the project has progressed.

Then send:

> We are continuing the Menu Studio project. Read PROJECT-CONTEXT.md and
> the latest project documentation ZIP. First summarize the current
> state, accepted decisions, proposed decisions, and exact next action.
> Do not restart completed architecture discussions. Do not assume
> proposed ADRs are approved. Identify conflicts between this context
> file and the latest docs and ask before resolving them. Continue from
> the current checkpoint.

## 11. Maintenance rules

-   Update this file after each major architecture decision or
    implementation milestone.
-   Never mark a proposed ADR as accepted without explicit user
    approval.
-   Keep the latest ZIP/docs and this handover file aligned.
-   Record why a decision was made, not only the decision itself.
-   When implementation starts, add current branch, commit, completed
    tasks, failing tests, and next coding task.
-   If documentation conflicts with code, record the discrepancy and ask
    which is authoritative before changing either.
