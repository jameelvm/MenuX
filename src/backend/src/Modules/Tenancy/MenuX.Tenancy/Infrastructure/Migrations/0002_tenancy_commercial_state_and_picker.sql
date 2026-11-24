-- Tenancy: self-service tenant creation and billing readiness (product owner, 2026-11-23).
--
-- Commercial state: a local record next to plan_key (U7: billing deferred, plan_key + static limits).
-- ASSUMPTION (pending the billing phase): no trial duration, pricing or access rules are defined yet.
-- The status values mirror doc 08 billing.subscriptions; 'none' = no subscription record yet.
-- A later billing module (e.g. Stripe) updates these columns through events; nothing else changes.

ALTER TABLE tenancy.tenants
    ADD COLUMN subscription_status text NOT NULL DEFAULT 'none'
        CHECK (subscription_status IN ('none', 'trialing', 'active', 'past_due', 'canceled')),
    ADD COLUMN trial_ends_at timestamptz NULL,
    ADD COLUMN created_by uuid NULL;   -- the user who created the tenant (Identity ID, no cross-schema FK)

-- Tenant picker: a signed-in user may READ the tenants they are a member of. The member tenant IDs come
-- from the Identity module (verified memberships) and are passed as app.member_tenant_ids for that one
-- transaction; writes still require the selected tenant (app.tenant_id).
DROP POLICY tenant_isolation ON tenancy.tenants;
CREATE POLICY tenant_isolation ON tenancy.tenants
    USING (id = nullif(current_setting('app.tenant_id', true), '')::uuid
        OR id = ANY (string_to_array(nullif(current_setting('app.member_tenant_ids', true), ''), ',')::uuid[]))
    WITH CHECK (id = nullif(current_setting('app.tenant_id', true), '')::uuid);
