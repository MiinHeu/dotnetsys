-- Migration: AddOriginalDescriptionToPoiTranslation
-- Created manually for SQLite development

ALTER TABLE "PoiTranslations" ADD "OriginalDescription" TEXT NOT NULL DEFAULT '';

-- For existing records, set OriginalDescription = Description to preserve data
UPDATE "PoiTranslations" SET "OriginalDescription" = "Description" WHERE "OriginalDescription" = '';
