-- Add the optional hash of the 6-digit boarding PIN next to the QR code hash.
-- Nullable: trips created before the PIN existed keep validating the PIN against codigo_hash.
BEGIN;

ALTER TABLE trip_execution.abordajes
    ADD COLUMN IF NOT EXISTS pin_hash varchar(128) NULL;

COMMIT;
