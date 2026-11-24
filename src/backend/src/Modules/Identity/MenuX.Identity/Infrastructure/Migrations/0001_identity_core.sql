-- Identity module: users, memberships, roles and permissions (doc 08 §4.2). Runs as menux_owner.
-- Provider-neutral identity (ADR-06): a user is (idp_issuer, idp_subject); Keycloak locally, Cognito in AWS.
-- Tenant and location IDs from the Tenancy module are stored as plain values: no foreign keys across
-- module schemas (doc 05 §3 rule 2); composite (tenant_id, id) keys apply within this schema.
--
-- Row-Level Security settings (set with SET LOCAL by TenantSessionInterceptor):
--   app.tenant_id  the selected tenant
--   app.user_id    the signed-in user, so "my memberships" can be listed before a tenant is selected

CREATE SCHEMA IF NOT EXISTS identity;
GRANT USAGE ON SCHEMA identity TO menux_app;

CREATE TABLE identity.users (
    id            uuid        PRIMARY KEY,
    idp_issuer    text        NOT NULL,
    idp_subject   text        NOT NULL,
    email         citext      NOT NULL UNIQUE,
    display_name  text        NOT NULL,
    status        text        NOT NULL CHECK (status IN ('active', 'disabled')),
    last_login_at timestamptz NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    UNIQUE (idp_issuer, idp_subject)
);

CREATE TABLE identity.memberships (
    tenant_id  uuid        NOT NULL,
    id         uuid        NOT NULL,
    user_id    uuid        NOT NULL REFERENCES identity.users (id),
    status     text        NOT NULL CHECK (status IN ('invited', 'active', 'disabled')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (tenant_id, id),
    UNIQUE (tenant_id, user_id)
);
CREATE INDEX memberships_user_id ON identity.memberships (user_id);

-- tenant_id NULL = system role (owner, admin, editor, staff).
CREATE TABLE identity.roles (
    id         uuid        PRIMARY KEY,
    tenant_id  uuid        NULL,
    key        text        NOT NULL,
    name       text        NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE NULLS NOT DISTINCT (tenant_id, key)
);

CREATE TABLE identity.role_permissions (
    role_id        uuid NOT NULL REFERENCES identity.roles (id),
    permission_key text NOT NULL,
    PRIMARY KEY (role_id, permission_key)
);

CREATE TABLE identity.membership_roles (
    tenant_id     uuid NOT NULL,
    membership_id uuid NOT NULL,
    role_id       uuid NOT NULL REFERENCES identity.roles (id),
    PRIMARY KEY (tenant_id, membership_id, role_id),
    FOREIGN KEY (tenant_id, membership_id) REFERENCES identity.memberships (tenant_id, id)
);

-- No rows for a membership = all locations of the tenant.
CREATE TABLE identity.membership_locations (
    tenant_id     uuid NOT NULL,
    membership_id uuid NOT NULL,
    location_id   uuid NOT NULL,
    PRIMARY KEY (tenant_id, membership_id, location_id),
    FOREIGN KEY (tenant_id, membership_id) REFERENCES identity.memberships (tenant_id, id)
);

-- System roles and permissions (doc 03 §2). Seeded before RLS is forced, as the owner.
-- Editor publishing is a tenant setting (doc 03 ⚙): tenants grant it through a custom role.
INSERT INTO identity.roles (id, tenant_id, key, name) VALUES
    ('0193a000-0000-7000-8000-000000000001', NULL, 'owner',  'Owner'),
    ('0193a000-0000-7000-8000-000000000002', NULL, 'admin',  'Admin'),
    ('0193a000-0000-7000-8000-000000000003', NULL, 'editor', 'Menu editor'),
    ('0193a000-0000-7000-8000-000000000004', NULL, 'staff',  'Location staff');

INSERT INTO identity.role_permissions (role_id, permission_key)
SELECT r.id, p.key
FROM identity.roles r
JOIN (VALUES
    ('owner', 'tenant.billing'), ('owner', 'tenant.delete'), ('owner', 'member.manage'),
    ('owner', 'location.manage'), ('owner', 'catalog.edit'), ('owner', 'menu.edit'),
    ('owner', 'menu.publish'), ('owner', 'item.sold_out'), ('owner', 'qr.manage'), ('owner', 'audit.read'),
    ('admin', 'member.manage'), ('admin', 'location.manage'), ('admin', 'catalog.edit'), ('admin', 'menu.edit'),
    ('admin', 'menu.publish'), ('admin', 'item.sold_out'), ('admin', 'qr.manage'), ('admin', 'audit.read'),
    ('editor', 'catalog.edit'), ('editor', 'menu.edit'), ('editor', 'item.sold_out'),
    ('staff', 'item.sold_out')
) AS p (role_key, key) ON p.role_key = r.key
WHERE r.tenant_id IS NULL;

GRANT SELECT, INSERT, UPDATE ON identity.users, identity.memberships TO menux_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.roles, identity.role_permissions,
    identity.membership_roles, identity.membership_locations TO menux_app;

-- Users: the signed-in user, or users who belong to the selected tenant.
ALTER TABLE identity.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.users FORCE ROW LEVEL SECURITY;
CREATE POLICY user_visibility ON identity.users
    USING (id = nullif(current_setting('app.user_id', true), '')::uuid
        OR EXISTS (SELECT 1 FROM identity.memberships m
                   WHERE m.user_id = users.id
                     AND m.tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid))
    WITH CHECK (id = nullif(current_setting('app.user_id', true), '')::uuid);

-- Memberships: the selected tenant's memberships, plus the signed-in user's own (tenant picker).
ALTER TABLE identity.memberships ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.memberships FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.memberships
    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
        OR user_id = nullif(current_setting('app.user_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

-- Roles: system roles are visible to every tenant; tenants can only write their own roles.
ALTER TABLE identity.roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.roles FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.roles
    USING (tenant_id IS NULL OR tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

ALTER TABLE identity.role_permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.role_permissions FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.role_permissions
    USING (EXISTS (SELECT 1 FROM identity.roles r WHERE r.id = role_id))
    WITH CHECK (EXISTS (SELECT 1 FROM identity.roles r
                        WHERE r.id = role_id
                          AND r.tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid));

ALTER TABLE identity.membership_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.membership_roles FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.membership_roles
    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

ALTER TABLE identity.membership_locations ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.membership_locations FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.membership_locations
    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

-- Sign-in lookup before user_id is known: returns only the ID for (issuer, subject).
-- SECURITY DEFINER, owned by menux_rls_definer (NOLOGIN, BYPASSRLS, SELECT on identity.users only),
-- because forced RLS also applies to the schema owner. search_path pinned to avoid hijacking.
CREATE FUNCTION identity.find_user_id(p_issuer text, p_subject text)
    RETURNS uuid
    LANGUAGE sql STABLE SECURITY DEFINER
    SET search_path = pg_catalog, identity
AS $$
    SELECT id FROM identity.users WHERE idp_issuer = p_issuer AND idp_subject = p_subject AND status = 'active';
$$;
GRANT USAGE ON SCHEMA identity TO menux_rls_definer;
GRANT SELECT ON identity.users TO menux_rls_definer;
-- A new owner needs CREATE on the schema at the moment of the ownership change; revoked straight after.
GRANT CREATE ON SCHEMA identity TO menux_rls_definer;
ALTER FUNCTION identity.find_user_id(text, text) OWNER TO menux_rls_definer;
REVOKE CREATE ON SCHEMA identity FROM menux_rls_definer;
REVOKE ALL ON FUNCTION identity.find_user_id(text, text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION identity.find_user_id(text, text) TO menux_app;
