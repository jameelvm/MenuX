# Versions record (week 1)

Checked **2026-11-21** for doc 10 §3 and doc 14 §6. This file records facts and the versions pinned in the repository. It approves nothing: C1 (other P-items) and C4 remain open, and ADR-17 remains **Proposed** (region decision due before the pilot environment). C5 was approved on 2026-11-21 separately (doc 13 §1d).

Tags: [S] stated · [D] derived · [A] assumption · [R] recommendation.

## 1. Pinned in the repository

| Component | Pinned | Where | Source checked | Notes |
|-----------|--------|-------|----------------|-------|
| .NET SDK | **10.0.401** (`rollForward: latestPatch`) | `src/backend/global.json` | .NET 10 release metadata: latest runtime 10.0.15, latest SDK **10.0.404** (2026-11-10); .NET 10 is LTS, supported to 2028-11-14 | The SDK installed on this machine is 10.0.401. [R] Update the local SDK to 10.0.404 (security patch) and raise the pin; CI installs the pinned version. |
| Test runner | Microsoft Testing Platform | `src/backend/global.json` (`test.runner`) | xUnit v3 4.x no longer supports the VSTest target on the .NET 10 SDK | `dotnet test --solution src/backend/MenuX.slnx` |
| xUnit | xunit.v3 **4.0.3** | `Directory.Packages.props` | NuGet | Test projects only |
| Architecture tests | TngTech.ArchUnitNET.xUnitV3 **0.13.5** (Apache-2.0) | `Directory.Packages.props` | NuGet | Doc 05 §2 names NetArchTest or ArchUnitNET; NetArchTest.Rules has had no release since 1.3.2 |
| PostgreSQL (Docker) | **postgres:18.6-alpine** | `compose.yaml` | RDS for PostgreSQL release calendar: latest major **18** (18.1 GA 2025-11-14; latest minor listed **18.6**, 2026-08-25). Community PostgreSQL 19.0 is out, but RDS is not offering 19 yet (RDS adds a major within 30 days of x.1) | Same major as RDS, as doc 10 §3 requires. Npgsql 10.0.4 targets net10.0 and supports PostgreSQL 18. **RDS PostgreSQL 18 availability in `us-east-1` (the test region): pending AWS**, checked with `aws rds describe-db-engine-versions` in the final AWS phase (doc 10 §12.7, A3). |
| DynamoDB Local | **amazon/dynamodb-local:3.3.2** | `compose.yaml` | Docker Hub | Verified locally: conditional `TransactWriteItems` (a repeated write is cancelled with `ConditionalCheckFailed`) and a keys-only GSI query |
| LocalStack (S3) | **localstack/localstack:4.14.0** | `compose.yaml` | LocalStack blog: since **2026-03-23** the LocalStack image requires an auth token (free "Hobby" tier needs an account; CI credits not included) | Verified locally: 4.14.0 starts **without** a token; S3 bucket versioning works; `PUT` with `If-None-Match: *` returns 200, then **412** on repeat. Pinned images get no backported fixes. **Decision needed** (§4). |
| Mailpit | **axllent/mailpit:v1.34.0** | `compose.yaml` | Docker Hub | Verified locally: SMTP message received |
| Angular | **22.3.0** (`@angular/*` ^22.3.0, exact versions in `package-lock.json`) | `src/frontend/package.json` | npm `@angular/cli`; angular.dev releases: active support to 2027-06-30, LTS to 2028-06-30. Angular 23 targeted for about June 2027 (12-month cycle) | Workspace `menux` with `admin` (prefix `adm`) and `public` (prefix `pub`); TypeScript ~6.0, Vitest. Verified: `npm ci` (0 vulnerabilities), both builds and unit tests pass |
| Node.js | **24.22.3** (24 "Krypton" LTS, 2026-11-10) | `src/frontend/.nvmrc` (CI reads it) | nodejs.org release index; Angular CLI 22.3 requires Node `^22.22.3`, `^24.15.0` or `>=26` | Installed locally 2026-11-21 with `winget install OpenJS.NodeJS.24 --version 24.22.3` (winget's `OpenJS.NodeJS.LTS` now installs Node 26) |

### 1.1 LocalStack 4.14.0: temporary test dependency, with known limitations

**Status (product owner, 2026-11-21):** keep `localstack/localstack:4.14.0` **for now**, as a **temporary** dependency for local development and CI tests only. Switching the image or adding a LocalStack token needs the product owner's approval.

**Maintenance limitations:**
- 4.14.0 is the last SemVer release. Images from 2026-03-23 onward use calendar versions (`2026.x`) and need an auth token.
- The Community GitHub repository was archived (read-only) on 2026-03-23. LocalStack says Community gets no further updates; it has also said it can't commit to regular Community updates.
- So the pinned image is **frozen**: no bug fixes, and no emulation of S3 or other AWS API behaviour that AWS adds or changes after early 2026.

**Security limitations:**
- No security patches for LocalStack itself or its bundled OS and Python packages. Vulnerabilities found later will stay.
- **Exposure stays low only while these hold:**
  - it runs locally and in CI;
  - its port is bound to `127.0.0.1` in `compose.yaml`;
  - it holds only test data (no real tenant data, per W2);
  - it uses dummy credentials;
  - it is never deployed or reachable from a network.
- Don't expose its port, add real credentials or keep real data in it.

**What it has been verified for (2026-11-21):** S3 bucket versioning, and conditional `PUT` with `If-None-Match: *` (200, then 412 on repeat).

**Reassess before depending on unsupported or newer AWS API behaviour [R]:**
1. Before any test relies on an S3 or AWS feature beyond the verified list above. If it isn't emulated correctly, the test could pass locally and fail on AWS.
2. When adding any further AWS service to the local stack. Today it runs S3 only. **Triggered by ADR-18 (accepted 2026-11-21):** SQS will be added to this same image in **week 2** (product owner's choice: no image switch, no token). At that point verify DLQ redrive, visibility timeout, `ChangeMessageVisibility` and long polling before relying on them.
3. At the latest before the first AWS environment (week 9), when integration tests can also run against real AWS.

**Options at reassessment:**
- a LocalStack Hobby account and token (the token becomes a CI secret; the free plan has no CI credits);
- a maintained S3-compatible emulator, checked for conditional writes and versioning first;
- running the relevant integration tests against a real AWS test bucket.

## 2. Checked, not yet used (added when first needed)

| Component | Version | Source | Notes |
|-----------|---------|--------|-------|
| EF Core | **10.0.15** | NuGet | EF Core **11.0.0** is out but targets **net11.0** only, so it can't be used on .NET 10 LTS. Stay on 10.0.x. |
| Npgsql EF Core provider | **10.0.4** (net10.0) | NuGet | Matches EF Core 10 |
| AWS SDK for .NET | v4: AWSSDK.DynamoDBv2 **4.1.0.3**, AWSSDK.S3 **4.1.2**, AWSSDK.SecurityToken **4.1.1.1** | NuGet | Current major is v4 |
| Keycloak | **26.8.2** | Docker Hub `keycloak/keycloak` | **Pinned in `compose.yaml` and the BFF tests (week 2, ADR-06 accepted)**; local only, never deployed |
| Terraform + AWS provider | Terraform **1.17.2** (installed locally 2026-11-21; HashiCorp binary, GPG signature and SHA-256 verified); `hashicorp/aws` **6.73.0** (latest, 2026-11-11) | releases.hashicorp.com; registry.terraform.io | Nothing pinned in the repository or CI yet. Pin `required_version = "~> 1.17.0"` and provider `~> 6.73` with a committed `.terraform.lock.hcl` when the configuration is prepared; S3 native state locking (`use_lockfile`). Checked offline only (`fmt`, `validate`); **no AWS calls until the final AWS phase** |
| Anthropic SDK for C# | — | — | Phase 2 |

## 3. `me-central-1` feasibility check (public AWS sources only)

Scope: feasibility only, as input to the ADR-17 region decision before the pilot environment. No AWS account was used, the region wasn't enabled, and no account changes were made.

### 3.1 Service availability (public regional services table, snapshot dated 2025-11-13)

| Required service | Listed for `me-central-1` | Notes |
|------------------|---------------------------|-------|
| ECS on Fargate | Yes (ECS listed; AWS Fargate Regions page lists `me-central-1` for Linux containers) | |
| RDS for PostgreSQL | Yes | **Engine versions per region not checked**: needs `aws rds describe-db-engine-versions` with an enabled region |
| DynamoDB | Yes | Feature-level parity (transactions, GSIs, PITR) not checked per region |
| S3 | Yes | |
| Cognito | Yes | |
| Secrets Manager | Yes | |
| KMS, ECR, CloudWatch/Logs, X-Ray, SES, WAF, ACM, IAM Identity Center | Yes | |
| CloudFront | Global service; any regional origin is supported | Certificate and WAF stay in `us-east-1` (AWS requirement) |

### 3.2 Region health: blocker (evidence for the product owner; re-checked 2026-11-21)

**Primary source:** the AWS Health Dashboard public feed (`https://health.aws.amazon.com/public/currentevents`, which backs `https://health.aws.amazon.com/health/status`), read 2026-11-21.

- **Event:** "Region Availability", region **UAE (me-central-1)**.
  - Event ARN: `arn:aws:health:me-central-1::event/MULTIPLE_SERVICES/AWS_MULTIPLE_SERVICES_OPERATIONAL_ISSUE/AWS_MULTIPLE_SERVICES_OPERATIONAL_ISSUE_5E6B8_EF2498889B5`.
  - Started **2026-03-01 12:51 UTC**. The event is **still open**, with 24 updates in total.
- **Current status level:** **3, "service disruption"**, the highest level. It applies to 140 listed services, including every service the plan needs: ECS, Fargate, RDS, DynamoDB, S3, Cognito, Secrets Manager, EC2 and IAM Identity Center.

**Current status (latest AWS statement, 2026-09-15 10:27 UTC):**
> "After a thorough assessment, we have determined that we are unable to restore access to the resources and data hosted exclusively in the mec1-az2 Availability Zone. We continue to work on recovering regional resources, as well as zonal resources hosted in the other affected Availability Zones (mec1-az1 and mec1-az3). […] We are working on replacing the affected infrastructure and will provide an update on the restoration of our services in the coming months."

**Still-standing guidance (2026-04-30 07:25 UTC; not superseded):**
> "The Middle East (UAE) Region (ME-CENTRAL-1) has suffered damage as a result of the conflict in the Middle East and is currently unable to reliably support customer applications. While some workloads continue to function normally, we strongly recommend customers migrate all accessible resources to other Regions […]. This process is expected to take several months."

**Older notices (2026-03-01 to 2026-03-03; superseded by the later updates):**
- Power and connectivity problems in one zone (mec1-az2) on 2026-03-01, then a second zone (mec1-az3) on 2026-03-02.
- S3 and DynamoDB error rates were raised, and EC2 launches were throttled.
- The 2026-03-03 16:14 UTC update said further updates would go to affected customers through the Personal Health Dashboard, and recommended moving to "AWS Regions in the United States, Europe, or Asia Pacific".

**What isn't known:**
- AWS has published **no restoration date**, and there has been no public update since 2026-09-15.
- Account-specific detail goes only to the Personal Health Dashboard, which wasn't checked because no AWS account was used.
- Secondary reporting matches the AWS text, for example [InfoQ, 2026-09](https://www.infoq.com/news/2026/09/aws-middle-east-data-loss/) and [The Register, 2026-03-02](https://www.theregister.com/off-prem/2026/03/02/aws-says-drones-hit-two-of-its-datacenters-in-uae/4663049).

**Related:** the same feed shows **ME-SOUTH-1 (Bahrain)** as unavailable. AWS's 2026-09-15 update says it can't restore data held only in that region. So no other AWS Middle East region is a near-term substitute.

**Assessment [D]:**
1. **As of today, `me-central-1` is not usable for the plan.** AWS's own still-standing guidance says the region cannot reliably support customer applications and advises customers to migrate out. ADR-17 proposes the pilot environment there, which would hold the US pilot and then the UAE launch (weeks 9–12 now use temporary `us-east-1` test deployments).
2. **Whether it is still blocked at the decision point (before the pilot environment) is unknown.** AWS has given no date. The region decision has to be made on the AWS status at that time, so re-check before deciding.
3. **Relevance to the pilot risks in doc 04 §5.3:** AWS permanently lost data held only in one zone (mec1-az2). That is direct evidence for the single-AZ RDS risk (E3) and the "no multi-region disaster recovery" residual risk, whatever region is chosen. Backups stored in another region would have been the recovery path.

**Documented fallback** (ADR-17 in doc 11; doc 04 §5.1 region row):
- **Option A:** `us-east-1` for the US pilot, with a UAE deployment later.
- **Already required whichever region is chosen:**
  - Terraform takes the region as a parameter, with nothing hard-coded.
  - `tenants.home_region` records where each tenant's data lives.
  - The CloudFront certificate and the CloudFront-scope WAF are in `us-east-1` anyway.
  - No region-specific features are used.
- **What Option A changes, as documented:**
  - US pilot staff would avoid the roughly 200–250 ms admin latency.
  - The "data stored in the UAE" disclosure in the pilot terms (G12) would change.
  - A later UAE deployment, and possibly a tenant migration, would be needed for the UAE launch.

**Nothing in this record changes ADR-17.** It stays **Proposed**, and the region decision stays due before the pilot environment.

## 4. Open points for the product owner

1. **ADR-17 region** (§3.2): AWS says `me-central-1` cannot currently support customer applications reliably and has given no restoration date. The documented fallback is Option A (`us-east-1` for the pilot). The decision is due before the pilot environment (doc 10 §12.5 phase A); re-check AWS status then. Week 1 work is unaffected, because nothing is deployed and the local stack uses no AWS region.
2. **Local S3 emulator** (§1): staying on LocalStack 4.14.0 means a frozen image with no further fixes. 4.14.0 is the last SemVer release; later releases use calendar versions and need a token. The Community GitHub repository was archived on 2026-03-23, and LocalStack says Community gets no further updates or security patches. The alternatives are a free LocalStack Hobby account and token (an account, and the token becomes a CI secret) or another emulator. [R] Keep 4.14.0 for MVP-1 local development and tests, because conditional writes and versioning (the features the publish flow needs) were verified. Temporary; see §1.1 for its limitations and the reassessment triggers. No image switch and no token without the product owner's approval.
3. **Node.js**: done, 24.22.3 installed locally and pinned in `src/frontend/.nvmrc`.
4. **.NET SDK**: update locally to 10.0.404 (optional security patch), then raise `global.json`.
5. **AWS-only checks** (RDS PostgreSQL 18 in `us-east-1`, Terraform plan, LocalStack vs AWS behaviour, `me-central-1` account-level checks): **pending until the final AWS phase** (doc 10 §12.7; product owner, 2026-11-22).

## 5. Sources

- .NET 10 release metadata: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- NuGet package metadata: https://api.nuget.org/v3-flatcontainer/
- npm registry (`@angular/cli`): https://registry.npmjs.org/@angular/cli
- Angular releases: https://angular.dev/reference/releases
- Node.js releases: https://nodejs.org/dist/index.json
- RDS for PostgreSQL release calendar: https://docs.aws.amazon.com/AmazonRDS/latest/PostgreSQLReleaseNotes/postgresql-release-calendar.html
- AWS regional services table: https://api.regional-table.region-services.aws.a2z.com/index.json
- AWS Fargate Regions: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate-Regions.html
- AWS Health Dashboard: https://health.aws.amazon.com/health/status (public feed `https://health.aws.amazon.com/public/currentevents`)
- LocalStack single image: https://blog.localstack.cloud/localstack-single-image-next-steps/
- LocalStack, the road ahead (Community delivery changes): https://blog.localstack.cloud/the-road-ahead-for-localstack/
- LocalStack Community repository (archived 2026-03-23): https://github.com/localstack/localstack
- Docker Hub tags: postgres, amazon/dynamodb-local, localstack/localstack, axllent/mailpit, keycloak/keycloak
