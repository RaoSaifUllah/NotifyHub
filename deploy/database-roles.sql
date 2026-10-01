-- Run as schema owner after explicit migrations, on a dedicated NotifyHub database.
-- Provision LOGIN roles/passwords through your secret manager first.
-- Expected role names: notifyhub_write, notifyhub_read. Never grant SUPERUSER.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO notifyhub_write, notifyhub_read;
GRANT SELECT, INSERT, UPDATE, DELETE ON workspaces TO notifyhub_write;
GRANT SELECT ON workspace_summaries TO notifyhub_read;
-- No default broad future-table grants and no underlying-table grant for Read.
GRANT SELECT ON "__EFMigrationsHistory" TO notifyhub_write;
GRANT SELECT, INSERT, UPDATE, DELETE ON "AspNetUsers", "AspNetRoles",
"AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens",
"AspNetRoleClaims", workspace_members TO notifyhub_write;
GRANT USAGE, SELECT ON SEQUENCE "AspNetUserClaims_Id_seq", "AspNetRoleClaims_Id_seq" TO notifyhub_write;
GRANT SELECT, INSERT, UPDATE, DELETE ON refresh_sessions, refresh_tokens TO notifyhub_write;
