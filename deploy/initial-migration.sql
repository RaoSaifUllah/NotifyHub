CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE workspaces (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    time_zone character varying(100) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_workspaces" PRIMARY KEY (id)
);

CREATE VIEW workspace_summaries AS SELECT id, name, time_zone, created_at FROM workspaces

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261001161350_InitialWorkspace', '10.0.12');

COMMIT;

