using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBilingualLookupNamesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotentní doplnění CS/EN názvů pro známé seedované hodnoty.
            // V1 migrace někdy nezasáhla produkční řádky (jiné spelling/casing).
            // Tady používáme LTRIM/RTRIM + case-insensitive porovnání a guard
            // `NameCs IS NULL` / `NameEn IS NULL`, takže nikdy nepřepíše existující hodnotu.
            migrationBuilder.Sql(@"
                UPDATE TicketPriorities SET NameCs = N'Nízká'    WHERE LOWER(LTRIM(RTRIM(Name))) = 'low'      AND NameCs IS NULL;
                UPDATE TicketPriorities SET NameEn = N'Low'      WHERE LOWER(LTRIM(RTRIM(Name))) = 'low'      AND NameEn IS NULL;
                UPDATE TicketPriorities SET NameCs = N'Střední'  WHERE LOWER(LTRIM(RTRIM(Name))) = 'medium'   AND NameCs IS NULL;
                UPDATE TicketPriorities SET NameEn = N'Medium'   WHERE LOWER(LTRIM(RTRIM(Name))) = 'medium'   AND NameEn IS NULL;
                UPDATE TicketPriorities SET NameCs = N'Vysoká'   WHERE LOWER(LTRIM(RTRIM(Name))) = 'high'     AND NameCs IS NULL;
                UPDATE TicketPriorities SET NameEn = N'High'     WHERE LOWER(LTRIM(RTRIM(Name))) = 'high'     AND NameEn IS NULL;
                UPDATE TicketPriorities SET NameCs = N'Kritická' WHERE LOWER(LTRIM(RTRIM(Name))) = 'critical' AND NameCs IS NULL;
                UPDATE TicketPriorities SET NameEn = N'Critical' WHERE LOWER(LTRIM(RTRIM(Name))) = 'critical' AND NameEn IS NULL;

                UPDATE TaskStates SET NameCs = N'Backlog'     WHERE LOWER(LTRIM(RTRIM(Name))) = 'backlog'                          AND NameCs IS NULL;
                UPDATE TaskStates SET NameEn = N'Backlog'     WHERE LOWER(LTRIM(RTRIM(Name))) = 'backlog'                          AND NameEn IS NULL;
                UPDATE TaskStates SET NameCs = N'K řešení'    WHERE LOWER(LTRIM(RTRIM(Name))) IN ('todo', 'to do', 'k řešení')      AND NameCs IS NULL;
                UPDATE TaskStates SET NameEn = N'Todo'        WHERE LOWER(LTRIM(RTRIM(Name))) IN ('todo', 'to do', 'k řešení')      AND NameEn IS NULL;
                UPDATE TaskStates SET NameCs = N'Probíhá'     WHERE LOWER(LTRIM(RTRIM(Name))) IN ('inprogress', 'in progress', 'probíhá') AND NameCs IS NULL;
                UPDATE TaskStates SET NameEn = N'In Progress' WHERE LOWER(LTRIM(RTRIM(Name))) IN ('inprogress', 'in progress', 'probíhá') AND NameEn IS NULL;
                UPDATE TaskStates SET NameCs = N'Ke kontrole' WHERE LOWER(LTRIM(RTRIM(Name))) IN ('review', 'ke kontrole')         AND NameCs IS NULL;
                UPDATE TaskStates SET NameEn = N'Review'      WHERE LOWER(LTRIM(RTRIM(Name))) IN ('review', 'ke kontrole')         AND NameEn IS NULL;
                UPDATE TaskStates SET NameCs = N'Hotovo'      WHERE LOWER(LTRIM(RTRIM(Name))) IN ('done', 'hotovo')                AND NameCs IS NULL;
                UPDATE TaskStates SET NameEn = N'Done'        WHERE LOWER(LTRIM(RTRIM(Name))) IN ('done', 'hotovo')                AND NameEn IS NULL;
                UPDATE TaskStates SET NameCs = N'Uzavřeno'    WHERE LOWER(LTRIM(RTRIM(Name))) IN ('closed', 'uzavřeno')            AND NameCs IS NULL;
                UPDATE TaskStates SET NameEn = N'Closed'      WHERE LOWER(LTRIM(RTRIM(Name))) IN ('closed', 'uzavřeno')            AND NameEn IS NULL;

                UPDATE ProjectStates SET NameCs = N'Aktivní'    WHERE LOWER(LTRIM(RTRIM(Name))) = 'active'    AND NameCs IS NULL;
                UPDATE ProjectStates SET NameEn = N'Active'     WHERE LOWER(LTRIM(RTRIM(Name))) = 'active'    AND NameEn IS NULL;
                UPDATE ProjectStates SET NameCs = N'Pozastaven' WHERE LOWER(LTRIM(RTRIM(Name))) IN ('onhold', 'on hold') AND NameCs IS NULL;
                UPDATE ProjectStates SET NameEn = N'On Hold'    WHERE LOWER(LTRIM(RTRIM(Name))) IN ('onhold', 'on hold') AND NameEn IS NULL;
                UPDATE ProjectStates SET NameCs = N'Dokončený'  WHERE LOWER(LTRIM(RTRIM(Name))) = 'completed' AND NameCs IS NULL;
                UPDATE ProjectStates SET NameEn = N'Completed'  WHERE LOWER(LTRIM(RTRIM(Name))) = 'completed' AND NameEn IS NULL;
                UPDATE ProjectStates SET NameCs = N'Archivován' WHERE LOWER(LTRIM(RTRIM(Name))) = 'archived'  AND NameCs IS NULL;
                UPDATE ProjectStates SET NameEn = N'Archived'   WHERE LOWER(LTRIM(RTRIM(Name))) = 'archived'  AND NameEn IS NULL;

                UPDATE ProjectTypes SET NameCs = N'Vývoj'      WHERE LOWER(LTRIM(RTRIM(Name))) = 'development' AND NameCs IS NULL;
                UPDATE ProjectTypes SET NameEn = N'Development' WHERE LOWER(LTRIM(RTRIM(Name))) = 'development' AND NameEn IS NULL;

                UPDATE TaskTypes SET NameCs = N'Funkce' WHERE LOWER(LTRIM(RTRIM(Name))) = 'feature' AND NameCs IS NULL;
                UPDATE TaskTypes SET NameEn = N'Feature' WHERE LOWER(LTRIM(RTRIM(Name))) = 'feature' AND NameEn IS NULL;
                UPDATE TaskTypes SET NameCs = N'Chyba'  WHERE LOWER(LTRIM(RTRIM(Name))) = 'bug'     AND NameCs IS NULL;
                UPDATE TaskTypes SET NameEn = N'Bug'    WHERE LOWER(LTRIM(RTRIM(Name))) = 'bug'     AND NameEn IS NULL;
                UPDATE TaskTypes SET NameCs = N'Úkol'   WHERE LOWER(LTRIM(RTRIM(Name))) = 'task'    AND NameCs IS NULL;
                UPDATE TaskTypes SET NameEn = N'Task'   WHERE LOWER(LTRIM(RTRIM(Name))) = 'task'    AND NameEn IS NULL;

                UPDATE ApplicationRoles SET NameCs = N'Administrátor' WHERE LOWER(LTRIM(RTRIM(Name))) = 'admin'    AND NameCs IS NULL;
                UPDATE ApplicationRoles SET NameEn = N'Admin'         WHERE LOWER(LTRIM(RTRIM(Name))) = 'admin'    AND NameEn IS NULL;
                UPDATE ApplicationRoles SET NameCs = N'Manažer'       WHERE LOWER(LTRIM(RTRIM(Name))) = 'manager'  AND NameCs IS NULL;
                UPDATE ApplicationRoles SET NameEn = N'Manager'       WHERE LOWER(LTRIM(RTRIM(Name))) = 'manager'  AND NameEn IS NULL;
                UPDATE ApplicationRoles SET NameCs = N'Uživatel'      WHERE LOWER(LTRIM(RTRIM(Name))) = 'user'     AND NameCs IS NULL;
                UPDATE ApplicationRoles SET NameEn = N'User'          WHERE LOWER(LTRIM(RTRIM(Name))) = 'user'     AND NameEn IS NULL;
                UPDATE ApplicationRoles SET NameCs = N'Externí'       WHERE LOWER(LTRIM(RTRIM(Name))) = 'external' AND NameCs IS NULL;
                UPDATE ApplicationRoles SET NameEn = N'External'      WHERE LOWER(LTRIM(RTRIM(Name))) = 'external' AND NameEn IS NULL;

                -- Fallback: pro libovolný řádek, který výše nepoznáme (vlastní hodnoty
                -- vytvořené uživatelem nebo importované z EasyProject), zkopírujeme
                -- Name do NameCs/NameEn, aby fallback v UI byl konzistentní.
                UPDATE TicketPriorities  SET NameCs = Name WHERE NameCs IS NULL;
                UPDATE TicketPriorities  SET NameEn = Name WHERE NameEn IS NULL;
                UPDATE TaskStates        SET NameCs = Name WHERE NameCs IS NULL;
                UPDATE TaskStates        SET NameEn = Name WHERE NameEn IS NULL;
                UPDATE ProjectStates     SET NameCs = Name WHERE NameCs IS NULL;
                UPDATE ProjectStates     SET NameEn = Name WHERE NameEn IS NULL;
                UPDATE ProjectTypes      SET NameCs = Name WHERE NameCs IS NULL;
                UPDATE ProjectTypes      SET NameEn = Name WHERE NameEn IS NULL;
                UPDATE TaskTypes         SET NameCs = Name WHERE NameCs IS NULL;
                UPDATE TaskTypes         SET NameEn = Name WHERE NameEn IS NULL;
                UPDATE ApplicationRoles  SET NameCs = Name WHERE NameCs IS NULL;
                UPDATE ApplicationRoles  SET NameEn = Name WHERE NameEn IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: data migrace, není co rollbackovat.
        }
    }
}
