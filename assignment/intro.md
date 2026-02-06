## **Zadání projektu: SoftimProject**

**Cíl:** Vytvořit interní, vysoce efektivní nástroj pro řízení projektů, který bude sloužit jako centrální "hub" pro správu tiketů, času a komunikace, a to i v případě, že klientská strana využívá jiné systémy.

### **1. Vizuální identita a UX**

Systém musí odrážet identitu **https://softim.cz**.

Zjisti si logo a barevnou paletu.

---

### **2. Autentizace a Správa uživatelů**

* **MS Entra ID (Azure AD):** Plná integrace pomocí SSO. Uživatelé se nepřihlašují heslem, ale firemním Microsoft účtem.
* **Permissions (Oprávnění):** * Matice oprávnění postavená na rolích (Admin, Project Manager, Developer, Guest/Client).
* Možnost definovat viditelnost na úrovni jednotlivých projektů (Uživatel X vidí pouze projekt Y).
* *Filozofie:* "Co nemusím vidět, to mě neruší."

---

### **3. Jádro systému: Projekty a Kanban**

* **Dashboard:** Přehled aktivních projektů s indikátory zdraví (včasnost, čerpání budgetu).
* **Kanban Board:** Intuitivní drag-and-drop rozhraní. Možnost customizace sloupců (To Do, In Progress, Review, Done).
* **Detail tiketu:** Popis (Markdown podpora), přílohy, check-listy a vlákno komentářů.

---

### **4. Integrační vrstva (Agregátor)**

Tohle je "mozek" systému. SoftimProject nebude jen izolovaný ostrov.

* **One-way Sync:** Pravidelné stahování dat z externích systémů (Jira, Redmine) přes API.
* **Synchronizace:** Pokud se v Redminu změní stav tiketu nebo přibude komentář, SoftimProject to okamžitě (nebo v krátkém intervalu) reflektuje.
* **E-mail Integration:** Možnost vytvořit tiket zasláním e-mailu na specifickou adresu projektu nebo automatické párování odpovědí do komentářů.

---

### **5. AI Inteligence (LLM Layer)**

Implementace pomocí OpenAI API nebo lokálního Llama modelu.

* **Reportování (Project Level):** Generování týdenních/měsíčních reportů. AI projde všechny změny, uzavřené tikety a komentáře a vytvoří lidsky čitelný souhrn: *"V lednu jsme dokončili migraci databáze, ale zasekli jsme se na integraci platební brány kvůli chybějící dokumentaci od klienta."*
* **Summarizace tiketu:** Tlačítko "Shrnout stav", které z 50 komentářů v tiketu vytáhne aktuální status a další kroky (Next Steps).
  * Sumarizovat by to mělo i průběžně po každém novém tiketu. Mělo by se i idenitifkovat zdali se čeká na zákazníka nebo vývojáře. Teoreticky by se na projektu mohlo i povolit tlačítko, zdali dovolit LLM automaticky přepínat stavy tiketů na základě obsahu v komentáři.

---

### **6. Worklogy a Tracking**

* **Zapisování:** Extrémně rychlé zadávání (např. pomocí klávesových zkratek nebo "Quick log" okna).
* **Importy:** * Podpora CSV/Excel pro hromadné nahrávání.
* **MCP Server (Model Context Protocol):** Umožní AI asistentům (jako jsem já nebo Claude) přímo zapisovat worklogy do vašeho systému na základě konverzace nebo analýzy kódu.
* **Public API:** Pro napojení externích trackerů (Toggl apod.).

---

### **7. Návrhy na rozšíření (Co by se vám mohlo hodit)**

* **Plánování kapacit (Resource Heatmap):** Jednoduchý vizuální přehled, kdo je přetížený a kdo má volno. To v EasyProjectu bývá často příliš složité, u vás by stačil "semafor".
* **Klientský přístup (Light UI):** Možnost nasdílet klientovi unikátní link, kde uvidí pouze kanban a stav čerpání hodin (bez interních komentářů).
* **Automatická fakturace:** Propojení worklogů s hodinovými sazbami projektu a generování podkladů pro fakturaci jedním kliknutím.
* **Teams Notifikace:** Inteligentní upozornění na blížící se deadliny nebo urgentní tikety přímo do chatu.


Technický stack:
Next.js na frontend
TanStackQuery
React hook form na formuláře
Zod na validaci schémat
Zustand

Backend .NET 10
MSSQL DB

Nasazeno to bude na Azure v rámci app service