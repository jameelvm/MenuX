-- Identity: email verification and first-login provisioning (product owner, 2026-11-23).
-- A MenuX user is created only at first login with a verified email (enforced by the identity provider
-- AND the API). The email, verified flag and name are read from the ID token once; afterwards the
-- database is the source of truth. Sessions keep only iss + sub (ADR-06).

ALTER TABLE identity.users ADD COLUMN email_verified_at timestamptz NULL;

-- Defence in depth: an active user must have a verified email.
ALTER TABLE identity.users ADD CONSTRAINT users_active_requires_verified_email
    CHECK (status <> 'active' OR email_verified_at IS NOT NULL);

-- New permission tenant.manage (edit the tenant profile) for Owner and Admin [D]. Forced RLS also applies
-- to the owner, so FORCE is lifted for this seed only (the owner is then exempt as table owner).
ALTER TABLE identity.role_permissions NO FORCE ROW LEVEL SECURITY;
INSERT INTO identity.role_permissions (role_id, permission_key) VALUES
    ('0193a000-0000-7000-8000-000000000001', 'tenant.manage'),
    ('0193a000-0000-7000-8000-000000000002', 'tenant.manage');
ALTER TABLE identity.role_permissions FORCE ROW LEVEL SECURITY;
