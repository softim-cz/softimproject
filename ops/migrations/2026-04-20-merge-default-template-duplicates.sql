-- =============================================================================
-- One-off data migration: merge duplicate Task States and Ticket Priorities
-- inside the "Default" ProjectTemplate.
--
-- Context:
--   After the EasyProject import, the Default template contains the original
--   6 English states (Backlog, Todo, InProgress, Review, Done, Closed) plus
--   11 Czech states (Nový, Přiřazeno, V realizaci, Otestováno, Ke schválení,
--   Vráceno klientem, Na klientovi, Čeká na podpis, Hotovo, Odloženo, Zrušeno).
--   Priorities show the same pattern: 4 English (Low/Medium/High/Critical) plus
--   4 Czech (A) Urgentní, B) Vysoká, C) Normální, D) Nízká).
--
--   The English rows are duplicates of the Czech ones in different language.
--   This script:
--     1. Remaps any Ticket / KanbanColumn references on the English rows to
--        their Czech canonical counterparts (a no-op for imported UNI/UNITEG
--        tickets, which already use the Czech IDs, but kept as a safety net).
--     2. Deletes the English duplicate rows from TaskStates and
--        TicketPriorities.
--     3. Normalizes the surviving Czech rows: assigns a sane SortOrder and
--        sets "Nový" + "C) Normální" as defaults. Names stay Czech-only;
--        multilingual support will be introduced later as a separate feature.
--
--   After commit, the Default template carries exactly 11 states and 4
--   priorities — all Czech-named, deduplicated.
--
-- Run via Azure Portal -> SQL Database -> Query editor or SSMS against the
-- production DB behind softimproject-api.azurewebsites.net. The script is
-- wrapped in an explicit transaction and ends WITHOUT committing — review the
-- verification output, then run `COMMIT TRANSACTION;` (or `ROLLBACK;`).
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @Now datetime2 = SYSUTCDATETIME();

-- ---- Canonical (Czech) state IDs — stay, get renamed --------------------
DECLARE @CsNovy          uniqueidentifier = '54a0ed8c-ad71-41e8-84c4-2a758086607a';
DECLARE @CsPrirazeno     uniqueidentifier = '0b4dc041-fbda-493a-bab2-39c8e7607ea0';
DECLARE @CsVRealizaci    uniqueidentifier = 'f2bdb62c-dd3c-4efb-85c4-74e5e7b63741';
DECLARE @CsOtestovano    uniqueidentifier = '4ae23c80-4927-4554-9d7a-ba302dac7db3';
DECLARE @CsKeSchvaleni   uniqueidentifier = 'b348589a-0d68-4b06-bd13-9c2c8b762d19';
DECLARE @CsVracenoKlient uniqueidentifier = '3cc91c1f-394c-4997-b857-8c23b0b28137';
DECLARE @CsNaKlientovi   uniqueidentifier = '998d8aef-721e-487f-9d98-a1f7ff3d0ece';
DECLARE @CsCekaNaPodpis  uniqueidentifier = '4754987e-9abc-4052-b563-20b6fcf0c875';
DECLARE @CsHotovo        uniqueidentifier = '754b44d7-3e00-474c-a06a-569a608e3f23';
DECLARE @CsOdlozeno      uniqueidentifier = 'f0f3250c-8a0c-44f4-90cd-ab03401e417e';
DECLARE @CsZruseno       uniqueidentifier = '01e1188e-2b4d-49ae-af3b-995542aad253';

-- ---- Duplicate (English) state IDs — merged, then deleted ---------------
DECLARE @EnBacklog    uniqueidentifier = '20000000-0000-0000-0000-000000000001';  -- -> Nový
DECLARE @EnTodo       uniqueidentifier = '20000000-0000-0000-0000-000000000002';  -- -> Nový
DECLARE @EnInProgress uniqueidentifier = '20000000-0000-0000-0000-000000000003';  -- -> V realizaci
DECLARE @EnReview     uniqueidentifier = '20000000-0000-0000-0000-000000000004';  -- -> Ke schválení
DECLARE @EnDone       uniqueidentifier = '20000000-0000-0000-0000-000000000005';  -- -> Hotovo
DECLARE @EnClosed     uniqueidentifier = '20000000-0000-0000-0000-000000000006';  -- -> Hotovo

-- ---- Canonical (Czech) priority IDs — stay, get renamed -----------------
DECLARE @CsPrA uniqueidentifier = 'c0573892-45a2-4616-978f-701ceee80c3e';  -- A) Urgentní
DECLARE @CsPrB uniqueidentifier = 'c0e1ce3a-0b9c-4cf3-a813-21116d7e749c';  -- B) Vysoká
DECLARE @CsPrC uniqueidentifier = '774b63a0-6d5a-4219-aee9-1f91961aac69';  -- C) Normální
DECLARE @CsPrD uniqueidentifier = '7fa6b215-b6ad-4bb9-88b0-e1508287ed4a';  -- D) Nízká

-- ---- Duplicate (English) priority IDs — merged, then deleted ------------
DECLARE @EnLow      uniqueidentifier = '10000000-0000-0000-0000-000000000001';  -- -> D) Nízká
DECLARE @EnMedium   uniqueidentifier = '10000000-0000-0000-0000-000000000002';  -- -> C) Normální
DECLARE @EnHigh     uniqueidentifier = '10000000-0000-0000-0000-000000000003';  -- -> B) Vysoká
DECLARE @EnCritical uniqueidentifier = '10000000-0000-0000-0000-000000000004';  -- -> A) Urgentní

-- =============================================================================
-- 1. Remap any ticket referencing an English state/priority to the Czech one
-- =============================================================================

UPDATE Tickets
SET TaskStateId = CASE TaskStateId
        WHEN @EnBacklog    THEN @CsNovy
        WHEN @EnTodo       THEN @CsNovy
        WHEN @EnInProgress THEN @CsVRealizaci
        WHEN @EnReview     THEN @CsKeSchvaleni
        WHEN @EnDone       THEN @CsHotovo
        WHEN @EnClosed     THEN @CsHotovo
    END,
    UpdatedAt = @Now
WHERE TaskStateId IN (@EnBacklog, @EnTodo, @EnInProgress, @EnReview, @EnDone, @EnClosed);

UPDATE Tickets
SET TicketPriorityId = CASE TicketPriorityId
        WHEN @EnLow      THEN @CsPrD
        WHEN @EnMedium   THEN @CsPrC
        WHEN @EnHigh     THEN @CsPrB
        WHEN @EnCritical THEN @CsPrA
    END,
    UpdatedAt = @Now
WHERE TicketPriorityId IN (@EnLow, @EnMedium, @EnHigh, @EnCritical);

-- =============================================================================
-- 2. Remap KanbanColumn <-> TaskState join rows. First drop any English join
--    rows whose column already has the canonical Czech state mapped (avoids
--    violating the composite unique key after the UPDATE).
-- =============================================================================

DELETE kcts_dup
FROM KanbanColumnTaskState kcts_dup
WHERE kcts_dup.TaskStateId IN (@EnBacklog, @EnTodo, @EnInProgress, @EnReview, @EnDone, @EnClosed)
  AND EXISTS (
      SELECT 1 FROM KanbanColumnTaskState kcts_can
      WHERE kcts_can.KanbanColumnId = kcts_dup.KanbanColumnId
        AND kcts_can.TaskStateId = CASE kcts_dup.TaskStateId
                WHEN @EnBacklog    THEN @CsNovy
                WHEN @EnTodo       THEN @CsNovy
                WHEN @EnInProgress THEN @CsVRealizaci
                WHEN @EnReview     THEN @CsKeSchvaleni
                WHEN @EnDone       THEN @CsHotovo
                WHEN @EnClosed     THEN @CsHotovo
            END
  );

UPDATE KanbanColumnTaskState
SET TaskStateId = CASE TaskStateId
        WHEN @EnBacklog    THEN @CsNovy
        WHEN @EnTodo       THEN @CsNovy
        WHEN @EnInProgress THEN @CsVRealizaci
        WHEN @EnReview     THEN @CsKeSchvaleni
        WHEN @EnDone       THEN @CsHotovo
        WHEN @EnClosed     THEN @CsHotovo
    END
WHERE TaskStateId IN (@EnBacklog, @EnTodo, @EnInProgress, @EnReview, @EnDone, @EnClosed);

-- =============================================================================
-- 3. Delete the English duplicates. Tickets & join rows no longer reference
--    them, so the Restrict FK on Tickets cannot fire.
-- =============================================================================

DELETE FROM TaskStates
WHERE Id IN (@EnBacklog, @EnTodo, @EnInProgress, @EnReview, @EnDone, @EnClosed);

DELETE FROM TicketPriorities
WHERE Id IN (@EnLow, @EnMedium, @EnHigh, @EnCritical);

-- =============================================================================
-- 4. Normalize the surviving Czech rows: names stay Czech-only, SortOrder
--    follows the workflow, "Nový" becomes the template's default state and
--    "C) Normální" the default priority.
-- =============================================================================

UPDATE TaskStates SET Name = N'Nový',             SortOrder = 10,  IsDefault = 1, UpdatedAt = @Now WHERE Id = @CsNovy;
UPDATE TaskStates SET Name = N'Přiřazeno',        SortOrder = 20,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsPrirazeno;
UPDATE TaskStates SET Name = N'V realizaci',      SortOrder = 30,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsVRealizaci;
UPDATE TaskStates SET Name = N'Otestováno',       SortOrder = 40,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsOtestovano;
UPDATE TaskStates SET Name = N'Ke schválení',     SortOrder = 50,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsKeSchvaleni;
UPDATE TaskStates SET Name = N'Vráceno klientem', SortOrder = 60,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsVracenoKlient;
UPDATE TaskStates SET Name = N'Na klientovi',     SortOrder = 70,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsNaKlientovi;
UPDATE TaskStates SET Name = N'Čeká na podpis',   SortOrder = 80,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsCekaNaPodpis;
UPDATE TaskStates SET Name = N'Hotovo',           SortOrder = 90,  IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsHotovo;
UPDATE TaskStates SET Name = N'Odloženo',         SortOrder = 100, IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsOdlozeno;
UPDATE TaskStates SET Name = N'Zrušeno',          SortOrder = 110, IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsZruseno;

UPDATE TicketPriorities SET Name = N'A) Urgentní', SortOrder = 10, IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsPrA;
UPDATE TicketPriorities SET Name = N'B) Vysoká',   SortOrder = 20, IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsPrB;
UPDATE TicketPriorities SET Name = N'C) Normální', SortOrder = 30, IsDefault = 1, UpdatedAt = @Now WHERE Id = @CsPrC;
UPDATE TicketPriorities SET Name = N'D) Nízká',    SortOrder = 40, IsDefault = 0, UpdatedAt = @Now WHERE Id = @CsPrD;

-- =============================================================================
-- Verification — all four counters MUST be zero before you COMMIT.
-- =============================================================================

SELECT 'tickets_still_on_english_state' AS check_name, COUNT(*) AS bad_rows
FROM Tickets
WHERE TaskStateId IN (@EnBacklog, @EnTodo, @EnInProgress, @EnReview, @EnDone, @EnClosed);

SELECT 'tickets_still_on_english_priority' AS check_name, COUNT(*) AS bad_rows
FROM Tickets
WHERE TicketPriorityId IN (@EnLow, @EnMedium, @EnHigh, @EnCritical);

SELECT 'kanban_columns_still_on_english_state' AS check_name, COUNT(*) AS bad_rows
FROM KanbanColumnTaskState
WHERE TaskStateId IN (@EnBacklog, @EnTodo, @EnInProgress, @EnReview, @EnDone, @EnClosed);

SELECT 'default_template_has_duplicate_names' AS check_name, COUNT(*) AS bad_rows
FROM (
    SELECT Name FROM TaskStates
    WHERE ProjectTemplateId = '00000000-0000-0000-0000-000000000001'
    GROUP BY Name HAVING COUNT(*) > 1
    UNION ALL
    SELECT Name FROM TicketPriorities
    WHERE ProjectTemplateId = '00000000-0000-0000-0000-000000000001'
    GROUP BY Name HAVING COUNT(*) > 1
) dupes;

-- Final listing — 11 states, 4 priorities.
SELECT 'final_task_states' AS summary, SortOrder, Name, IsDefault, IsClosedState
FROM TaskStates
WHERE ProjectTemplateId = '00000000-0000-0000-0000-000000000001'
ORDER BY SortOrder;

SELECT 'final_ticket_priorities' AS summary, SortOrder, Name, IsDefault
FROM TicketPriorities
WHERE ProjectTemplateId = '00000000-0000-0000-0000-000000000001'
ORDER BY SortOrder;

-- =============================================================================
-- All four checks returned bad_rows = 0 and final listings look right?
--   -> COMMIT TRANSACTION;
-- Anything unexpected?
--   -> ROLLBACK TRANSACTION;
-- =============================================================================
