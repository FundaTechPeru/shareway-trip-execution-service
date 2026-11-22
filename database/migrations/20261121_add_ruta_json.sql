-- Add optional route metadata to existing trips without altering existing rows.
-- Apply only after confirming that trip_execution.viajes already exists.
BEGIN;

ALTER TABLE trip_execution.viajes
    ADD COLUMN IF NOT EXISTS "RutaJson" jsonb NULL;

COMMIT;
