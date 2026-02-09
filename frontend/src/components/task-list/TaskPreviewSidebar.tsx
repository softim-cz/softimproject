"use client";

import { X, ExternalLink } from "lucide-react";
import Link from "next/link";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge, DynamicStateBadge } from "@/components/shared/status-badge";
import type { Ticket } from "@/types";

export function TaskPreviewSidebar({
  ticket,
  projectId,
  onClose,
}: {
  ticket: Ticket | null;
  projectId: string;
  onClose: () => void;
}) {
  if (!ticket) return null;

  return (
    <div className="w-96 border-l border-border bg-card flex flex-col h-full overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-border">
        <h3 className="text-sm font-semibold text-foreground truncate flex-1">
          {ticket.title}
        </h3>
        <div className="flex items-center gap-1 ml-2">
          <Link
            href={`/projects/${projectId}/tickets/${ticket.id}`}
            className="p-1 text-muted-foreground hover:text-foreground rounded"
          >
            <ExternalLink className="h-4 w-4" />
          </Link>
          <button
            onClick={onClose}
            className="p-1 text-muted-foreground hover:text-foreground rounded"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Status & Priority */}
        <div className="flex items-center gap-2 flex-wrap">
          <StatusBadge status={ticket.status} />
          <PriorityBadge priority={ticket.priority} />
          {ticket.taskStateName && ticket.taskStateColor && (
            <DynamicStateBadge name={ticket.taskStateName} color={ticket.taskStateColor} />
          )}
        </div>

        {/* Task Type */}
        {ticket.taskTypeName && (
          <div>
            <p className="text-xs font-medium text-muted-foreground mb-1">Type</p>
            <p className="text-sm">
              {ticket.taskTypeIcon && <span className="mr-1">{ticket.taskTypeIcon}</span>}
              {ticket.taskTypeName}
            </p>
          </div>
        )}

        {/* Description */}
        {ticket.description && (
          <div>
            <p className="text-xs font-medium text-muted-foreground mb-1">Description</p>
            <p className="text-sm text-foreground whitespace-pre-wrap line-clamp-6">
              {ticket.description}
            </p>
          </div>
        )}

        {/* Assignee */}
        <div>
          <p className="text-xs font-medium text-muted-foreground mb-1">Assignee</p>
          <p className="text-sm">
            {ticket.assignee?.displayName || "Unassigned"}
          </p>
        </div>

        {/* Due date */}
        {ticket.dueDate && (
          <div>
            <p className="text-xs font-medium text-muted-foreground mb-1">Due Date</p>
            <p className="text-sm">{ticket.dueDate}</p>
          </div>
        )}

        {/* Estimated hours */}
        {ticket.estimatedHours != null && (
          <div>
            <p className="text-xs font-medium text-muted-foreground mb-1">Estimated Hours</p>
            <p className="text-sm">{ticket.estimatedHours}h</p>
          </div>
        )}

        {/* Worked hours */}
        {ticket.cumulativeWorkedHours != null && ticket.cumulativeWorkedHours > 0 && (
          <div>
            <p className="text-xs font-medium text-muted-foreground mb-1">Worked Hours</p>
            <p className="text-sm">{ticket.cumulativeWorkedHours}h</p>
          </div>
        )}

        {/* Last comment */}
        {ticket.lastComment && (
          <div>
            <p className="text-xs font-medium text-muted-foreground mb-1">Last Comment</p>
            <p className="text-sm text-muted-foreground line-clamp-3">{ticket.lastComment}</p>
          </div>
        )}

        {/* Created */}
        <div>
          <p className="text-xs font-medium text-muted-foreground mb-1">Created</p>
          <p className="text-sm">{new Date(ticket.createdAt).toLocaleDateString()}</p>
        </div>
      </div>
    </div>
  );
}
