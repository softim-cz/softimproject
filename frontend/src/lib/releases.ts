// Curated changelog rendered on /releases and linked from the footer version.
// Newest first. Bump frontend/package.json `version` to match the top entry when releasing.
// Content is kept in Czech (the team's language); page chrome is localized via next-intl.

export interface ReleaseEntry {
  version: string;
  date: string; // dd. mm. yyyy
  headline: string;
  added?: string[];
  changed?: string[];
  fixed?: string[];
}

export const releases: ReleaseEntry[] = [
  {
    version: "0.2.8",
    date: "1. 7. 2026",
    headline: "Editace projektu v Administraci",
    changed: [
      "Nastavení projektu (obecné údaje, členové, GitHub, nástěnka, vlastní pole, smazání) se přesunulo do Administrace / Projekty pod akci Upravit.",
      "Z projektu zmizela záložka Nastavení – správa projektu je nyní v Administraci.",
    ],
  },
  {
    version: "0.2.7",
    date: "30. 6. 2026",
    headline: "Vytváření projektů jen v Administraci",
    changed: [
      "Z Přehledu (dashboardu) zmizelo tlačítko Nový projekt – projekty se zakládají v Administraci / Projekty.",
    ],
  },
  {
    version: "0.2.6",
    date: "30. 6. 2026",
    headline: "Projekty a Firmy v Administraci",
    added: [
      "Administrace má nové sekce Projekty (vytváření a mazání projektů včetně obsahu) a Firmy.",
    ],
    changed: [
      "Firmy se přesunuly z Číselníků do samostatné sekce Administrace.",
      "Tlačítko Nový projekt se přesunulo z Přehledu do Administrace / Projekty.",
    ],
  },
  {
    version: "0.2.5",
    date: "30. 6. 2026",
    headline: "Úplné smazání projektu",
    fixed: [
      "Smazání projektu nově odstraní i všechny jeho úkoly, komentáře, výkazy, checklisty a nástěnky – dříve selhalo u projektu s obsahem.",
    ],
  },
  {
    version: "0.2.4",
    date: "30. 6. 2026",
    headline: "Oprava importu vlastních polí v migraci",
    fixed: [
      "Migrace už neselže na duplicitní hodnotě vlastního pole, když má úkol dvě pole stejného názvu – hodnoty se sloučí do jedné.",
      "Vlastní pole se ukládají po jednom úkolu; jeden vadný řádek neshodí celou dávku a do logu se propíše skutečná příčina.",
    ],
  },
  {
    version: "0.2.3",
    date: "30. 6. 2026",
    headline: "Spolehlivější import komentářů a výkazů v migraci",
    fixed: [
      "Migrace ukládá komentáře a výkazy po jednom – jeden vadný řádek už neshodí celou dávku a do logu se propíše skutečná příčina chyby.",
      "Dlouhá poznámka u výkazu se ořízne na povolenou délku místo selhání importu.",
    ],
  },
  {
    version: "0.2.2",
    date: "30. 6. 2026",
    headline: "Průběžný stav stahování v průvodci migrace",
    changed: [
      "Průvodce migrace hlásí průběh stahování (stahuji detaily X/Y úkolů, po projektech) – během dlouhého stahování z EasyProjectu už nevypadá zaseklý.",
    ],
  },
  {
    version: "0.2.1",
    date: "30. 6. 2026",
    headline: "Zprovoznění migrace z EasyProjectu",
    added: [
      "Výběr dříve uloženého připojení v průvodci migrace – URL a API klíč se po otestování zapamatují.",
      "Šablona projektu se zobrazuje v detailu projektu (Nastavení).",
    ],
    fixed: [
      "Migrace přiděluje ticketu pořadové číslo a izoluje selhání po jednotlivých ticketech (odstranění hromadných chyb při ukládání).",
      "Import číselných hodnot vlastních polí z EasyProjectu (possible_values) už nepadá na deserializaci.",
    ],
  },
  {
    version: "0.2.0",
    date: "28. 6. 2026",
    headline: "Patička s verzí a přehled releases",
    added: [
      "Patička aplikace s verzí, číslem commitu a odkazem na přehled novinek.",
      "Stránka „Co je nového“ (/releases) s časovou osou verzí.",
    ],
  },
  {
    version: "0.1.5",
    date: "28. 6. 2026",
    headline: "Integrace zákaznických systémů (EasyProject, Jira, Redmine)",
    added: [
      "Jednorázový i automatický inkrementální import projektů, úkolů a výkazů ze zdrojových systémů.",
      "Plánovaná synchronizace (denně až po 1 h) a webhooky pro téměř okamžité aktualizace.",
      "Konektory pro EasyProject, Jira a Redmine přes sdílený kanonický model.",
    ],
    changed: [
      "Importované projekty lze navázat na zákazníka (firmu); přihlašovací údaje zdroje jsou šifrované.",
    ],
  },
  {
    version: "0.1.4",
    date: "26. 6. 2026",
    headline: "Sledování ticketu a vylepšení detailu",
    added: ["Příznak Sleduji/Nesleduji na ticketu."],
    changed: [
      "Sjednocená barevnost napříč firemními aplikacemi.",
      "Detail ticketu: HTML popis, AI souhrn respektuje světlý/tmavý režim, plynulá šířka.",
    ],
    fixed: ["Smyčka přihlašování na produkci (opakované načítání dashboardu)."],
  },
];
