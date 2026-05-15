# SoftimProject

[![CI](https://github.com/softim-cz/softimproject/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/softim-cz/softimproject/actions/workflows/ci.yml)
[![Build & Deploy](https://github.com/softim-cz/softimproject/actions/workflows/deploy.yml/badge.svg?branch=main)](https://github.com/softim-cz/softimproject/actions/workflows/deploy.yml)

Interní nástroj pro řízení projektů a servisních aktivit firmy Softim. Centrální hub pro správu projektů, tiketů, kanban boardů, worklogů, komentářů, příloh, klientského portálu, migraci z Easy Projectu, GitHub integraci, AI sumarizaci a MCP server pro AI asistenty.

Vývojové standardy a onboarding: [CONTRIBUTING.md](./CONTRIBUTING.md)

## Tech stack

**Backend** (`backend/`)

- .NET 10 (SDK 10.0.100), C# s `TreatWarningsAsErrors=true`
- Clean Architecture: `Domain` → `Application` → `Infrastructure` → `WebApi`
- MediatR CQRS s cross-cutting pipeline (logging, validace, autorizace)
- EF Core 10 + Dapper, Azure SQL
- Microsoft.Identity.Web (Entra ID), Serilog, SignalR
- Central Package Management (`Directory.Packages.props` – žádné inline verze v .csproj)
- Samostatný MCP server (`SoftimProject.McpServer`) pro AI asistenty

**Frontend** (`frontend/`)

- Next.js 16 (App Router), React 19, TypeScript, Tailwind 4, shadcn/ui
- TanStack Query (centralizovaná query key factory v `queries/query-keys.ts`)
- Zustand, React Hook Form, Zod
- MSAL (Entra ID) pro interní uživatele, token-based auth pro klientský portál
- SignalR klient pro realtime kanban, notifikace a migration progress

## Struktura repozitáře

```
backend/
  Directory.Packages.props    # Central Package Management
  SoftimProject.sln
  src/
    SoftimProject.Domain          # entity, enums, doménové pravidla
    SoftimProject.Application     # CQRS handlery (MediatR), DTOs, interfaces
    SoftimProject.Infrastructure  # EF Core, background services, integrace, AI
    SoftimProject.WebApi          # REST API, SignalR huby, auth, controllery
    SoftimProject.McpServer       # samostatný MCP server pro AI asistenty
  tests/
    SoftimProject.Domain.Tests
    SoftimProject.Application.Tests
    SoftimProject.Infrastructure.Tests
    SoftimProject.WebApi.Tests

frontend/
  src/
    app/                 # Next.js App Router (dashboardy, projekty, portál)
    components/          # shadcn/ui a doménové komponenty
    queries/             # TanStack Query moduly + centrální query-keys
    stores/              # Zustand stores
    lib/auth             # MSAL provider + hooks
    lib/signalr          # SignalR provider
    schemas/             # Zod validační schémata

assignment/              # zadání, swagger definice Easy Projectu
```

## Požadavky

- .NET SDK **10.0.100+**
- Node.js **22.17+**
- SQL Server / Azure SQL (lokálně lze SQL Server Express nebo LocalDB)
- Azure Blob Storage (pro přílohy) – volitelné pro lokální běh
- Azure OpenAI resource – volitelné, pro AI sumarizaci a reporty
- Entra ID (Azure AD) app registration – pro auth

## Konfigurace

### Backend – `backend/src/SoftimProject.WebApi/appsettings.json`

Pro lokální vývoj vytvořte `appsettings.Development.json` nebo použijte User Secrets (`dotnet user-secrets`). Nikdy necommitujte reálné hodnoty.

| Sekce | Klíč | Popis |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `Audience` | Entra ID app registration |
| `ConnectionStrings` | `DefaultConnection` | SQL connection string |
| `AzureBlobStorage` | `ConnectionString`, `ContainerName` | Blob storage pro přílohy |
| `AzureOpenAI` | `Endpoint`, `DeploymentName`, `ApiKey` | Azure OpenAI pro AI vrstvu |
| `Frontend` | `BaseUrl` | URL frontendu (CORS, odkazy v notifikacích) |
| `GitHub` | `ClientId`, `ClientSecret`, `CallbackUrl` | GitHub OAuth pro sync |

### Frontend – `frontend/.env.local`

```env
NEXT_PUBLIC_API_URL=http://localhost:5249
NEXT_PUBLIC_AZURE_AD_CLIENT_ID=<client-id>
NEXT_PUBLIC_AZURE_AD_TENANT_ID=<tenant-id>
NEXT_PUBLIC_SIGNALR_URL=http://localhost:5249/hubs
```

## Rychlý start (lokální dev stack)

Bez Azure přístupu — LocalDB + Azurite (blob emulátor) + DevAuth bypass místo Entra přihlášení. Vhodné pro denní vývoj i manuální smoke testy PR větví.

```bash
# jednorázově: kopie šablon
cp backend/src/SoftimProject.WebApi/appsettings.Development.json.template \
   backend/src/SoftimProject.WebApi/appsettings.Development.json   # gitignored
cp frontend/.env.example frontend/.env.local                        # gitignored
# v .env.local nastav NEXT_PUBLIC_DEV_AUTH=true

# deps (také jednorázově)
cd backend  && dotnet restore && cd ..
cd frontend && npm install    && cd ..

# start: API + FE + Azurite v jedné konzoli
bash scripts/dev-up.sh
# API:      http://localhost:5249  (Swagger na /swagger)
# FE:       http://localhost:3000
# Azurite:  127.0.0.1:10000-10002  (blob/queue/table)
# Ctrl-C    ukončí vše
```

Skript nastartuje SqlLocalDB, pustí Azurite na pozadí (logy v `.dev-logs/`), pustí API (které samo aplikuje migrace a naseedí data) a FE v popředí.

### Dev uživatelé (seed)

Backend běží s DevAuth schématem: místo Entra tokenu čte hlavičku `X-Dev-User-Id`. Frontend v `NEXT_PUBLIC_DEV_AUTH=true` módu skipne MSAL a tu hlavičku posílá automaticky. Výchozí uživatel je `dev:admin`, přepnout se dá v prohlížeči přes `localStorage.setItem('softim-dev-user-id', 'dev:manager')`.

| EntraObjectId | Global role | Project role (Demo Project) |
|---|---|---|
| `dev:admin` | Admin | ProjectManager |
| `dev:manager` | Manager | ProjectManager |
| `dev:user` | User | Developer |
| `dev:external` | User | Guest |

Seednutý `Demo Project` (`code=DEMO`) obsahuje 2 tickety, 1 komentář a 1 worklog — připraveno pro E2E/smoke testy.

> **Nikdy v produkci.** `DevAuth` registrace je v `Program.cs` gated přes `builder.Environment.IsDevelopment() && DevAuth:Enabled`. Azure App Service běží v `Production` env a token bypass tam není dostupný.

## Plný setup (proti reálným Azure zdrojům)

Pro práci s GitHub OAuth sync, Azure OpenAI, produkční Entra auth apod. — vyplňte reálné hodnoty v `appsettings.Development.json` (nebo `dotnet user-secrets`) a `frontend/.env.local`. `DevAuth:Enabled` nastavte na `false`.

```bash
# 1) Backend
cd backend
dotnet restore
dotnet build
dotnet run --project src/SoftimProject.WebApi  # migrace proběhnou automaticky

# 2) Frontend – v novém terminálu
cd frontend
npm install
npm run dev

# 3) MCP server – volitelně, v dalším terminálu
dotnet run --project backend/src/SoftimProject.McpServer
```

**Doporučené pořadí spuštění:** API → Frontend → (MCP server). Frontend se bez běžícího API nepřipojí přes MSAL k backendu a SignalR huby nebudou dostupné.

## Build a testy

```bash
# Backend
dotnet build backend/SoftimProject.sln
dotnet test  backend/SoftimProject.sln

# Frontend
cd frontend
npm run lint
npm run build
```

Oba buildy musí skončit s 0 errory (backend má `TreatWarningsAsErrors=true`).

## EF Core migrace

Migrace žijí v `backend/src/SoftimProject.Infrastructure/Persistence/Migrations`.

```bash
# Vytvoření nové migrace
dotnet ef migrations add <Nazev> \
  --project backend/src/SoftimProject.Infrastructure \
  --startup-project backend/src/SoftimProject.WebApi

# Aplikace na DB
dotnet ef database update \
  --project backend/src/SoftimProject.Infrastructure \
  --startup-project backend/src/SoftimProject.WebApi
```

> Pozor: SQL Server má cascade delete cycle omezení. Některé vztahy (Ticket→Project, Worklog→Project, ProjectMember→User) používají `NoAction` – při úpravách konfigurací na to myslete.

## Funkční moduly

- **Projekty, tickety, kanban** – CQRS handlery v `Application/Features`, realtime přes SignalR hub.
- **Worklogy** – evidence času, export XLSX s aktivními filtry.
- **Komentáře, přílohy, checklisty** – sdílená ownership validace (`ProjectResourceGuards`).
- **Lookup administrace** – typy projektů/úkolů, stavy, priority, role, firmy, šablony projektů.
- **Custom fields** – definice (`CustomFieldDefinition`) a hodnoty pro projekty a tickety.
- **GitHub integrace** – OAuth, linkování repozitářů, background sync service, webhooks.
- **Migrace z Easy Projectu** – REST client, background job s progress reportingem přes SignalR hub `MigrationHub`.
- **AI vrstva** – sumarizace tiketů, weekly reporty, deadline notifikace (background services v `Infrastructure/BackgroundServices`).
- **Klientský portál** – token-based auth, maskovaný pohled na projekty zákazníka (`/portal/[token]`).
- **MCP server** – samostatná služba s nástroji pro čtení projektů/tiketů a zápis worklogů. Auth přes Entra/JWT.

## Klientský portál — co klient uvidí a co ne

Portál je anonymní read-only view projektu přes unikátní URL `/{frontend}/portal/{token}`. Backend endpoint `GET /api/v1/portal/{token}` projeví data jen pokud je `Project.ClientAccessEnabled = true` **a** token sedí.

**Co vidí (`PortalTicketDto`, `PortalColumnDto`, `PortalProjectDto`):**

- Název, kód, popis projektu
- Stav projektu (Active / OnHold / Completed / …)
- Budget / spent hours, health score, is-over-budget / is-over-deadline flagy
- Kanban board: kolony, jejich pořadí, WIP limity, mapované task states
- Tickety: klíč (`CODE-N`), název, priorita (name + color), stav (name + color), assignee display name, due date
- Součet billable worklog hodin (agregát, ne per-user breakdown)

**Co nevidí (maskováno projekcí v controlleru):**

- Komentáře (interní ani non-internal — `PortalResponseDto.Comments` je `Array.Empty<object>()`)
- Description, estimated hours, cumulative worked hours ticketů
- Worklogy (ani záznamy, ani per-user součet — jen agregát billable hodin)
- Billable flag / invoiced string u worklogů
- Interní IDs kromě těch, co jsou potřeba pro FE routing
- Skryté kolony (`KanbanColumn.IsVisible=false`) — filter je v query

**Rotace tokenu:**

Admin v Settings → Client portal section:

- **Generate token** — vygeneruje nový 32B random base64-url, přepíše starý (pokud existoval) a zapne access
- **Regenerate** — stejná akce, ale s potvrzením „existující link přestane fungovat"
- **Revoke access** — token=null, enabled=false; portál vrátí 404
- Toggle **Client portal access enabled** — vypne portál bez smazání tokenu (lze znovu zapnout beze změny URL)

Token je unikátní v rámci všech projektů (`IX_Projects_ClientAccessToken` filter `IS NOT NULL`).

## Autorizační vrstva — co chrání jaký guard

Backend má čtyři marker interfaces napojené na MediatR `AuthorizationBehavior`. Handler je vlastně business logika, kontrola přístupu běží _před_ ním:

| Guard                    | Kde se aplikuje                                                         | Co kontroluje                                                             | Odpověď při selhání           |
| ------------------------ | ----------------------------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------- |
| `IRequireProjectAccess`  | Command/query napojený na konkrétní projekt (čtení, Guest-povolené zápisy) | Uživatel je member projektu **nebo** má `GlobalRole.Admin`                | `UnauthorizedAccessException` → HTTP 403 |
| `IRequireProjectRole`    | Zápisové operace, kde záleží na roli v projektu (PM vs Developer)       | Uživatel má hierarchicky dostatečnou `ProjectRole` na daném projektu; Admin bypass | `UnauthorizedAccessException` → HTTP 403 |
| `IRequireRole`           | Akce vyžadující konkrétní `GlobalRole` (např. `CreateProject` → Admin)  | Uživatel je v dané globální roli (nebo má claim)                          | `UnauthorizedAccessException` → HTTP 403 |
| `IRequirePermission`     | Akce napojené na `PermissionArea` + `PermissionOperation` (worklogy, reporty) | Alespoň jedna z `UserApplicationRoles` má flag na matici; Admin bypass | `UnauthorizedAccessException` → HTTP 403 |

### Matice rolí

Hierarchie: **Admin ≫ ProjectManager ≫ Developer ≫ Guest**. `IRequireProjectRole` porovnává členství hierarchicky — role, která je požadována jako `Developer`, je splněna i `ProjectManager`em; `Guest` ji nesplňuje.

| Role                         | Rozsah           | Co může                                                                                                     |
| ---------------------------- | ---------------- | ----------------------------------------------------------------------------------------------------------- |
| `GlobalRole.Admin`           | global           | Vše, napříč všemi projekty (bypass pro `IRequireProjectAccess` i `IRequireProjectRole`)                     |
| `ProjectRole.ProjectManager` | per-projekt      | Vše na projektu **kromě** smazání projektu samotného (Admin-only)                                           |
| `ProjectRole.Developer`      | per-projekt      | Read vše na projektu, Create ticket/comment/worklog, Update & Move ticket, Update/Delete **jen vlastních** zápisů |
| `ProjectRole.Guest`          | per-projekt (klientský portál) | Create task, Read task, Comment (Create + Read), Update/Delete vlastních comment/attachment/checklist       |

- `GlobalRole.Manager` je legacy; nová autorizační logika ho nepoužívá a v budoucnu ho odstraníme.
- Ownership na úrovni záznamů (comment/worklog/attachment update/delete) řeší handler inline: povoluje autora, PM daného projektu, nebo Admina.

### Další ochrany

- `[Authorize]` na `ApiControllerBase` zajišťuje, že anonymní požadavky skončí na 401 ještě před MediatR pipeline. Výjimky: klientský portál (`/api/v1/portal/{token}`), OAuth callbacky.
- DevAuth scheme (`X-Dev-User-Id` header) běží jen v `IsDevelopment()` a je kompletně oddělený od Entra JWT cesty.

Pokrytí integračními testy: `SoftimProject.WebApi.Tests.Integration.AuthorizationBoundaryTests` pokrývá oba pipeline guardy — cross-project isolation (`IRequireProjectAccess`) i role-matrix případy (Developer zablokován na PM-only akcích, Guest zablokován na Developer-only akcích, Admin & PM bypass).

## Observability — background jobs

Všechny `BackgroundService`-y dědí z `TrackedBackgroundService`, což automaticky zajišťuje tři věci při každém ticku:

1. **JobRun řádek** v tabulce `JobRuns` — Running při startu, Success / PartialSuccess / Failed při dokončení, s délkou běhu a čítači `itemsProcessed` / `itemsFailed`.
2. **Strukturované logy** s property `{JobName}` a `{JobRunId}` na každé zprávě (přes Serilog `LogContext`).
3. **Registrace v `IJobRegistry`** tak, aby `/health/jobs` znal očekávaný interval.

### Health endpoint

```
GET /api/v1/health/jobs          # anonymní — probeable infra monitoringem
```

Vrací `{ status, jobs[] }`:

- `status` = **Healthy** pokud všechny registrované joby mají nedávný běh a poslední status ≠ Failed
- `status` = **Degraded** + HTTP **503** pokud aspoň jeden job:
  - nemá žádný JobRun záznam (po restartu ještě neběžel), nebo
  - poslední běh je starší než **2× očekávaný interval** (grace na PeriodicTimer drift a idle sloty jako DeadlineNotificationService), nebo
  - poslední běh skončil ve stavu `Failed`

Pole `jobs[]` obsahuje per-job last-run timestamp, duration, processed/failed counters a error message.

### Kam se dívat když něco padne

| Symptom                              | Kde hledat                                                                      |
| ------------------------------------ | ------------------------------------------------------------------------------- |
| Job po restartu nenaběhl             | `/api/v1/health/jobs` → `isOverdue=true`, `lastRunAt=null`                      |
| Poslední běh Failed                  | `/health/jobs` → `lastError`; plný stack v logu (filtruj `JobName = "..."`)    |
| Rekonstrukce konkrétního běhu        | Log query na `JobRunId = "<guid>"` — dostane všechny logy z té iterace         |
| Sync per-projekt failuje             | `SyncLogs` tabulka (per-projekt audit, samostatně od JobRun)                    |

### Serilog a export do App Insights / Log Analytics

Konfigurace je v `appsettings.json` pod `Serilog`. `Enrich: FromLogContext` je zapnutý, takže `{JobName}` / `{JobRunId}` propagují do všech sinks. Pro napojení na Azure Monitor přidat sink (např. `Serilog.Sinks.ApplicationInsights` nebo OTel) do `WriteTo` s ConnectionString z `ApplicationInsights:ConnectionString` — proměnné jsou strukturované, takže se stanou custom dimensions automaticky.

Alerty na opakovaná selhání se nastavují na straně Azure Monitoru na dotaz typu `traces | where customDimensions.JobName == "X" and message startswith "Job ... finished Failed" | summarize count() by bin(timestamp, 1h)`.

## Retry + dead-letter pro integrační joby

Kromě strukturovaných logů a JobRun tracování má backend i retry/DLQ strategii pro volání externích služeb:

### Retry pipelines

Polly `ResiliencePipeline`-y jsou registrované pod pojmenovanými klíči v `ResiliencePipelines` (v `SoftimProject.Application.Interfaces`) a volají se přes `ResiliencePipelineProvider<string>`:

| Pipeline      | Profil                                                    | Kde se aplikuje                                                         |
| ------------- | --------------------------------------------------------- | ----------------------------------------------------------------------- |
| `ai-api`      | 3 retries, exponential backoff 2s + jitter               | `AiSummarizationService` (volání `IAiService.SummarizeTicketAsync`)    |
| `github-api`  | 4 retries, exponential backoff 2s + jitter               | `GitHubSyncService` (Octokit volání přes `GitHubSyncHelper`)           |
| *(HTTP handler)* | 5 retries, backoff + sliding-window rate limiter     | Easy Project API client (`AddResilienceHandler` v DI, starší existence) |

### Dead-letter queue

Když operace vyčerpá všechny retry pokusy, handler zachytí výjimku a zapíše záznam do tabulky `DeadLetterEntries` přes `IDeadLetterQueue`. Semantika je **upsert-by-key** (OperationType + OperationKey), takže opakovaná selhání stejné jednotky se akumulují na jednom řádku (AttemptCount++), nikoli jako N řádků.

DLQ sleduje: `OperationType` (enum — AiSummarizeTicket, GitHubSyncProject, EasyProjectFetch, GitHubWebhook), `OperationKey` (např. ticket id), `Payload` (JSON blob s kontextem), `AttemptCount`, `LastError` (stacktrace, truncováno na 4000 znaků), `FirstFailedAt`/`LastFailedAt`, `Status` (Pending/Replayed/Dismissed).

### Admin UI

Stránka `/admin` má sekci **Dead-letter queue** s filtrem "include resolved" a per-řádek akcemi Replay / Dismiss:

| Endpoint                                                 | Kdo               | Co dělá                                                              |
| -------------------------------------------------------- | ----------------- | -------------------------------------------------------------------- |
| `GET /api/v1/admin/dead-letter?includeResolved=true`     | Admin (`IRequireRole`) | Seznam entries (pending default, volitelně i replayed+dismissed) |
| `POST /api/v1/admin/dead-letter/{id}/replay`             | Admin             | Pokusí se znovu spustit operaci přes `IDeadLetterReplayer`; při úspěchu se entry posune do `Replayed`. Pro operace bez zaregistrovaného handleru vrátí 400 "replay not supported". |
| `POST /api/v1/admin/dead-letter/{id}/dismiss`            | Admin             | Ručně označí entry jako vyřízenou (např. opraveno mimo appku)         |

**Replay handlery** se registrují jako implementace `IDeadLetterReplayHandler` pro konkrétní `DeadLetterOperation`. Aktuálně podporované: **AiSummarizeTicket** (idempotentní — znovu sumarizuje ticket z DB). Ostatní typy (GitHubSyncProject, webhooky) jsou listable/dismissable — replay pro ně dnes vrátí 400, dokud někdo nenapíše handler (bezpečnější opt-in, protože webhooky nebývají idempotentní).

### Kdy se co použije

- Transientní chyba (502/503, rate-limit spike) → Polly retry, job pokračuje. `SyncLog` + `JobRun` ukazují úspěch.
- Chyba přežije retries → DLQ row, warning log, `SyncLog`/`JobRun` failed status.
- Admin v `/admin` klikne Replay → idempotentní operace se znovu pustí; pokud projde, entry se označí Replayed.

## External sync — Email-to-ticket

`EmailPollingService` (každé 2 min) čte nepřečtenou poštu z jedné sdílené schránky přes Microsoft Graph a převádí ji na tickety nebo komentáře. Disabled by default — zapnout přes `Sync:Email:Enabled = true` v konfiguraci.

### Routing

Email se přiřadí k projektu podle plus-aliasu v recipientech: `inbox+<key>@<your-domain>` se mapuje na projekt, který má `ExternalSystem = "Email"` a `ExternalProjectId = "<key>"` (case-insensitive). Prefix `inbox+` je konfigurovatelný (`Sync:Email:AliasPrefix`). Emaily bez matchingu se rovnou označí jako přečtené (nepouštět je pak znovu na log).

### Reply detection

Subject ve tvaru `[#<CODE>-<NUM>]` (např. `Re: [#ACME-42] ...`) přidá komentář k existujícímu ticketu místo vytvoření nového. Fallback: pokud ticket s tím číslem v daném projektu neexistuje, vytvoří se nový.

### Idempotence

Graph message id se ukládá do `Ticket.ExternalId` (pro nové tickety) nebo `Comment.ExternalId` se `Source = CommentSource.Email` (pro komentáře). Druhé spuštění stejného message id je no-op.

### Provisioning

1. V Entra ID založ App Registration (single-tenant). Přidej **application permission** `Mail.ReadWrite` (ne delegated) a admin-consent ji.
2. Vygeneruj client secret. Uložit do Azure App Service Configuration jako `Sync__Email__ClientSecret` (nikdy do repo).
3. (Volitelně) Omez aplikaci jen na konkrétní schránku přes `New-ApplicationAccessPolicy` v Exchange Online, ať app nevidí celý tenant.
4. Konfigurace: `TenantId`, `ClientId`, `ClientSecret`, `MailboxUserId` (UPN nebo Graph id sdílené schránky).
5. Per-projekt zapni nastavením `ExternalSystem = "Email"` + `ExternalProjectId = "<key>"`. Klienti pak píšou na `inbox+<key>@your-domain`.

SyncLog řádky (jeden per projekt s aktivitou) reflektují skutečnost — `ItemsSynced` = počet zpracovaných emailů, `Failed` = ty, které spadly v transientní chybě (zůstávají nepřečtené, příští tick je zkusí znovu).

## GitHub E2E flow

Projekty napojené na GitHub (přes OAuth v Project settings) mají v ticket detailu sekci **Linked pull requests** s tlačítkem **Create branch**.

### Konvence názvů větví

Create branch vytvoří ref `feat/<PROJECT_CODE>-<TICKET_NUMBER>-<slug>`, kde slug je lowercase-hyphenated výtah z titulku (max 40 znaků, non-alnum se kolabuje na pomlčky). Např. ticket **WEB-42 "Add structured logging"** → `feat/WEB-42-add-structured-logging`. Idempotentní: druhý klik při existující větvi vrátí odkaz bez duplicitní chyby.

### Ticket discovery z webhooku

Když dorazí `pull_request` webhook, hledáme `(<CODE>-<NUMBER>)` pattern v branch name, PR titulu a PR body — první match vyhrává. Takže commitní konvence jako „Fixes WEB-42" v PR popisu pracují stejně dobře jako formálně vygenerovaná větev.

### Status mapping (konvence, do budoucna konfigurovatelné)

| GitHub event                          | Přechod ticket stavu                                             |
| ------------------------------------- | ---------------------------------------------------------------- |
| `pull_request.opened`                 | První aktivní TaskState s názvem obsahujícím "review" (case-insensitive) |
| `pull_request.closed` + `merged=true` | První aktivní `TaskState.IsClosedState=true` (Done / Closed)     |
| `pull_request.closed` + merged=false  | Stav se nemění (linked PR se označí jako Closed)                |
| `pull_request.reopened`               | Stav se nemění                                                   |

Pokud se žádný odpovídající TaskState nenajde, přechod se tiše přeskočí a link na PR pořád vznikne v DB + v UI. Po-projektová konfigurace mapování je v plánu — zatím stačí pojmenovat stav "In Review" a nechat alespoň jeden `IsClosedState`.

### Setup GitHub webhook

V repozitáři nastavit webhook na `https://<api-host>/api/webhooks/github` s content-type `application/json`, secret = `Project.WebhookSecret`, a vybrat events **Issues**, **Issue comments**, **Pull requests**. Bez PR events nebudou Linked PRs fungovat.

### Endpointy

| Endpoint                                                                 | Kdo              | Co dělá                                               |
| ------------------------------------------------------------------------ | ---------------- | ----------------------------------------------------- |
| `POST /api/v1/projects/{id}/tickets/{id}/github/create-branch`           | Developer+       | Vytvoří větev podle konvence, idempotentně           |
| `GET  /api/v1/projects/{id}/tickets/{id}/github/pull-requests`           | Project member   | Seznam linked PRs (newest first)                      |
| `POST /api/webhooks/github` (`X-GitHub-Event: pull_request`)             | GitHub (HMAC)    | Upsertuje LinkedPullRequest + status transition       |

## AI audit + rate limit

Každé volání do Azure OpenAI jde přes `IAiInvocationRecorder`, který:

1. **Hashuje vstup** (SHA-256) — umožňuje dedup / detekci opakovaných volání na stejný obsah bez ukládání plného promptu.
2. **Rate-limit check** per uživatele — default 20 volání za 10 min (konfigurovatelné přes `Ai:RateLimit:CallsPerWindow` a `Ai:RateLimit:WindowMinutes`). Background joby (bez uživatele) rate-limit obchází — škrtí je jejich vlastní PeriodicTimer.
3. **Měří náklady** — `Ai:Pricing:InputPerMillionTokensUsd` (default $2.50) a `Ai:Pricing:OutputPerMillionTokensUsd` (default $10.00) pro gpt-4o. Přepsat v appsettings pokud máte jiný Azure commit.
4. **Zapíše `AiInvocation`** row: trigger, user, project/ticket scope, prompt/completion tokens, cost, první 1000 znaků výstupu, success + error, duration, reason (u manual re-run).

### Trigger typy

| Trigger              | Kdy se zapisuje                                                        |
| -------------------- | ---------------------------------------------------------------------- |
| `AutoSummarize`      | `AiSummarizationService` (každých 6h)                                  |
| `WeeklyReport`       | `WeeklyReportService` (pondělí 07:00 CET)                              |
| `ManualResummarize`  | `POST /tickets/{id}/ai/resummarize` (vyžaduje `reason` 3–500 znaků)   |
| `Replay`             | DLQ replay z `/admin`                                                   |

### Endpointy

| Endpoint                                                             | Kdo                | Co dělá                                                          |
| -------------------------------------------------------------------- | ------------------ | ---------------------------------------------------------------- |
| `GET  /api/v1/projects/{id}/tickets/{id}/ai/invocations`             | Project member     | AI history ticketu (newest first)                                |
| `POST /api/v1/projects/{id}/tickets/{id}/ai/resummarize`             | Developer+         | Manuální re-run s povinným `reason`. Rate-limit → 429.          |
| `GET  /api/v1/admin/ai-usage?days=30`                                | Admin              | Agregace tokenů + nákladů per projekt za zvolené období        |

### UI

- **Ticket detail** má sekci **AI history** s chronologickým seznamem volání (trigger, tokens, cost, důvod) a **Re-summarize** tlačítkem otevírajícím dialog s povinným polem "Reason".
- **/admin** má sekci **AI usage** s window picker (7/30/90 dnů) a tabulkou projektů podle celkových nákladů.

### Rate-limit chování

Při překročení limitu vrací endpoint **HTTP 429** s `ErrorResponse.Message` vysvětlujícím důvod. FE toast ukáže přesnou hlášku. Background joby rate-limit necítí (nemají `TriggeredByUserId`).

## EasyProject migrace — validace + resume

### Validate před spuštěním

`POST /api/v1/migration/validate` (Admin-only) přijme `baseUrl`, `apiKey`, `projectIds` a vrátí `MigrationValidationResult`:

- **`credentialsValid`** — výsledek `TestConnectionAsync` proti EP API
- **`epProjectCount`** — kolik projektů API vidí s daným klíčem
- **`selectedProjects`** — preview (název + `alreadyMigrated=true/false`); vazba na existující SP project přes `ExternalProjectId`
- **`issues`** — seznam `Blocking` / `Warning` hlášek (neexistující project id, chybné creds, kolize na SP straně)

Wizard může Validate volat před Step Review, aby uživatel zahlédl kolize ještě před skutečným Start.

### Resume failed / cancelled job

Každý běh migrace ukládá po dokončení **každé fáze** (Fetching → Lookups → Users → Projects → Tickets → Comments → Worklogs → CustomFields → Checklists → Attachments → Recalculating → Done) hodnotu `MigrationJob.CurrentPhase` do DB. Když proces spadne uprostřed, status přejde na `Failed` a `CurrentPhase` zůstává na poslední **dokončené** fázi — admin pak přes UI pokračuje od přesně tohoto místa.

V `/admin/migration` historii klikne admin **Resume** u Failed / Cancelled / CompletedWithErrors jobu, zadá API key (secret se záměrně neukládá na job-level — je to rotovatelný token), a endpoint `POST /api/v1/migration/{jobId}/resume` znovu spustí `EasyProjectMigrationService.ExecuteAsync` s uloženou `Configuration`.

Fáze, které už proběhly, jsou **idempotentní** díky ExternalId upsertu (projekty přes `ExternalProjectId`, tickety přes `ExternalId`, komentáře / worklogy / přílohy přes `(ExternalId, Source)`) — znovu-běh je bezpečný; typicky jen update existujících záznamů místo insertu.

Resumovat nejde jobs ve stavu `Pending` / `Running` / `Completed` — endpoint vrátí 400. Bez `Configuration` v DB (starší historie) taky 400. Prázdný API key v body → 400 přes FluentValidation.

### Co se ukládá do `Configuration`

`StoredMigrationConfig` (`Application/Features/Migration/EasyProject/StoredMigrationConfig.cs`) — celý `StartMigrationCommand` minus `ApiKey`. Na staré joby (před #17) se Resume nepoužije — chybí jim strukturovaný tvar.

### Endpointy

| Endpoint                                        | Kdo    | Co dělá                                                            |
| ----------------------------------------------- | ------ | ------------------------------------------------------------------ |
| `POST /api/v1/migration/validate`               | Admin  | Pre-flight kontrola credentials + project collision                |
| `POST /api/v1/migration/{jobId}/resume`         | Admin  | Znovu spustí Failed/Cancelled/CompletedWithErrors job s novým API key |

## MCP server — sdílené CQRS kontrakty s WebApi

`SoftimProject.McpServer` je samostatný ASP.NET Core proces pro LLM-driven tooling (AI agenty, kteří chtějí pracovat s projektem přes HTTP). Od #18 **každý tool endpoint volá stejný MediatR handler** jako WebApi — žádná duplicitní query logika, žádné samostatné DTO tvary.

### Co to znamená prakticky

- **Jedna zdrojová pravda pro autorizaci** — `IRequireProjectAccess`, `IRequireProjectRole`, `IRequireRole` fires přes `AuthorizationBehavior` i na MCP cestě. MCP tool nemusí (a nesmí) duplikovat `HasProjectAccessAsync` check — pipeline to udělá.
- **Jedna zdrojová pravda pro DTO** — MCP vrací `PagedResult<ProjectDto>`, `PagedResult<TicketListItemDto>`, `TicketDetailDto`, `PagedResult<WorklogDto>`. Anonymous objekty byly odstraněny.
- **Sdílená observability** — volání přes MCP tools tvoří stejné audit artefakty (JobRun, AiInvocation, SyncLog) jako volání ze WebApi.
- **Bezpečnost** — `/tools/tickets/{ticketId}` bez `projectId` bylo odstraněno: obcházelo `IRequireProjectAccess`. Náhradní endpoint je scoped k projektu.

### Tool katalog

| Tool                                                           | MediatR handler                   | Guard                            |
| -------------------------------------------------------------- | --------------------------------- | -------------------------------- |
| `GET  /tools/projects`                                         | `GetProjectsQuery`                | Auth only                        |
| `GET  /tools/projects/{projectId}/tickets`                     | `GetTicketsQuery`                 | `IRequireProjectAccess`          |
| `GET  /tools/projects/{projectId}/tickets/{ticketId}`          | `GetTicketByIdQuery`              | `IRequireProjectAccess`          |
| `POST /tools/worklogs`                                         | `CreateWorklogCommand`            | `IRequireProjectRole(Developer)` |
| `GET  /tools/projects/{projectId}/worklogs`                    | `GetWorklogsQuery`                | Auth only (filter per project)   |

### DI parita s WebApi

MCP process nyní registruje **`AddApplicationServices()` + `AddInfrastructureServices(Configuration)`** — stejně jako WebApi. Background services, Polly pipelines, DLQ recorder, AI invocation recorder běží i v MCP procesu. Pokud to bude do budoucna příliš, cesta je rozdělit Infrastructure DI na `AddHostedServicesOnly` / `AddApiOnly`.

### Breaking change v tool API

Před #18 existoval `GET /tools/tickets/{ticketId}` — tento endpoint je **odstraněn**. Klienti přecházejí na `GET /tools/projects/{projectId}/tickets/{ticketId}`. Změna je žádoucí: původní route vyžadoval, aby MCP tool sám implementoval access check (což dělal, ale až po načtení ticketu — informační únik přes timing/404-vs-403 rozlišení), nová route volá handler s plným `IRequireProjectAccess` guardem.

## Související dokumenty

- `REPOSITORY_ANALYSIS.md` – analýza stavu projektu, silné a slabé stránky.
- `REFACTORING_REVIEW.md` – shrnutí provedených refaktoringů a zbylé technické dluhy.
- `IMPLEMENTATION_BACKLOG.md` – plán dalších iterací (stabilizace, funkční dotažení, provozní kvalita).
- `assignment/softim_easy_swagger.yml` – swagger definice Easy Projectu (zdroj pro migration modul).
