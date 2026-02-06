"use client";

import { use } from "react";
import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { KanbanSkeleton, Skeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge } from "@/components/shared/status-badge";
import {
  LayoutGrid,
  Clock,
  MessageSquare,
  User,
  Calendar,
} from "lucide-react";
import type {
  KanbanBoard,
  KanbanColumn,
  Ticket,
  Comment,
  Project,
} from "@/types";

const portalClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "https://localhost:7001",
  headers: { "Content-Type": "application/json" },
});

interface PortalData {
  project: Project;
  board: KanbanBoard;
  totalHours: number;
  comments: Comment[];
}

function usePortalData(token: string) {
  return useQuery({
    queryKey: ["portal", token],
    queryFn: async () => {
      const { data } = await portalClient.get<PortalData>(
        `/api/v1/portal/${token}`
      );
      return data;
    },
    enabled: !!token,
  });
}

function PortalKanbanColumn({ column }: { column: KanbanColumn }) {
  return (
    <div className="min-w-[280px] max-w-[280px] flex flex-col">
      <div className="flex items-center gap-2 mb-3 px-1">
        <h3 className="text-sm font-semibold text-foreground">
          {column.name}
        </h3>
        <span className="text-xs text-muted-foreground bg-muted rounded-full px-2 py-0.5">
          {column.tickets.length}
        </span>
      </div>
      <div className="flex-1 rounded-lg bg-muted/50 p-2 space-y-2 min-h-[200px]">
        {column.tickets.map((ticket: Ticket) => (
          <div
            key={ticket.id}
            className="rounded-lg border border-border bg-card p-3"
          >
            <p className="text-sm font-medium text-card-foreground mb-2 line-clamp-2">
              {ticket.title}
            </p>
            <div className="flex items-center justify-between">
              <PriorityBadge priority={ticket.priority} />
              <StatusBadge status={ticket.status} />
            </div>
            {ticket.assignee && (
              <div className="flex items-center gap-1 text-xs text-muted-foreground mt-2">
                <User className="h-3 w-3" />
                {ticket.assignee.displayName}
              </div>
            )}
            {ticket.dueDate && (
              <div className="flex items-center gap-1 text-xs text-muted-foreground mt-1">
                <Calendar className="h-3 w-3" />
                {new Date(ticket.dueDate).toLocaleDateString()}
              </div>
            )}
          </div>
        ))}
        {column.tickets.length === 0 && (
          <p className="text-xs text-muted-foreground text-center py-8">
            No tickets
          </p>
        )}
      </div>
    </div>
  );
}

export default function PortalPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = use(params);
  const { data, isLoading, error } = usePortalData(token);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background p-6">
        <div className="max-w-7xl mx-auto space-y-6">
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-4 w-96" />
          <KanbanSkeleton />
        </div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-foreground mb-2">
            Access Denied
          </h1>
          <p className="text-muted-foreground">
            This portal link is invalid or has expired.
          </p>
        </div>
      </div>
    );
  }

  const { project, board, totalHours } = data;

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b border-border bg-card px-6 py-4">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-8 w-8 rounded-lg bg-accent-orange flex items-center justify-center font-bold text-white text-sm">
              S
            </div>
            <div>
              <h1 className="text-lg font-bold text-card-foreground">
                {project.name}
              </h1>
              <p className="text-xs text-muted-foreground">
                Client Portal - {project.code}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm text-muted-foreground">Total Hours</p>
              <p className="text-lg font-bold text-foreground">
                {totalHours.toFixed(1)}h
              </p>
            </div>
          </div>
        </div>
      </header>

      {/* Content */}
      <main className="max-w-7xl mx-auto p-6 space-y-6">
        {/* Hours summary */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="rounded-lg border border-border bg-card p-5">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-accent-orange/10 flex items-center justify-center">
                <Clock className="h-5 w-5 text-accent-orange" />
              </div>
              <div>
                <p className="text-2xl font-bold text-card-foreground">
                  {totalHours.toFixed(1)}h
                </p>
                <p className="text-sm text-muted-foreground">Hours Logged</p>
              </div>
            </div>
          </div>
          {project.budgetHours && (
            <div className="rounded-lg border border-border bg-card p-5">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-lg bg-blue-100 flex items-center justify-center">
                  <Clock className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="text-2xl font-bold text-card-foreground">
                    {project.budgetHours}h
                  </p>
                  <p className="text-sm text-muted-foreground">Budget Hours</p>
                </div>
              </div>
            </div>
          )}
          <div className="rounded-lg border border-border bg-card p-5">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-green-100 flex items-center justify-center">
                <LayoutGrid className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-card-foreground">
                  {board.columns.reduce(
                    (sum, c) => sum + c.tickets.length,
                    0
                  )}
                </p>
                <p className="text-sm text-muted-foreground">Total Tickets</p>
              </div>
            </div>
          </div>
        </div>

        {/* Board */}
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-4">
            Project Board
          </h2>
          {board.columns.length > 0 ? (
            <div className="flex gap-4 overflow-x-auto pb-4">
              {board.columns
                .sort((a, b) => a.position - b.position)
                .map((column) => (
                  <PortalKanbanColumn key={column.id} column={column} />
                ))}
            </div>
          ) : (
            <EmptyState
              icon={<LayoutGrid className="h-12 w-12" />}
              title="No board configured"
            />
          )}
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-border mt-12 py-4 text-center text-xs text-muted-foreground">
        Powered by SoftimProject - Softim.cz s.r.o.
      </footer>
    </div>
  );
}
