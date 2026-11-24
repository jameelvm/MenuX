-- Audit module: append-only audit trail (doc 08 §4.7). Runs as menux_owner.
-- Partitioned by month on occurred_at; a DEFAULT partition catches rows until monthly partitions
-- are managed (later). The application role can only INSERT and SELECT through the parent table,
-- and only for the selected tenant. Platform-level events (tenant_id NULL) are written by the
-- platform role.

CREATE SCHEMA IF NOT EXISTS audit;
GRANT USAGE ON SCHEMA audit TO menux_app;

CREATE TABLE audit.audit_events (
    id             uuid        NOT NULL,
    tenant_id      uuid        NULL,
    actor_type     text        NOT NULL CHECK (actor_type IN ('user', 'staff', 'system', 'support')),
    actor_id       uuid        NULL,
    action         text        NOT NULL,
    target_type    text        NULL,
    target_id      text        NULL,
    ip             inet        NULL,
    user_agent     text        NULL,
    correlation_id text        NULL,
    metadata       jsonb       NOT NULL DEFAULT '{}',
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

CREATE TABLE audit.audit_events_default PARTITION OF audit.audit_events DEFAULT;
CREATE INDEX audit_events_tenant_time ON audit.audit_events (tenant_id, occurred_at DESC);

-- Append-only for the application: no UPDATE or DELETE, and no direct access to partitions.
GRANT SELECT, INSERT ON audit.audit_events TO menux_app;
REVOKE ALL ON audit.audit_events_default FROM menux_app;

ALTER TABLE audit.audit_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.audit_events FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON audit.audit_events
    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
