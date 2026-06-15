# SoftimProject API — návod pro práci s API

API umožňuje programový přístup k projektům, **ticketům** a **worklogům** (plný CRUD) i dalším objektům.

- **Base URL (cloud):** `https://softimproject-api.azurewebsites.net`
- **Swagger UI:** `https://softimproject-api.azurewebsites.net/swagger`
- **Verzování:** cesty mají prefix `/api/v1/…`

## Autentizace

API přijímá dvě schémata:

1. **Osobní API klíč** (doporučeno pro skripty/integrace) — vygeneruj si ho v aplikaci v **uživatelské menu → API klíče** (stránka `/api-keys`). Klíč `spk_…` se zobrazí **jen jednou**.
   - Pošli ho v hlavičce: `Authorization: Bearer spk_…` (nebo `X-Api-Key: spk_…`).
   - Volání běží **pod tvými oprávněními** (stejně, jako bys byl přihlášený).
   - Klíč má volitelnou expiraci a lze ho kdykoli revokovat.
2. **Entra ID JWT** — interaktivní přihlášení (používá web).

Ve **Swaggeru** klikni *Authorize* a vlož API klíč do pole **Bearer** (předpona `Bearer ` se doplní sama).

## Konvence

- **Stránkování:** seznamy vrací `{ items, totalCount, page, pageSize, hasNext }`. Parametry `page` (1+) a `pageSize`.
- **Chyby:** JSON `{ "message": "…", "errors": ["…"] }`. Stavové kódy: `400` validace, `403` bez oprávnění, `404` nenalezeno, `429` překročen rate-limit, `500` neočekávaná chyba.
- **Rate-limit (jen API klíče):** 120 požadavků / min na klíč; při překročení `429` s hlavičkou `Retry-After`.

## Příklady

Viz [`SoftimProject.http`](./SoftimProject.http) (VS Code REST Client / JetBrains HTTP client) — autentizace, výpis a založení ticketu, worklogy. Plný seznam endpointů je v Swaggeru.
