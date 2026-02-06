"use client";

import { use, useState, useCallback } from "react";
import { useBoard, useMoveTicket } from "@/queries/kanban";
import { useProject } from "@/queries/projects";
import { KanbanSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { PriorityBadge } from "@/components/shared/priority-badge";
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
import Link from "next/link";
import {
  MessageSquare,
  Paperclip,
  User,
  Calendar,
  LayoutGrid,
} from "lucide-react";
import type { Ticket, KanbanColumn as KanbanColumnType } from "@/types";

function TicketCard({
  ticket,
  projectId,
  isDragging = false,
}: {
  ticket: Ticket;
  projectId: string;
  isDragging?: boolean;
}) {
  return (
    <Link
      href={`/projects/${projectId}/tickets/${ticket.id}`}
      className={cn(
        "block rounded-lg border border-border bg-card p-3 hover:shadow-sm transition-shadow cursor-pointer",
        isDragging && "shadow-lg opacity-90 rotate-2"
      )}
    >
      <p className="text-sm font-medium text-card-foreground mb-2 line-clamp-2">
        {ticket.title}
      </p>
      <div className="flex items-center justify-between">
        <PriorityBadge priority={ticket.priority} />
        <div className="flex items-center gap-2 text-muted-foreground">
          {ticket.commentsCount > 0 && (
            <span className="flex items-center gap-0.5 text-xs">
              <MessageSquare className="h-3 w-3" />
              {ticket.commentsCount}
            </span>
          )}
          {ticket.attachmentsCount > 0 && (
            <span className="flex items-center gap-0.5 text-xs">
              <Paperclip className="h-3 w-3" />
              {ticket.attachmentsCount}
            </span>
          )}
        </div>
      </div>
      <div className="flex items-center justify-between mt-2">
        {ticket.assignee ? (
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <User className="h-3 w-3" />
            {ticket.assignee.displayName}
          </span>
        ) : (
          <span />
        )}
        {ticket.dueDate && (
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <Calendar className="h-3 w-3" />
            {new Date(ticket.dueDate).toLocaleDateString()}
          </span>
        )}
      </div>
    </Link>
  );
}

function SortableTicketCard({
  ticket,
  projectId,
}: {
  ticket: Ticket;
  projectId: string;
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
      <TicketCard ticket={ticket} projectId={projectId} />
    </div>
  );
}

function KanbanColumn({
  column,
  projectId,
}: {
  column: KanbanColumnType;
  projectId: string;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: column.id });

  return (
    <div className="min-w-[300px] max-w-[300px] flex flex-col">
      <div className="flex items-center justify-between mb-3 px-1">
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
              projectId={projectId}
            />
          ))}
        </SortableContext>
        {column.tickets.length === 0 && (
          <p className="text-xs text-muted-foreground text-center py-8">
            Drop tickets here
          </p>
        )}
      </div>
    </div>
  );
}

export default function BoardPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id: projectId } = use(params);
  const { data: project } = useProject(projectId);
  const { data: board, isLoading, error } = useBoard(projectId);
  const moveTicket = useMoveTicket();
  const [activeTicket, setActiveTicket] = useState<Ticket | null>(null);

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

      // Find which column the ticket is being dropped to
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
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold text-foreground">
          {project?.name || "Board"}
        </h1>
        <p className="text-sm text-muted-foreground">
          {board.name}
        </p>
      </div>

      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div className="flex gap-4 overflow-x-auto pb-4">
          {board.columns
            .sort((a, b) => a.position - b.position)
            .map((column) => (
              <KanbanColumn
                key={column.id}
                column={column}
                projectId={projectId}
              />
            ))}
        </div>

        <DragOverlay>
          {activeTicket && (
            <div className="w-[280px]">
              <TicketCard
                ticket={activeTicket}
                projectId={projectId}
                isDragging
              />
            </div>
          )}
        </DragOverlay>
      </DndContext>
    </div>
  );
}
