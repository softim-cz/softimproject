# SoftimProject

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

## První spuštění (lokálně)

```bash
# 1) Backend – obnovení, build, migrace DB, spuštění API
cd backend
dotnet restore
dotnet build
dotnet ef database update \
  --project src/SoftimProject.Infrastructure \
  --startup-project src/SoftimProject.WebApi
dotnet run --project src/SoftimProject.WebApi
# API běží na http://localhost:5249 (Swagger: /swagger)

# 2) Frontend – v novém terminálu
cd frontend
npm install
npm run dev
# Frontend běží na http://localhost:3000

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

## Související dokumenty

- `REPOSITORY_ANALYSIS.md` – analýza stavu projektu, silné a slabé stránky.
- `REFACTORING_REVIEW.md` – shrnutí provedených refaktoringů a zbylé technické dluhy.
- `IMPLEMENTATION_BACKLOG.md` – plán dalších iterací (stabilizace, funkční dotažení, provozní kvalita).
- `assignment/softim_easy_swagger.yml` – swagger definice Easy Projectu (zdroj pro migration modul).
