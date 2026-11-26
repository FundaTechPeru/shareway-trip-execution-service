-- Add optional vehicle id to existing trips without altering existing rows.
-- The column is nullable so old trips registered without a vehicle keep working,
-- and the panel can rehydrate the vehicle details from Driver Management on demand.
BEGIN;

ALTER TABLE trip_execution.viajes
    ADD COLUMN IF NOT EXISTS "VehiculoId" uuid NULL;

CREATE INDEX IF NOT EXISTS ix_viajes_vehiculo_id
    ON trip_execution.viajes ("VehiculoId")
    WHERE "VehiculoId" IS NOT NULL;

COMMIT;
