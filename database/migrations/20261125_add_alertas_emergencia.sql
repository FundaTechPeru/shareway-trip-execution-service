-- Create the alertas_emergencia table. Alerts coexist with the trip they belong to,
-- so the FK is logical (by ViajeId) without ON DELETE CASCADE: a cancelled trip
-- keeps its alert history for the admin console.
BEGIN;

CREATE TABLE IF NOT EXISTS trip_execution.alertas_emergencia (
    "Id"              uuid         PRIMARY KEY,
    "ViajeId"         uuid         NOT NULL,
    "ReportanteId"    uuid         NOT NULL,
    "RolReportante"   varchar(20)  NOT NULL,
    "Tipo"            varchar(60)  NOT NULL,
    "Descripcion"     varchar(1000) NOT NULL,
    "Estado"          varchar(20)  NOT NULL,
    "CreadaEn"        timestamptz  NOT NULL,
    "AtendidaEn"      timestamptz  NULL,
    "AtendidaPor"     uuid         NULL,
    "Version"         integer      NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_alertas_estado    ON trip_execution.alertas_emergencia ("Estado");
CREATE INDEX IF NOT EXISTS ix_alertas_viaje_id  ON trip_execution.alertas_emergencia ("ViajeId");

COMMIT;
