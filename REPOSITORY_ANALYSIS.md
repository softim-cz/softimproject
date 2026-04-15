# Analýza repozitáře `softimproject`

## O čem projekt je

Projekt `SoftimProject` je interní nástroj pro řízení projektů a servisních aktivit. Z přiloženého zadání vyplývá, že cílem je vytvořit centrální hub pro:

- správu projektů, tiketů a kanban boardů,
- evidenci worklogů a výkazů času,
- komentáře, přílohy a klientský portál,
- synchronizaci s externími systémy typu Jira, Redmine, GitHub a Easy Project,
- AI sumarizaci tiketů a generování reportů,
- napojení na MS Entra ID a role-based oprávnění,
- MCP server pro AI asistenty, kteří mají umět číst data a zapisovat worklogy.

Aktuální stav repozitáře potvrzuje, že to není jen prototyp jedné obrazovky, ale poměrně široce pojatý full-stack systém:

- `frontend/` obsahuje Next.js aplikaci s dashboardem, přehledem projektů, task listem, boardem, worklogy, administrací a klientským portálem.
- `backend/src/SoftimProject.WebApi/` poskytuje hlavní REST API, SignalR huby a autentizaci přes Microsoft Identity.
- `backend/src/SoftimProject.McpServer/` je samostatný MCP-orientovaný server pro AI nástroje.
- backend je rozdělen do vrstev `Domain`, `Application`, `Infrastructure`, `WebApi`, což odpovídá clean architecture / CQRS přístupu.

## Silné stránky

- Jasně definovaná produktová vize. Zadání je konkrétní a dobře popisuje, co má systém řešit: interní projektový management, migrace z Easy Projectu, integrace a AI vrstvu.
- Dobře zvolený stack. Next.js + TanStack Query + Zod + Zustand na frontendu a .NET + EF Core + MediatR na backendu je rozumná kombinace pro tento typ aplikace.
- Solidní architektonické rozdělení backendu. Oddělení na `Domain`, `Application`, `Infrastructure` a `WebApi` zlepšuje udržovatelnost a čitelnost.
- CQRS a pipeline chování. V `Application` jsou použity MediatR handlery a cross-cutting pipeline pro logování, validaci a autorizaci.
- Široký funkční záběr už v kódu. Repozitář už pokrývá projekty, tickety, komentáře, worklogy, přílohy, notifikace, exporty, kanban, migraci z Easy Projectu a GitHub integraci.
- Reálná podpora integrací. Jsou zde background služby pro Jira, Redmine, GitHub, e-mail polling, AI sumarizaci i přepočet health indikátorů.
- Zabudované realtime prvky. SignalR huby pro kanban, notifikace a migraci dávají smysl pro živou spolupráci.
- MCP server je dobrý strategický tah. Pokud má být systém použitelný i pro AI asistenty, oddělený server s jednoduchými nástroji pro projekty, tickety a worklogy je praktický.
- Frontend není jen skelet navigace. Dashboard, task list s konfigurovatelnými sloupci, filtry, exportem a preview sidebarem ukazují, že část UX už je reálně rozpracovaná.
- Myslí se na provozní aspekty. V API jsou health checks, Serilog, Swagger/OpenAPI a základní CORS konfigurace.

## Slabé stránky

- Dokumentace repozitáře je zatím slabá. Chybí skutečný root `README`, frontend má stále výchozí README z `create-next-app`, takže onboarding nového vývojáře bude zbytečně pomalý.
- Projekt působí jako široký MVP, ne jako dotažený produkt. Záběr je velký, ale některé části vypadají spíš jako rozestavěná kostra než plně dokončené workflow.
- Testovací vrstva je nedotažená. Testovací projekty existují, ale v `backend/tests` jsem nenašel žádné testovací `.cs` soubory, takže aktuální krytí chování je pravděpodobně nulové nebo téměř nulové.
- Frontend má známky nedokončenosti. Například dashboard zobrazuje placeholdery `--` místo skutečných agregovaných metrik, i když zbytek stránky už data načítá.
- V projektu je vysoká komplexita na rozsah týmu/MVP. Integrace s více externími systémy, AI vrstva, migrace, klientský portál, exporty a realtime komunikace současně výrazně zvyšují riziko nedodělků a regresí.
- Konfigurace a tajemství nejsou produktově dotažené. `appsettings.json` obsahuje jen prázdné placeholdery pro Azure AD, DB, blob storage, Azure OpenAI a GitHub; bez jasného setupu je nasazení i lokální spuštění křehké.
- MCP server obchází část hlavního aplikačního stacku. Je jednoduchý a účelný, ale zapisuje worklogy přímo přes DB context, takže část business pravidel může být duplicitní nebo nekonzistentní vůči hlavnímu API.
- Frontend místy používá těžší klientský stav a callback logiku i tam, kde bude potřeba disciplinovaně hlídat složitost. Task list je funkčně bohatý, ale postupně může být hůř udržovatelný bez dobrých komponentových hranic a testů.
- V repozitáři jsou přibalené artefakty typu `.next/` a `deploy.zip`, což obvykle není ideální. Naznačuje to slabší hygienu repozitáře a zbytečný šum.

## Celkové zhodnocení

`SoftimProject` je ambiciózní interní projektový a servisní systém se silnou produktovou logikou a nadprůměrně širokým funkčním záběrem. Největší plus je, že vize není abstraktní a v kódu už je vidět reálná implementace důležitých modulů i rozumná architektura backendu.

Největší riziko není ve stacku, ale v rozsahu a dotaženosti. Projekt vypadá jako slibně rozpracované MVP, které má dobré základy, ale ještě potřebuje:

- doplnit dokumentaci a způsob lokálního spuštění,
- výrazně posílit testy,
- zredukovat nebo prioritizovat scope,
- sjednotit business pravidla mezi Web API a MCP serverem,
- dotáhnout UX a provozní připravenost u nejdůležitějších toků.

## Praktický závěr

Pokud bych to shrnul jednou větou: jde o dobře navržený základ interního project management hubu pro Softim, který má nadstandardní integrační a AI ambice, ale zatím působí spíš jako široce rozestavěná platforma než hotový produkční systém.
