# Contributing to SoftimProject

## Development standards

Odsouhlaseno Karlem a Janem 2026-04-16. Platí pro všechny, kdo pushují do `main`.

### 1. Pre-push validace

Každý `git push` musí nejprve proběhnout lokálně přes plný build.

- **Frontend:** `cd frontend && npm run build`
- **Backend:** `cd backend && dotnet build -c Release`

Oba musí projít bez chyb. Git `pre-push` hook to vynucuje automaticky — po klonu repa spusťte jednorázově:

```bash
bash scripts/install-hooks.sh
```

Hook se registruje jako `core.hooksPath = .githooks` a pouští se před každým `git push`. Obejít (zřídka, když opravdu víte proč) lze přes `git push --no-verify`.

### 2. EF migrace se aplikují automaticky při startu API

Nepouštějte `dotnet ef database update` ani ruční SQL proti produkční DB. Vše přes EF migrace:

1. Upravte model v `SoftimProject.Domain` a `SoftimProject.Infrastructure`.
2. `dotnet ef migrations add <NameInPascalCase>` v `SoftimProject.Infrastructure`.
3. Commit + push. API na startu zavolá `Database.Migrate()` a migraci aplikuje.

U destruktivních migrací (drop column, backfill, přeškolení schématu) přidejte do migrace bezpečnostní guardy (IF EXISTS, data backfill) a zkoordinujte s týmem.

### 3. Env vars vždy v `.env.example`

Kdykoli přidáváte nový env var / app setting, doplňte ho do:

- `frontend/.env.example`
- `backend/src/SoftimProject.WebApi/appsettings.Example.json`

Nikdy nekomitujte reálné hodnoty (hesla, tokeny, connection stringy). Produkční hodnoty jsou v Azure App Service → Configuration, ne v repu.

### 4. `main` zůstává zelený

GitHub branch protection vyžaduje passing CI před přijetím pushe. I přímý push do `main` je odmítnut, pokud poslední workflow skončil červeně. Nejdřív opravit CI, pak pokračovat.

### 5. Konzistentní formátování

- **Frontend:** `npm run lint` + `npm run format` (Prettier) před commitem.
- **Backend:** `dotnet format` před commitem.
- **Oba:** `.editorconfig` řídí whitespace a encoding.

### 6. Konvenční commit zprávy

Formát: `type(scope): subject`

Typy: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `perf`, `build`, `ci`, `style`.

Subject pod 72 znaků, imperativ, bez tečky na konci. Tělo volitelné.

Příklady:

```
fix(frontend): resolve TypeScript errors blocking deploy
feat(backend): add GitHub sync service
chore(deps): pin System.Security.Cryptography.Xml to 10.0.6
```

---

## Outstanding setup

Tyto položky ještě nejsou zavedené, dokud nebudou hotové, je pravidlo "best effort":

- [x] Pre-push git hook — spustí `npm run build` + `dotnet build` (bod 1)
- [x] `Database.Migrate()` v `Program.cs` v `using var scope` bloku (bod 2)
- [ ] `frontend/.env.example` + `backend/src/SoftimProject.WebApi/appsettings.Example.json` (bod 3)
- [ ] GitHub branch protection na `main` s "require status checks to pass" (bod 4)
- [ ] `.editorconfig` v kořeni + Prettier config + lint skript ve `frontend/package.json` (bod 5)

Bod 6 (konvenční commity) je zavedený ode dneška — Honza už to tak většinou dělá, Karel navázal.
