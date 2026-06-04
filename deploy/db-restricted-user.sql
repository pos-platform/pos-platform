-- PosPlatform — cheklangan huquqli PostgreSQL foydalanuvchi
-- Productionda application ulanish uchun; superuser "postgres" emas!
--
-- Ishlatish:
--   psql -U postgres -d postgres -f db-restricted-user.sql
--
-- Parolni o'zgartirishni UNUTMANG (CHANGE_ME_strong_password → haqiqiy parol).

-- 1. Foydalanuvchi yaratish
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'posplatform_app') THEN
        CREATE ROLE posplatform_app WITH LOGIN
            PASSWORD 'CHANGE_ME_strong_password'
            NOSUPERUSER NOCREATEDB NOCREATEROLE;
    END IF;
END;
$$;

-- 2. Bazaga ulanish huquqi
GRANT CONNECT ON DATABASE pos_platform TO posplatform_app;

-- 3. Sxema ichidagi barcha hozirgi + kelajakdagi ob'yektlarga huquqlar
\c pos_platform

GRANT USAGE ON SCHEMA public TO posplatform_app;

-- Jadvallar: SELECT, INSERT, UPDATE, DELETE (DELETE faqat zarur jadvallarga)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO posplatform_app;

-- Kelajakda CREATE TABLE/migration bilan yaratilgan jadvallarga ham
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO posplatform_app;

-- Sequences (SERIAL / auto-increment uchun)
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO posplatform_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO posplatform_app;

-- 4. Migration foydalanuvchisiga alohida to'liq huquq (ixtiyoriy — migration uchun boshqa user)
--    Migration productionda bitta marta, deploy paytida ishga tushadi.
--    Agar migration ham shu user orqali bo'lsa — CREATETABLE huquqi kerak.
--    Xavfsizroq variant: migration uchun alohida posplatform_migrate user yarating.

-- DO $$
-- BEGIN
--     IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'posplatform_migrate') THEN
--         CREATE ROLE posplatform_migrate WITH LOGIN
--             PASSWORD 'CHANGE_ME_migrate_password'
--             NOSUPERUSER NOCREATEDB NOCREATEROLE;
--     END IF;
-- END;
-- $$;
-- GRANT ALL ON DATABASE pos_platform TO posplatform_migrate;
-- GRANT ALL ON SCHEMA public TO posplatform_migrate;
-- GRANT ALL ON ALL TABLES IN SCHEMA public TO posplatform_migrate;
-- GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO posplatform_migrate;

-- 5. Tekshirish
-- \du posplatform_app
-- \dp

-- appsettings.Production.json ga:
-- "ConnectionStrings": {
--   "DefaultConnection": "Host=localhost;Port=5432;Database=pos_platform;Username=posplatform_app;Password=HAQIQIY_PAROL"
-- }
