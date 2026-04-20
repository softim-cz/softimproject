-- =============================================================================
-- One-off data migration: create "Unitegra" ProjectTemplate and rebase
-- UNITEGRA (UNI) + Unitegra v2 (UNITEG) onto it.
--
-- Context:
--   After the EasyProject import, both projects are attached to the "Default"
--   template, which now carries a mix of original English states and
--   EasyProject-imported Czech states. This script creates a dedicated
--   "Unitegra" template containing ONLY the 3 states and 4 priorities that
--   tickets in UNI/UNITEG actually reference, remaps every ticket and Kanban
--   column mapping to the new IDs, and sets both projects' ProjectTemplateId
--   to the new template.
--
-- Run via Azure Portal -> SQL Database -> Query editor, or SSMS, against the
-- production database bound to softimproject-api.azurewebsites.net.
--
-- Execution is wrapped in an explicit transaction. The script ends without
-- COMMIT; run `COMMIT TRANSACTION;` manually after reviewing the verification
-- block output. If anything looks wrong, run `ROLLBACK TRANSACTION;`.
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

-- ---- Known IDs (from Azure production, captured 2026-04-20) -----------------

DECLARE @UniProjectId      uniqueidentifier = 'c7aa8008-62be-4e38-969a-666879697ad1';
DECLARE @UnitegV2ProjectId uniqueidentifier = 'd05706f7-1b27-4061-bcbf-58bbdc803c87';

-- Old state IDs (currently under "Default" template)
DECLARE @StNovy_Old         uniqueidentifier = '54a0ed8c-ad71-41e8-84c4-2a758086607a';
DECLARE @StVRealizaci_Old   uniqueidentifier = 'f2bdb62c-dd3c-4efb-85c4-74e5e7b63741';
DECLARE @StKeSchvaleni_Old  uniqueidentifier = 'b348589a-0d68-4b06-bd13-9c2c8b762d19';

-- Old priority IDs (currently under "Default" template)
DECLARE @PrA_Old uniqueidentifier = 'c0573892-45a2-4616-978f-701ceee80c3e';
DECLARE @PrB_Old uniqueidentifier = 'c0e1ce3a-0b9c-4cf3-a813-21116d7e749c';
DECLARE @PrC_Old uniqueidentifier = '774b63a0-6d5a-4219-aee9-1f91961aac69';
DECLARE @PrD_Old uniqueidentifier = '7fa6b215-b6ad-4bb9-88b0-e1508287ed4a';

-- New template + state + priority IDs
DECLARE @UnitegraTemplateId uniqueidentifier = NEWID();
DECLARE @StNovy_New         uniqueidentifier = NEWID();
DECLARE @StVRealizaci_New   uniqueidentifier = NEWID();
DECLARE @StKeSchvaleni_New  uniqueidentifier = NEWID();
DECLARE @PrA_New            uniqueidentifier = NEWID();
DECLARE @PrB_New            uniqueidentifier = NEWID();
DECLARE @PrC_New            uniqueidentifier = NEWID();
DECLARE @PrD_New            uniqueidentifier = NEWID();

DECLARE @Now datetime2 = SYSUTCDATETIME();

-- ---- 1. Create the "Unitegra" template --------------------------------------

INSERT INTO ProjectTemplates (Id, Name, Description, IsActive, CreatedAt)
VALUES (
    @UnitegraTemplateId,
    N'Unitegra',
    N'Workflow used by UNITEGRA (UNI) and Unitegra v2 — contains only the states and priorities actually referenced by imported tickets.',
    1,
    @Now
);

-- ---- 2. Create the 3 used TaskStates under Unitegra -------------------------

INSERT INTO TaskStates
    (Id, ProjectTemplateId, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
VALUES
    (@StNovy_New,        @UnitegraTemplateId, N'Nový',         '#6B7280', 10, 1, 1, 0, @Now),
    (@StVRealizaci_New,  @UnitegraTemplateId, N'V realizaci',  '#6B7280', 20, 1, 0, 0, @Now),
    (@StKeSchvaleni_New, @UnitegraTemplateId, N'Ke schválení', '#6B7280', 30, 1, 0, 0, @Now);

-- ---- 3. Create the 4 used TicketPriorities under Unitegra -------------------

INSERT INTO TicketPriorities
    (Id, ProjectTemplateId, Name, Color, SortOrder, IsActive, IsDefault, CreatedAt)
VALUES
    (@PrA_New, @UnitegraTemplateId, N'A) Urgentní',  '#6B7280', 10, 1, 0, @Now),
    (@PrB_New, @UnitegraTemplateId, N'B) Vysoká',    '#6B7280', 20, 1, 0, @Now),
    (@PrC_New, @UnitegraTemplateId, N'C) Normální',  '#6B7280', 30, 1, 1, @Now),
    (@PrD_New, @UnitegraTemplateId, N'D) Nízká',     '#6B7280', 40, 1, 0, @Now);

-- ---- 4. Remap Ticket.TaskStateId for both projects --------------------------

UPDATE Tickets
SET TaskStateId = CASE TaskStateId
        WHEN @StNovy_Old        THEN @StNovy_New
        WHEN @StVRealizaci_Old  THEN @StVRealizaci_New
        WHEN @StKeSchvaleni_Old THEN @StKeSchvaleni_New
    END,
    UpdatedAt = @Now
WHERE ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
  AND TaskStateId IN (@StNovy_Old, @StVRealizaci_Old, @StKeSchvaleni_Old);

-- ---- 5. Remap Ticket.TicketPriorityId for both projects ---------------------

UPDATE Tickets
SET TicketPriorityId = CASE TicketPriorityId
        WHEN @PrA_Old THEN @PrA_New
        WHEN @PrB_Old THEN @PrB_New
        WHEN @PrC_Old THEN @PrC_New
        WHEN @PrD_Old THEN @PrD_New
    END,
    UpdatedAt = @Now
WHERE ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
  AND TicketPriorityId IN (@PrA_Old, @PrB_Old, @PrC_Old, @PrD_Old);

-- ---- 6. Remap KanbanColumn <-> TaskState mappings for boards of both projects

UPDATE kcts
SET TaskStateId = CASE kcts.TaskStateId
        WHEN @StNovy_Old        THEN @StNovy_New
        WHEN @StVRealizaci_Old  THEN @StVRealizaci_New
        WHEN @StKeSchvaleni_Old THEN @StKeSchvaleni_New
    END
FROM KanbanColumnTaskState AS kcts
INNER JOIN KanbanColumns AS kc ON kc.Id = kcts.KanbanColumnId
INNER JOIN KanbanBoards  AS kb ON kb.Id = kc.BoardId
WHERE kb.ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
  AND kcts.TaskStateId IN (@StNovy_Old, @StVRealizaci_Old, @StKeSchvaleni_Old);

-- ---- 7. Point both projects at the Unitegra template ------------------------

UPDATE Projects
SET ProjectTemplateId = @UnitegraTemplateId,
    UpdatedAt = @Now
WHERE Id IN (@UniProjectId, @UnitegV2ProjectId);

-- =============================================================================
-- Verification — all four counters MUST be zero before you COMMIT.
-- =============================================================================

SELECT 'tickets_with_stale_state_fk' AS check_name,
       COUNT(*) AS bad_rows
FROM Tickets
WHERE ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
  AND TaskStateId NOT IN (@StNovy_New, @StVRealizaci_New, @StKeSchvaleni_New);

SELECT 'tickets_with_stale_priority_fk' AS check_name,
       COUNT(*) AS bad_rows
FROM Tickets
WHERE ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
  AND TicketPriorityId NOT IN (@PrA_New, @PrB_New, @PrC_New, @PrD_New);

SELECT 'kanban_columns_with_stale_state_fk' AS check_name,
       COUNT(*) AS bad_rows
FROM KanbanColumnTaskState AS kcts
INNER JOIN KanbanColumns AS kc ON kc.Id = kcts.KanbanColumnId
INNER JOIN KanbanBoards  AS kb ON kb.Id = kc.BoardId
WHERE kb.ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
  AND kcts.TaskStateId IN (@StNovy_Old, @StVRealizaci_Old, @StKeSchvaleni_Old);

SELECT 'projects_not_on_unitegra_template' AS check_name,
       COUNT(*) AS bad_rows
FROM Projects
WHERE Id IN (@UniProjectId, @UnitegV2ProjectId)
  AND ProjectTemplateId <> @UnitegraTemplateId;

-- Summary of what moved
SELECT 'ticket_state_distribution' AS summary,
       ts.Name AS state,
       COUNT(*) AS tickets
FROM Tickets t
INNER JOIN TaskStates ts ON ts.Id = t.TaskStateId
WHERE t.ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
GROUP BY ts.Name;

SELECT 'ticket_priority_distribution' AS summary,
       tp.Name AS priority,
       COUNT(*) AS tickets
FROM Tickets t
INNER JOIN TicketPriorities tp ON tp.Id = t.TicketPriorityId
WHERE t.ProjectId IN (@UniProjectId, @UnitegV2ProjectId)
GROUP BY tp.Name;

-- =============================================================================
-- All four checks returned bad_rows = 0?
--   -> Run:  COMMIT TRANSACTION;
-- Anything non-zero or unexpected?
--   -> Run:  ROLLBACK TRANSACTION;
-- =============================================================================
