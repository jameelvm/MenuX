-- Tenancy module: tenants and locations (doc 08 §4.1). Runs as the schema owner (menux_owner).
-- Row-Level Security: the application role (menux_app) sees only the tenant in app.tenant_id,
-- which TenantSessionInterceptor sets with SET LOCAL per transaction (doc 09 §4.2).

CREATE SCHEMA IF NOT EXISTS tenancy;
GRANT USAGE ON SCHEMA tenancy TO menux_app;

CREATE TABLE tenancy.tenants (
    id                    uuid        PRIMARY KEY,
    slug                  citext      NOT NULL UNIQUE,
    legal_name            text        NOT NULL,
    display_name          text        NOT NULL,
    business_type         text        NOT NULL CHECK (business_type IN ('restaurant', 'hotel', 'cafe', 'shop', 'other')),
    country_code          char(2)     NOT NULL,
    default_currency      char(3)     NOT NULL,
    default_language      text        NOT NULL,
    supported_languages   text[]      NOT NULL CHECK (cardinality(supported_languages) BETWEEN 1 AND 5),
    status                text        NOT NULL CHECK (status IN ('pending', 'active', 'suspended', 'pending_deletion', 'deleting', 'deleted')),
    plan_key              text        NOT NULL DEFAULT 'pilot',
    market_pack           text        NOT NULL,
    home_region           text        NOT NULL,
    suspended_reason      text        NULL,
    deletion_requested_at timestamptz NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    row_version           integer     NOT NULL DEFAULT 0
);

-- Outlets are locations under their tenant; no hierarchy above the tenant (U4).
CREATE TABLE tenancy.locations (
    tenant_id        uuid        NOT NULL REFERENCES tenancy.tenants (id),
    id               uuid        NOT NULL,
    slug             citext      NOT NULL,
    name             text        NOT NULL,
    location_type    text        NULL,
    timezone         text        NOT NULL,
    address          jsonb       NOT NULL DEFAULT '{}',
    currency         char(3)     NOT NULL,
    price_display    jsonb       NOT NULL DEFAULT '{}',
    status           text        NOT NULL CHECK (status IN ('active', 'inactive')),
    ordering_enabled boolean     NOT NULL DEFAULT false,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    row_version      integer     NOT NULL DEFAULT 0,
    PRIMARY KEY (tenant_id, id),
    UNIQUE (tenant_id, slug)
);

GRANT SELECT, INSERT, UPDATE ON tenancy.tenants, tenancy.locations TO menux_app;

ALTER TABLE tenancy.tenants ENABLE ROW LEVEL SECURITY;
ALTER TABLE tenancy.tenants FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON tenancy.tenants
    USING (id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (id = nullif(current_setting('app.tenant_id', true), '')::uuid);

ALTER TABLE tenancy.locations ENABLE ROW LEVEL SECURITY;
ALTER TABLE tenancy.locations FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON tenancy.locations
    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
