"use client";

import { use, useState, useCallback, useMemo, useRef, memo } from "react";
import { useBoard, useMoveTicket } from "@/queries/kanban";
import { useCreateTicket } from "@/queries/tickets";
import { useProjectByCode } from "@/queries/projects";
import {
  useViewConfiguration,
  useUpsertViewConfiguration,
} from "@/queries/view-configurations";
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
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useDroppable } from "@dnd-kit/core";
import { cn } from "@/lib/utils";
import { CreateTicketDialog } from "@/components/tickets/CreateTicketDialog";
import Link from "next/link";
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
} from "lucide-react";
import type {
  Ticket,
  KanbanBoard,
  KanbanColumn as KanbanColumnType,
} from "@/types";

type GroupBy = "none" | "assignee" | "priority" | "taskType";

interface CardFields {
  priority: boolean;
  assignee: boolean;
  dueDate: boolean;
  comments: boolean;
  attachments: boolean;
  taskType: boolean;
  taskState: boolean;
  estimatedHours: boolean;
}

interface KanbanConfig {
  groupBy: GroupBy;
  cardFields: CardFields;
}

const defaultCardFields: CardFields = {
  priority: true,
  assignee: true,
  dueDate: true,
  comments: true,
  attachments: true,
  taskType: false,
  taskState: false,
  estimatedHours: false,
};

const defaultConfig: KanbanConfig = {
  groupBy: "none",
  cardFields: defaultCardFields,
};

const TicketCard = memo(function TicketCard({
  ticket,
  code,
  cardFields,
  isDragging = false,
}: {
  ticket: Ticket;
  code: string;
  cardFields: CardFields;
  isDragging?: boolean;
}) {
  return (
    <Link
      href={`/projects/${code}/tickets/${ticket.key}`}
      className={cn(
        "block rounded-lg border border-border bg-card p-3 hover:shadow-sm transition-shadow cursor-pointer",
        isDragging && "shadow-lg opacity-90 rotate-2"
      )}
    >
      <p className="text-xs font-mono text-muted-foreground mb-1">{ticket.key}</p>
      <p className="text-sm font-medium text-card-foreground mb-2 line-clamp-2">
        {cardFields.taskType && ticket.taskTypeIcon && (
          <span className="mr-1">{ticket.taskTypeIcon}</span>
        )}
        {ticket.title}
      </p>
      <div className="flex items-center justify-between">
        {cardFields.priority ? (
          <PriorityBadge name={ticket.ticketPriorityName} color={ticket.ticketPriorityColor} />
        ) : (
          <span />
        )}
        <div className="flex items-center gap-2 text-muted-foreground">
          {cardFields.comments && ticket.commentsCount > 0 && (
            <span className="flex items-center gap-0.5 text-xs">
              <MessageSquare className="h-3 w-3" />
              {ticket.commentsCount}
            </span>
          )}
          {cardFields.attachments && ticket.attachmentsCount > 0 && (
            <span className="flex items-center gap-0.5 text-xs">
              <Paperclip className="h-3 w-3" />
              {ticket.attachmentsCount}
            </span>
          )}
          {cardFields.estimatedHours && ticket.estimatedHours != null && (
            <span className="flex items-center gap-0.5 text-xs">
              <Clock className="h-3 w-3" />
              {ticket.estimatedHours}h
            </span>
          )}
        </div>
      </div>
      <div className="flex items-center justify-between mt-2 flex-wrap gap-1">
        {cardFields.assignee && ticket.assignee ? (
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <User className="h-3 w-3" />
            {ticket.assignee.displayName}
          </span>
        ) : (
          <span />
        )}
        {cardFields.dueDate && ticket.dueDate && (
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <Calendar className="h-3 w-3" />
            {new Date(ticket.dueDate).toLocaleDateString()}
          </span>
        )}
      </div>
      {cardFields.taskState && (
        <div className="mt-2">
          <DynamicStateBadge
            name={ticket.taskStateName}
            color={ticket.taskStateColor}
          />
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
  cardFields: CardFields;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: ticket.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      <TicketCard
        ticket={ticket}
        code={code}
        cardFields={cardFields}
      />
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
  cardFields: CardFields;
}) {
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
        style={{ borderTopColor: column.color ?? 'transparent' }}
      >
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-semibold text-foreground">
            {column.name}
          </h3>
          <span className="text-xs text-muted-foreground bg-muted rounded-full px-2 py-0.5">
            {column.tickets.length}
          </span>
        </div>
        {column.wipLimit && (
          <span className="text-xs text-muted-foreground">
            WIP: {column.wipLimit}
          </span>
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
          <p className="text-xs text-muted-foreground text-center py-8">
            Drop tickets here
          </p>
        )}
        {isAdding ? (
          <div className="p-2 space-y-2">
            <input
              autoFocus
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleCreate();
                if (e.key === "Escape") { setIsAdding(false); setNewTitle(""); }
              }}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
              placeholder="Task title..."
            />
            <div className="flex gap-1">
              <button
                onClick={handleCreate}
                disabled={createTicket.isPending}
                className="px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
              >
                Add
              </button>
              <button
                onClick={() => { setIsAdding(false); setNewTitle(""); }}
                className="px-3 py-1.5 rounded-lg border border-border text-xs text-muted-foreground hover:text-foreground"
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <button
            onClick={() => setIsAdding(true)}
            className="w-full text-left px-2 py-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            + New
          </button>
        )}
      </div>
    </div>
  );
});

function getSwimlaneName(ticket: Ticket, groupBy: GroupBy): string {
  switch (groupBy) {
    case "assignee":
      return ticket.assignee?.displayName || "Unassigned";
    case "priority":
      return ticket.ticketPriorityName;
    case "taskType":
      return ticket.taskTypeName || "No type";
    default:
      return "";
  }
}

function buildSwimlanes(
  board: KanbanBoard,
  groupBy: GroupBy
): { name: string; columns: KanbanColumnType[] }[] {
  if (groupBy === "none") {
    return [{ name: "", columns: board.columns }];
  }

  const allTickets = board.columns.flatMap((c) =>
    c.tickets.map((t) => ({ ...t, _columnId: c.id }))
  );

  const groupNames = Array.from(
    new Set(allTickets.map((t) => getSwimlaneName(t, groupBy)))
  ).sort();

  return groupNames.map((name) => ({
    name,
    columns: board.columns.map((col) => ({
      ...col,
      tickets: col.tickets.filter(
        (t) => getSwimlaneName(t, groupBy) === name
      ),
    })),
  }));
}

const groupByOptions: { value: GroupBy; label: string }[] = [
  { value: "none", label: "No grouping" },
  { value: "assignee", label: "Assignee" },
  { value: "priority", label: "Priority" },
  { value: "taskType", label: "Task Type" },
];

const cardFieldLabels: { key: keyof CardFields; label: string }[] = [
  { key: "priority", label: "Priority" },
  { key: "assignee", label: "Assignee" },
  { key: "dueDate", label: "Due Date" },
  { key: "comments", label: "Comments" },
  { key: "attachments", label: "Attachments" },
  { key: "taskType", label: "Task Type" },
  { key: "taskState", label: "Task State" },
  { key: "estimatedHours", label: "Est. Hours" },
];

export default function BoardPage({
  params,
}: {
  params: Promise<{ code: string }>;
}) {
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
        cardFields: { ...defaultCardFields, ...parsed.cardFields },
      };
    } catch {
      return defaultConfig;
    }
  }, [viewConfig?.configurationJson]);

  const [groupBy, setGroupBy] = useState<GroupBy>(savedConfig.groupBy);
  const [cardFields, setCardFields] = useState<CardFields>(
    savedConfig.cardFields
  );

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
    (key: keyof CardFields) => {
      setCardFields((prev) => {
        const next = { ...prev, [key]: !prev[key] };
        persistConfig({ groupBy, cardFields: next });
        return next;
      });
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
      const ticket = board?.columns
        .flatMap((c) => c.tickets)
        .find((t) => t.id === ticketId);
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
      const targetColumnId = over.id as string;

      const targetColumn = board.columns.find((c) => c.id === targetColumnId);
      if (!targetColumn) return;

      moveTicket.mutate({
        projectId,
        ticketId,
        targetColumnId,
        position: targetColumn.tickets.length,
      });
    },
    [board, projectId, moveTicket]
  );

  const swimlanes = useMemo(
    () => (board ? buildSwimlanes(board, groupBy) : []),
    [board, groupBy]
  );

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
        Failed to load board. Please try again.
      </div>
    );
  }

  if (!board) {
    return (
      <EmptyState
        icon={<LayoutGrid className="h-12 w-12" />}
        title="No board configured"
        description="Set up a board for this project in settings."
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
            New Task
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
                ? "Group by"
                : `By ${groupByOptions.find((o) => o.value === groupBy)?.label}`}
            </button>
            {showGroupBy && (
              <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg py-1 w-44">
                {groupByOptions.map((opt) => (
                  <button
                    key={opt.value}
                    onClick={() => handleGroupByChange(opt.value)}
                    className={cn(
                      "w-full text-left px-3 py-1.5 text-sm hover:bg-muted transition-colors",
                      groupBy === opt.value &&
                        "text-primary font-medium bg-muted/50"
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
              Card fields
            </button>
            {showCardSettings && (
              <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg p-3 w-48">
                <p className="text-xs font-medium text-muted-foreground mb-2">
                  Show on cards
                </p>
                {cardFieldLabels.map(({ key, label }) => (
                  <label
                    key={key}
                    className="flex items-center gap-2 py-1 text-sm cursor-pointer"
                  >
                    <input
                      type="checkbox"
                      checked={cardFields[key]}
                      onChange={() => handleCardFieldToggle(key)}
                      className="rounded"
                    />
                    {label}
                  </label>
                ))}
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
                  <h3 className="text-sm font-semibold text-foreground">
                    {lane.name}
                  </h3>
                  <span className="text-xs text-muted-foreground">
                    (
                    {lane.columns.reduce(
                      (sum, c) => sum + c.tickets.length,
                      0
                    )}
                    )
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
              <TicketCard
                ticket={activeTicket}
                code={code}
                cardFields={cardFields}
                isDragging
              />
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

