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

## Související dokumenty

- `REPOSITORY_ANALYSIS.md` – analýza stavu projektu, silné a slabé stránky.
- `REFACTORING_REVIEW.md` – shrnutí provedených refaktoringů a zbylé technické dluhy.
- `IMPLEMENTATION_BACKLOG.md` – plán dalších iterací (stabilizace, funkční dotažení, provozní kvalita).
- `assignment/softim_easy_swagger.yml` – swagger definice Easy Projectu (zdroj pro migration modul).
