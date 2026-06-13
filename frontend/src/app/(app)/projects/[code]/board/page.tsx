"use client";

import { use, useState, useCallback, useMemo, useRef, useEffect, memo } from "react";
import { useBoard, useMoveTicket } from "@/queries/kanban";
import { useCreateTicket, type CreateTicketPayload } from "@/queries/tickets";
import { useProjectByCode } from "@/queries/projects";
import { useViewConfiguration, useUpsertViewConfiguration } from "@/queries/view-configurations";
import { KanbanSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { DynamicStateBadge } from "@/components/shared/status-badge";
import {
  DndContext,
  DragOverlay,
  closestCorners,
  PointerSensor,
  useSensor,
  useSensors,
  type DragStartEvent,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useDroppable } from "@dnd-kit/core";
import { cn } from "@/lib/utils";
import { CreateTicketDialog } from "@/components/tickets/CreateTicketDialog";
import Link from "next/link";
import { useTranslations } from "next-intl";
import {
  MessageSquare,
  Paperclip,
  User,
  Calendar,
  LayoutGrid,
  Settings2,
  ChevronDown,
  Clock,
  Plus,
  GripVertical,
} from "lucide-react";
import type { Ticket, KanbanBoard, KanbanColumn as KanbanColumnType } from "@/types";

type GroupBy = "none" | "assignee" | "priority" | "taskType";

type CardFieldKey =
  | "priority"
  | "assignee"
  | "dueDate"
  | "comments"
  | "attachments"
  | "taskType"
  | "taskState"
  | "estimatedHours";

interface CardField {
  key: CardFieldKey;
  visible: boolean;
}

interface KanbanConfig {
  groupBy: GroupBy;
  cardFields: CardField[];
}

const CARD_FIELD_TKEY: Record<CardFieldKey, string> = {
  priority: "fieldPriority",
  assignee: "fieldAssignee",
  dueDate: "fieldDueDate",
  comments: "fieldComments",
  attachments: "fieldAttachments",
  taskType: "fieldTaskType",
  taskState: "fieldTaskState",
  estimatedHours: "fieldEstimatedHours",
};

const ALL_FIELD_KEYS: CardFieldKey[] = [
  "priority",
  "assignee",
  "dueDate",
  "comments",
  "attachments",
  "taskType",
  "taskState",
  "estimatedHours",
];

const PRESETS: Record<"compact" | "default" | "detailed", CardField[]> = {
  compact: ALL_FIELD_KEYS.map((key) => ({
    key,
    visible: key === "priority" || key === "assignee",
  })),
  default: ALL_FIELD_KEYS.map((key) => ({
    key,
    visible: ["priority", "assignee", "dueDate", "comments", "attachments"].includes(key),
  })),
  detailed: ALL_FIELD_KEYS.map((key) => ({ key, visible: true })),
};

const defaultConfig: KanbanConfig = {
  groupBy: "none",
  cardFields: PRESETS.default,
};

// Migrates legacy cardFields object `{priority: true, ...}` to ordered array.
function parseCardFields(raw: unknown): CardField[] {
  if (Array.isArray(raw)) {
    const known = raw.filter(
      (f): f is CardField =>
        typeof f === "object" &&
        f !== null &&
        "key" in f &&
        "visible" in f &&
        ALL_FIELD_KEYS.includes((f as CardField).key)
    );
    const seen = new Set(known.map((f) => f.key));
    const missing = ALL_FIELD_KEYS.filter((k) => !seen.has(k)).map((key) => ({
      key,
      visible: false,
    }));
    return [...known, ...missing];
  }
  if (typeof raw === "object" && raw !== null) {
    const obj = raw as Partial<Record<CardFieldKey, boolean>>;
    return ALL_FIELD_KEYS.map((key) => ({ key, visible: Boolean(obj[key]) }));
  }
  return PRESETS.default;
}

const TicketCard = memo(function TicketCard({
  ticket,
  code,
  cardFields,
  isDragging = false,
}: {
  ticket: Ticket;
  code: string;
  cardFields: CardField[];
  isDragging?: boolean;
}) {
  const visibleFields = cardFields.filter((f) => f.visible);

  const renderField = (field: CardField) => {
    switch (field.key) {
      case "priority":
        return (
          <PriorityBadge
            key="priority"
            name={ticket.ticketPriorityName}
            color={ticket.ticketPriorityColor}
          />
        );
      case "assignee":
        return ticket.assignee ? (
          <span key="assignee" className="flex items-center gap-1 text-xs text-muted-foreground">
            <User className="h-3 w-3" />
            {ticket.assignee.displayName}
          </span>
        ) : null;
      case "dueDate":
        return ticket.dueDate ? (
          <span key="dueDate" className="flex items-center gap-1 text-xs text-muted-foreground">
            <Calendar className="h-3 w-3" />
            {new Date(ticket.dueDate).toLocaleDateString()}
          </span>
        ) : null;
      case "comments":
        return ticket.commentsCount > 0 ? (
          <span key="comments" className="flex items-center gap-0.5 text-xs text-muted-foreground">
            <MessageSquare className="h-3 w-3" />
            {ticket.commentsCount}
          </span>
        ) : null;
      case "attachments":
        return ticket.attachmentsCount > 0 ? (
          <span
            key="attachments"
            className="flex items-center gap-0.5 text-xs text-muted-foreground"
          >
            <Paperclip className="h-3 w-3" />
            {ticket.attachmentsCount}
          </span>
        ) : null;
      case "taskType":
        return ticket.taskTypeName ? (
          <span key="taskType" className="text-xs text-muted-foreground">
            {ticket.taskTypeIcon && <span className="mr-1">{ticket.taskTypeIcon}</span>}
            {ticket.taskTypeName}
          </span>
        ) : null;
      case "taskState":
        return (
          <DynamicStateBadge
            key="taskState"
            name={ticket.taskStateName}
            color={ticket.taskStateColor}
          />
        );
      case "estimatedHours":
        return ticket.estimatedHours != null ? (
          <span
            key="estimatedHours"
            className="flex items-center gap-0.5 text-xs text-muted-foreground"
          >
            <Clock className="h-3 w-3" />
            {ticket.estimatedHours}h
          </span>
        ) : null;
      default:
        return null;
    }
  };

  return (
    <Link
      href={`/projects/${code}/tickets/${ticket.key}`}
      className={cn(
        "block rounded-lg border border-border bg-card p-3 hover:shadow-sm transition-shadow cursor-pointer",
        isDragging && "shadow-lg opacity-90 rotate-2"
      )}
    >
      <p className="text-xs font-mono text-muted-foreground mb-1">{ticket.key}</p>
      <p className="text-sm font-medium text-card-foreground mb-2 line-clamp-2">{ticket.title}</p>
      {visibleFields.length > 0 && (
        <div className="flex items-center flex-wrap gap-x-2 gap-y-1">
          {visibleFields.map((f) => renderField(f))}
        </div>
      )}
    </Link>
  );
});

function SortableTicketCard({
  ticket,
  code,
  cardFields,
}: {
  ticket: Ticket;
  code: string;
  cardFields: CardField[];
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: ticket.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      <TicketCard ticket={ticket} code={code} cardFields={cardFields} />
    </div>
  );
}

const KanbanColumn = memo(function KanbanColumn({
  column,
  projectId,
  code,
  cardFields,
}: {
  column: KanbanColumnType;
  projectId: string;
  code: string;
  cardFields: CardField[];
}) {
  const t = useTranslations("Board");
  const tCommon = useTranslations("Common");
  const { setNodeRef, isOver } = useDroppable({ id: column.id });
  const [isAdding, setIsAdding] = useState(false);
  const [newTitle, setNewTitle] = useState("");
  const createTicket = useCreateTicket();

  const handleCreate = async () => {
    if (!newTitle.trim()) return;
    await createTicket.mutateAsync({
      projectId,
      title: newTitle.trim(),
      columnId: column.id,
    } as CreateTicketPayload);
    setNewTitle("");
    setIsAdding(false);
  };

  return (
    <div className="min-w-[300px] max-w-[300px] flex flex-col">
      <div
        className="flex items-center justify-between mb-3 px-1 border-t-3 rounded-t pt-2"
        style={{ borderTopColor: column.color ?? "transparent" }}
      >
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-semibold text-foreground">{column.name}</h3>
          <span className="text-xs text-muted-foreground bg-muted rounded-full px-2 py-0.5">
            {column.tickets.length}
          </span>
        </div>
        {column.wipLimit && (
          <span className="text-xs text-muted-foreground">WIP: {column.wipLimit}</span>
        )}
      </div>
      <div
        ref={setNodeRef}
        className={cn(
          "flex-1 rounded-lg bg-muted/50 p-2 space-y-2 min-h-[200px] transition-colors",
          isOver && "bg-accent-orange/10 ring-2 ring-accent-orange/30"
        )}
      >
        <SortableContext
          items={column.tickets.map((t) => t.id)}
          strategy={verticalListSortingStrategy}
        >
          {column.tickets.map((ticket) => (
            <SortableTicketCard
              key={ticket.id}
              ticket={ticket}
              code={code}
              cardFields={cardFields}
            />
          ))}
        </SortableContext>
        {column.tickets.length === 0 && !isAdding && (
          <p className="text-xs text-muted-foreground text-center py-8">{t("dropTicketsHere")}</p>
        )}
        {isAdding ? (
          <div className="p-2 space-y-2">
            <input
              autoFocus
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleCreate();
                if (e.key === "Escape") {
                  setIsAdding(false);
                  setNewTitle("");
                }
              }}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
              placeholder={t("taskTitlePlaceholder")}
            />
            <div className="flex gap-1">
              <button
                onClick={handleCreate}
                disabled={createTicket.isPending}
                className="px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
              >
                {t("add")}
              </button>
              <button
                onClick={() => {
                  setIsAdding(false);
                  setNewTitle("");
                }}
                className="px-3 py-1.5 rounded-lg border border-border text-xs text-muted-foreground hover:text-foreground"
              >
                {tCommon("cancel")}
              </button>
            </div>
          </div>
        ) : (
          <button
            onClick={() => setIsAdding(true)}
            className="w-full text-left px-2 py-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            {t("newColumn")}
          </button>
        )}
      </div>
    </div>
  );
});

function getSwimlaneName(
  ticket: Ticket,
  groupBy: GroupBy,
  unassignedLabel: string,
  noTypeLabel: string
): string {
  switch (groupBy) {
    case "assignee":
      return ticket.assignee?.displayName || unassignedLabel;
    case "priority":
      return ticket.ticketPriorityName;
    case "taskType":
      return ticket.taskTypeName || noTypeLabel;
    default:
      return "";
  }
}

function buildSwimlanes(
  board: KanbanBoard,
  groupBy: GroupBy,
  unassignedLabel: string,
  noTypeLabel: string
): { name: string; columns: KanbanColumnType[] }[] {
  const visibleColumns = board.columns.filter((c) => c.isVisible);

  if (groupBy === "none") {
    return [{ name: "", columns: visibleColumns }];
  }

  const allTickets = visibleColumns.flatMap((c) =>
    c.tickets.map((t) => ({ ...t, _columnId: c.id }))
  );

  const groupNames = Array.from(
    new Set(allTickets.map((t) => getSwimlaneName(t, groupBy, unassignedLabel, noTypeLabel)))
  ).sort();

  return groupNames.map((name) => ({
    name,
    columns: visibleColumns.map((col) => ({
      ...col,
      tickets: col.tickets.filter(
        (t) => getSwimlaneName(t, groupBy, unassignedLabel, noTypeLabel) === name
      ),
    })),
  }));
}

function SortableFieldRow({
  field,
  onToggle,
}: {
  field: CardField;
  onToggle: (key: CardFieldKey) => void;
}) {
  const t = useTranslations("Board");
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: field.key,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="flex items-center gap-2 py-1 rounded hover:bg-muted/50"
    >
      <button
        {...attributes}
        {...listeners}
        className="cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground"
        aria-label={t("dragToReorder")}
        type="button"
      >
        <GripVertical className="h-3.5 w-3.5" />
      </button>
      <label className="flex items-center gap-2 text-sm cursor-pointer flex-1">
        <input
          type="checkbox"
          checked={field.visible}
          onChange={() => onToggle(field.key)}
          className="rounded"
        />
        {t(CARD_FIELD_TKEY[field.key] as "fieldPriority")}
      </label>
    </div>
  );
}

export default function BoardPage({ params }: { params: Promise<{ code: string }> }) {
  const t = useTranslations("Board");
  const { code } = use(params);
  const { data: project } = useProjectByCode(code);
  const projectId = project?.id ?? "";
  const { data: board, isLoading, error } = useBoard(projectId);
  const { data: viewConfig } = useViewConfiguration("Kanban", projectId);
  const upsertConfig = useUpsertViewConfiguration();
  const moveTicket = useMoveTicket();
  const [activeTicket, setActiveTicket] = useState<Ticket | null>(null);
  const [showGroupBy, setShowGroupBy] = useState(false);
  const [showCardSettings, setShowCardSettings] = useState(false);
  const [showCreateDialog, setShowCreateDialog] = useState(false);

  const savedConfig = useMemo<KanbanConfig>(() => {
    if (!viewConfig?.configurationJson) return defaultConfig;
    try {
      const parsed = JSON.parse(viewConfig.configurationJson);
      return {
        groupBy: parsed.groupBy ?? "none",
        cardFields: parseCardFields(parsed.cardFields),
      };
    } catch {
      return defaultConfig;
    }
  }, [viewConfig]);

  const [groupBy, setGroupBy] = useState<GroupBy>(savedConfig.groupBy);
  const [cardFields, setCardFields] = useState<CardField[]>(savedConfig.cardFields);

  // useState initial value is captured before viewConfig query resolves.
  // Sync local state once when the config first arrives from the server.
  const hydrated = useRef(false);
  useEffect(() => {
    if (hydrated.current) return;
    if (!viewConfig) return;
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setGroupBy(savedConfig.groupBy);
    setCardFields(savedConfig.cardFields);
    hydrated.current = true;
  }, [viewConfig, savedConfig]);

  const saveTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
  const persistConfig = useCallback(
    (config: KanbanConfig) => {
      if (saveTimeout.current) clearTimeout(saveTimeout.current);
      saveTimeout.current = setTimeout(() => {
        upsertConfig.mutate({
          projectId,
          viewType: "Kanban",
          configurationJson: JSON.stringify(config),
        });
      }, 1000);
    },
    [projectId, upsertConfig]
  );

  const handleGroupByChange = useCallback(
    (value: GroupBy) => {
      setGroupBy(value);
      setShowGroupBy(false);
      persistConfig({ groupBy: value, cardFields });
    },
    [persistConfig, cardFields]
  );

  const handleCardFieldToggle = useCallback(
    (key: CardFieldKey) => {
      setCardFields((prev) => {
        const next = prev.map((f) => (f.key === key ? { ...f, visible: !f.visible } : f));
        persistConfig({ groupBy, cardFields: next });
        return next;
      });
    },
    [persistConfig, groupBy]
  );

  const handleFieldReorder = useCallback(
    (oldIndex: number, newIndex: number) => {
      setCardFields((prev) => {
        const next = arrayMove(prev, oldIndex, newIndex);
        persistConfig({ groupBy, cardFields: next });
        return next;
      });
    },
    [persistConfig, groupBy]
  );

  const handlePresetSelect = useCallback(
    (presetName: keyof typeof PRESETS) => {
      const next = PRESETS[presetName];
      setCardFields(next);
      persistConfig({ groupBy, cardFields: next });
    },
    [persistConfig, groupBy]
  );

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    })
  );

  const handleDragStart = useCallback(
    (event: DragStartEvent) => {
      const ticketId = event.active.id as string;
      const ticket = board?.columns.flatMap((c) => c.tickets).find((t) => t.id === ticketId);
      if (ticket) setActiveTicket(ticket);
    },
    [board]
  );

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      setActiveTicket(null);
      const { active, over } = event;
      if (!over || !board) return;

      const ticketId = active.id as string;
      const overId = over.id as string;
      if (overId === ticketId) return;

      // `over` is either a column (the droppable surface) or a ticket card (a
      // sortable item). When a column already has cards, dropping over it
      // reports the card's id, not the column's — so resolve to the column
      // either way, otherwise the move is dropped and the card snaps back.
      let targetColumn = board.columns.find((c) => c.id === overId);
      let position: number;
      if (targetColumn) {
        position = targetColumn.tickets.length;
      } else {
        targetColumn = board.columns.find((c) => c.tickets.some((t) => t.id === overId));
        if (!targetColumn) return;
        const overIndex = targetColumn.tickets.findIndex((t) => t.id === overId);
        position = overIndex === -1 ? targetColumn.tickets.length : overIndex;
      }

      moveTicket.mutate({
        projectId,
        ticketId,
        targetColumnId: targetColumn.id,
        position,
      });
    },
    [board, projectId, moveTicket]
  );

  const handleFieldDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;
      const oldIndex = cardFields.findIndex((f) => f.key === active.id);
      const newIndex = cardFields.findIndex((f) => f.key === over.id);
      if (oldIndex !== -1 && newIndex !== -1) handleFieldReorder(oldIndex, newIndex);
    },
    [cardFields, handleFieldReorder]
  );

  const unassignedLabel = t("unassigned");
  const noTypeLabel = t("noType");

  const swimlanes = useMemo(
    () => (board ? buildSwimlanes(board, groupBy, unassignedLabel, noTypeLabel) : []),
    [board, groupBy, unassignedLabel, noTypeLabel]
  );

  const groupByOptions: { value: GroupBy; label: string }[] = [
    { value: "none", label: t("groupByNone") },
    { value: "assignee", label: t("groupAssignee") },
    { value: "priority", label: t("groupPriority") },
    { value: "taskType", label: t("groupTaskType") },
  ];

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <KanbanSkeleton />
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        {t("loadFailed")}
      </div>
    );
  }

  if (!board) {
    return (
      <EmptyState
        icon={<LayoutGrid className="h-12 w-12" />}
        title={t("noBoardTitle")}
        description={t("noBoardDesc")}
      />
    );
  }

  return (
    <div className="space-y-4 h-full flex flex-col">
      {/* Toolbar */}
      <div className="flex items-center justify-between flex-shrink-0">
        <p className="text-sm text-muted-foreground">{board.name}</p>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setShowCreateDialog(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-primary-foreground bg-primary rounded-lg hover:opacity-90 transition-opacity"
          >
            <Plus className="h-4 w-4" />
            {t("newTask")}
          </button>
          {/* Group by dropdown */}
          <div className="relative">
            <button
              onClick={() => {
                setShowGroupBy((v) => !v);
                setShowCardSettings(false);
              }}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
            >
              <ChevronDown className="h-4 w-4" />
              {groupBy === "none"
                ? t("groupBy")
                : t("groupByPrefix", {
                    label: groupByOptions.find((o) => o.value === groupBy)?.label ?? "",
                  })}
            </button>
            {showGroupBy && (
              <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg py-1 w-44">
                {groupByOptions.map((opt) => (
                  <button
                    key={opt.value}
                    onClick={() => handleGroupByChange(opt.value)}
                    className={cn(
                      "w-full text-left px-3 py-1.5 text-sm hover:bg-muted transition-colors",
                      groupBy === opt.value && "text-primary font-medium bg-muted/50"
                    )}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Card settings */}
          <div className="relative">
            <button
              onClick={() => {
                setShowCardSettings((v) => !v);
                setShowGroupBy(false);
              }}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
            >
              <Settings2 className="h-4 w-4" />
              {t("cardFields")}
            </button>
            {showCardSettings && (
              <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg p-3 w-60">
                <p className="text-xs font-medium text-muted-foreground mb-2">{t("preset")}</p>
                <div className="flex gap-1 mb-3">
                  {(Object.keys(PRESETS) as Array<keyof typeof PRESETS>).map((name) => (
                    <button
                      key={name}
                      onClick={() => handlePresetSelect(name)}
                      className="flex-1 px-2 py-1 text-xs rounded border border-border hover:bg-muted transition-colors"
                    >
                      {t(
                        `preset${name.charAt(0).toUpperCase()}${name.slice(1)}` as
                          | "presetCompact"
                          | "presetDefault"
                          | "presetDetailed"
                      )}
                    </button>
                  ))}
                </div>
                <p className="text-xs font-medium text-muted-foreground mb-1">
                  {t("fieldsDragToReorder")}
                </p>
                <DndContext
                  sensors={sensors}
                  collisionDetection={closestCorners}
                  onDragEnd={handleFieldDragEnd}
                >
                  <SortableContext
                    items={cardFields.map((f) => f.key)}
                    strategy={verticalListSortingStrategy}
                  >
                    {cardFields.map((field) => (
                      <SortableFieldRow
                        key={field.key}
                        field={field}
                        onToggle={handleCardFieldToggle}
                      />
                    ))}
                  </SortableContext>
                </DndContext>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Board with swimlanes */}
      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div className="flex-1 overflow-auto">
          {swimlanes.map((lane) => (
            <div key={lane.name || "__default"} className="mb-6">
              {lane.name && (
                <div className="flex items-center gap-2 mb-3 sticky left-0">
                  <h3 className="text-sm font-semibold text-foreground">{lane.name}</h3>
                  <span className="text-xs text-muted-foreground">
                    ({lane.columns.reduce((sum, c) => sum + c.tickets.length, 0)})
                  </span>
                </div>
              )}
              <div className="flex gap-4 pb-4">
                {lane.columns
                  .sort((a, b) => a.position - b.position)
                  .map((column) => (
                    <KanbanColumn
                      key={`${lane.name}-${column.id}`}
                      column={column}
                      projectId={projectId}
                      code={code}
                      cardFields={cardFields}
                    />
                  ))}
              </div>
            </div>
          ))}
        </div>

        <DragOverlay>
          {activeTicket && (
            <div className="w-[280px]">
              <TicketCard ticket={activeTicket} code={code} cardFields={cardFields} isDragging />
            </div>
          )}
        </DragOverlay>
      </DndContext>

      <CreateTicketDialog
        projectId={projectId}
        projectTemplateId={project?.projectTemplateId}
        open={showCreateDialog}
        onClose={() => setShowCreateDialog(false)}
      />
    </div>
  );
}
