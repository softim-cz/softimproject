must have bez kterého nemůžeme být:
Uživatelé - stačí základní info z MS Entra + aplikační role a práva
Role a práva
ID MS Entra přes emails, zobrazení z Entra Jméno, Příjmení, firemní role, Firma
Aplikační role - viz číselník
Nastavení práv přes Aplikační role na Projekty, Sledování času, Výkazy (vše CRUD), přes úkoly a typy úkolů to není nutné
Číselníky (vzít z EP):
Firmy / Zákazníci (Název)
Aplikační role
Typy projektů
Stavy projektů
Typy úkolů
Stavy úkolů
Systémové a uživatelské filtry pro všechny přehledy, zejména úkoly, worklogy (nemusí to ale být číselník)
Filtry:
podle všech dostupných parametrů (projekt, úkol, worklog, řazení, seskupování
Projekty: 
hlavička projektu - ID projektu, Název, Firma / Zákazník, Typ projektu, Stav projektu, ID nadřazeného projektu (ideálně plus povolené typy úkolů), Popis
pohledy - Kanban, Přehled úkolů (seznam), Worklogs (filtr na projekt a podprojekty)
Úkoly / Tickety
hlavička úkolu - ID úkolu, Název Ticketu, Popis / Description (ideálně nějaký markdown s formátováním a zobrazením obrázků), Stav, Priorita, Přižazeno / Uživatel, (Autor), Datum/Čas založení, Předepsaná doba, Kumulativní odpracovaný čas (z úkolu a podúkolů), Typ úkolu, Projekt (ID), Externí projekt, Externí ticket, Externí budget, Externí ID, Externí uživatel, Poznámka k realizaci, Poslední komentář (automaticky doplňované při navázání nového komentáře), ID nadřízeného úkolu, AI souhrn
Vazby přes ID úkolu - Komentáře, Worklogy, navázané úkoly (podřízené)
Kanban - swimlane podle atributů úkolů, vizuální změna stavů přesunem myší, plně konfigurovatelný obsah bloku úkolu
Přehled úkolů (seznam) - konfigurovatelné zobrazení sloupců, nastavitelná šíře sloupců (se zapamatováním stavu), vodorovné i svislé posuvníky vizuálně, plnohodnotné filtry viz filtrace, export sestavy viz exporty
Náhledy úkolů ideálně v pravém sloupci, ne modálně jaku u EP
Worklogs / Timesheets
hlavička záznamu - ID ticketu, ID uživatele, Počet hodin, Datum workologu, Komentář, AI souhrn, K vyúčtování (A/N), Fakturováno (text)
Komentáře
hlavička komentáře - Datum, ID uživatele, Externí uživatel, Popis / text komentáře, Interní (A/N)
k projektům
úkolům / ticketům
Přílohy / soubory
k úkolům
Docházka (není must have)
Exporty
stačí xlsx, položková konfigurace výstupu ideálně podle projektu, filtrování viz filtry