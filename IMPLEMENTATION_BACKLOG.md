# Implementační backlog pro dotažení SoftimProject

> **Historický snapshot (2026-04-23).** Aktivní fronta prací je od této chvíle v [GitHub Issues](https://github.com/softim-cz/softimproject/issues) a v [GitHub Projectu](https://github.com/softim-cz/softimproject/projects) repozitáře. Issues jsou oštítkovány `phase-1..4`, `effort: S/M/L` a přiřazeny k milestones "Fáze 1–4". Nová rozhodnutí a priority patří tam, ne do tohoto souboru — tak se vyhneme duplicitě.

## Stav po této iteraci

Hotové v této fázi:

- uzamčení MCP serveru přes Entra/JWT autentizaci a project access kontrolu,
- sjednocení ownership validace pro komentáře, přílohy, checklisty a worklogy,
- ochrana AI sumarizace proti ukládání placeholder hodnot,
- korekce role logiky u worklogů,
- efektivnější export bez plného materializování entit do paměti,
- server-side filtrování ticket listu pro běžné filtry,
- oprava načítání uložené konfigurace task listu,
- odstranění kritických frontend lint chyb a několika render/effect anti-patternů,
- srovnání hlavních DTO kontraktů mezi backendem a frontendem pro tickety, komentáře a worklogy,
- doplnění základních regresních testů kolem ownership a AI fallbacku.

## Další priority

### Fáze 1: Release stabilization

- odstranit zbývající frontend warnings, hlavně nepoužité importy a několik zbytečných hook warningů,
- doplnit explicitní build/test skripty pro lokální i CI použití,
- doplnit root `README.md` s setupem, env proměnnými a pořadím spuštění služeb,
- doplnit seed/development data pro rychlé lokální ověření hlavních toků.

### Fáze 2: Funkční dotažení produktu

- dokončit dashboard o skutečné agregace a relevantní metriky,
- doplnit editaci/správu komentářů, checklistů a worklogů přímo ve UI,
- sjednotit exporty s aktivními filtry a uživatelskými pohledy,
- doplnit nahrávání příloh do detailu ticketu na frontendu,
- dotáhnout klientský portál a jeho oprávnění / maskování interních dat.

### Fáze 3: Provozní kvalita

- přidat integrační testy pro API authorization boundary a nested resource access,
- doplnit monitoring a strukturované logování pro sync a background jobs,
- doplnit retry / dead-letter strategii pro integrační background služby,
- přidat paging pro větší seznamy ticketů, komentářů a worklogů.

### Fáze 4: Produktové rozšíření

- dotáhnout GitHub flow do plně použitelného end-to-end scénáře,
- doplnit lepší AI workflow pro reporty a sumarizace s auditovatelným triggerem,
- doplnit importy a migraci z Easy Projectu o validace a resumable behavior,
- zvážit sjednocení MCP a Web API nad sdílenými aplikačními command/query kontrakty.

## Doporučené pořadí nasazení

1. Nasadit backend security a ownership fixy.
2. Ověřit hlavní UI toky na stagingu.
3. Doplnit chybějící dokumentaci a CI gate.
4. Teprve potom rozšiřovat scope o další integrace a AI automatizaci.
