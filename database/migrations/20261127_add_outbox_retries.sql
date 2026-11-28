-- Retry bookkeeping for the outbox: attempts, next attempt time, permanent failure and last error.
-- Existing rows start with zero attempts and are eligible to be sent right away.
BEGIN;

ALTER TABLE trip_execution.outbox
    ADD COLUMN IF NOT EXISTS "Intentos"           integer       NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "SiguienteIntentoEn" timestamptz   NULL,
    ADD COLUMN IF NOT EXISTS "FallidoEn"          timestamptz   NULL,
    ADD COLUMN IF NOT EXISTS "UltimoError"        varchar(2000) NULL;

CREATE INDEX IF NOT EXISTS ix_outbox_pendientes
    ON trip_execution.outbox ("OcurridoEn")
    WHERE "ProcesadoEn" IS NULL AND "FallidoEn" IS NULL;

COMMIT;
