#!/bin/sh
# Local PostgreSQL roles (doc 09 §4.2). Runs once, on first start with an empty volume.
#   menux_owner     owns the schemas; runs migrations (MenuX.Host.Migrator)
#   menux_app       application role: NOT the owner, NO BYPASSRLS -> subject to Row-Level Security
#   menux_platform  platform jobs only (tenant purge, cross-tenant operations): BYPASSRLS, audited
#   menux_rls_definer  NOLOGIN, BYPASSRLS: owns narrow SECURITY DEFINER functions (e.g. sign-in lookup);
#                      only menux_owner is a member (to assign ownership), never menux_app
# Passwords come from .env (placeholders for local development only).
set -eu
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<SQL
CREATE EXTENSION IF NOT EXISTS citext;
CREATE ROLE menux_owner    LOGIN PASSWORD '${MENUX_OWNER_PASSWORD}';
CREATE ROLE menux_app      LOGIN NOBYPASSRLS PASSWORD '${MENUX_APP_PASSWORD}';
CREATE ROLE menux_platform LOGIN BYPASSRLS PASSWORD '${MENUX_PLATFORM_PASSWORD}';
CREATE ROLE menux_rls_definer NOLOGIN BYPASSRLS;
GRANT menux_rls_definer TO menux_owner;
GRANT CONNECT ON DATABASE "$POSTGRES_DB" TO menux_owner, menux_app, menux_platform;
GRANT CREATE ON DATABASE "$POSTGRES_DB" TO menux_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
SQL
