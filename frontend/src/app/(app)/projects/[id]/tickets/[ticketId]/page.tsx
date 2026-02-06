"use client";

import { use, useState } from "react";
import { useTicket, useUpdateTicket } from "@/queries/tickets";
import { useComments, useCreateComment } from "@/queries/comments";
import { useProject } from "@/queries/projects";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge } from "@/components/shared/status-badge";
import { Skeleton } from "@/components/shared/loading-skeleton";
import {
  User,
  Calendar,
  Clock,
  MessageSquare,
  Paperclip,
  Sparkles,
  ChevronLeft,
  CheckSquare,
  Square,
  Send,
} from "lucide-react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createCommentSchema,
  type CreateCommentInput,
} from "@/schemas/comment";
import { toast } from "sonner";
import { TicketStatus, TicketPriority } from "@/types";
import type { Comment, ChecklistItem } from "@/types";
import { format } from "date-fns";

function ChecklistSection({
  items,
}: {
  items: ChecklistItem[];
}) {
  if (!items || items.length === 0) return null;

  const completed = items.filter((i) => i.isCompleted).length;
  const total = items.length;
  const percentage = Math.round((completed / total) * 100);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground">Checklist</h3>
        <span className="text-xs text-muted-foreground">
          {completed}/{total} ({percentage}%)
        </span>
      </div>
      <div className="h-1.5 bg-muted rounded-full overflow-hidden">
        <div
          className="h-full rounded-full bg-green-500 transition-all"
          style={{ width: `${percentage}%` }}
        />
      </div>
      <div className="space-y-1">
        {items
          .sort((a, b) => a.position - b.position)
          .map((item) => (
            <div key={item.id} className="flex items-center gap-2 py-1">
              {item.isCompleted ? (
                <CheckSquare className="h-4 w-4 text-green-600 shrink-0" />
              ) : (
                <Square className="h-4 w-4 text-muted-foreground shrink-0" />
              )}
              <span
                className={`text-sm ${item.isCompleted ? "line-through text-muted-foreground" : "text-foreground"}`}
              >
                {item.text}
              </span>
            </div>
          ))}
      </div>
    </div>
  );
}

function CommentsSection({
  projectId,
  ticketId,
}: {
  projectId: string;
  ticketId: string;
}) {
  const { data: comments, isLoading } = useComments(projectId, ticketId);
  const createComment = useCreateComment();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateCommentInput>({
    resolver: zodResolver(createCommentSchema),
    defaultValues: { isInternal: true },
  });

  const onSubmit = async (data: CreateCommentInput) => {
    try {
      await createComment.mutateAsync({
        projectId,
        ticketId,
        content: data.content,
        isInternal: data.isInternal,
      });
      reset();
      toast.success("Comment added");
    } catch {
      toast.error("Failed to add comment");
    }
  };

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
        <MessageSquare className="h-4 w-4" />
        Comments
      </h3>

      {/* Comment form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <textarea
          {...register("content")}
          rows={3}
          className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
          placeholder="Write a comment..."
        />
        {errors.content && (
          <p className="text-xs text-destructive">{errors.content.message}</p>
        )}
        <div className="flex items-center justify-between">
          <label className="flex items-center gap-2 text-sm text-muted-foreground">
            <input
              type="checkbox"
              {...register("isInternal")}
              className="rounded"
            />
            Internal only
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <Send className="h-3.5 w-3.5" />
            Comment
          </button>
        </div>
      </form>

      {/* Comments list */}
      {isLoading && (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </div>
      )}

      {comments && comments.length > 0 && (
        <div className="space-y-3">
          {comments.map((comment: Comment) => (
            <div
              key={comment.id}
              className="rounded-lg border border-border p-4"
            >
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <div className="h-6 w-6 rounded-full bg-primary-navy text-white flex items-center justify-center text-[10px] font-bold">
                    {comment.author.displayName
                      .split(" ")
                      .map((n) => n[0])
                      .join("")
                      .slice(0, 2)}
                  </div>
                  <span className="text-sm font-medium text-foreground">
                    {comment.author.displayName}
                  </span>
                  {comment.isInternal && (
                    <span className="text-[10px] px-1.5 py-0.5 rounded bg-yellow-100 text-yellow-700 font-medium">
                      Internal
                    </span>
                  )}
                </div>
                <span className="text-xs text-muted-foreground">
                  {format(new Date(comment.createdAt), "MMM d, yyyy HH:mm")}
                </span>
              </div>
              <p className="text-sm text-foreground whitespace-pre-wrap">
                {comment.content}
              </p>
            </div>
          ))}
        </div>
      )}

      {comments && comments.length === 0 && (
        <p className="text-sm text-muted-foreground text-center py-4">
          No comments yet.
        </p>
      )}
    </div>
  );
}

export default function TicketDetailPage({
  params,
}: {
  params: Promise<{ id: string; ticketId: string }>;
}) {
  const { id: projectId, ticketId } = use(params);
  const { data: project } = useProject(projectId);
  const { data: ticket, isLoading, error } = useTicket(projectId, ticketId);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-2/3" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error || !ticket) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        Failed to load ticket. Please try again.
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back link */}
      <Link
        href={`/projects/${projectId}/board`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ChevronLeft className="h-4 w-4" />
        Back to board
      </Link>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main content */}
        <div className="lg:col-span-2 space-y-6">
          <div>
            <h1 className="text-2xl font-bold text-foreground">
              {ticket.title}
            </h1>
            {ticket.externalUrl && (
              <a
                href={ticket.externalUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-sm text-accent-orange hover:underline"
              >
                {ticket.externalId || "External link"}
              </a>
            )}
          </div>

          {/* Description */}
          {ticket.description && (
            <div className="prose prose-sm max-w-none text-foreground">
              <h3 className="text-sm font-semibold text-foreground mb-2">
                Description
              </h3>
              <div className="rounded-lg border border-border p-4 bg-muted/30">
                <p className="whitespace-pre-wrap text-sm">{ticket.description}</p>
              </div>
            </div>
          )}

          {/* AI Summary */}
          {ticket.aiSummary && (
            <div className="rounded-lg border border-purple-200 bg-purple-50 p-4">
              <div className="flex items-center gap-2 mb-2">
                <Sparkles className="h-4 w-4 text-purple-600" />
                <h3 className="text-sm font-semibold text-purple-900">
                  AI Summary
                </h3>
              </div>
              <p className="text-sm text-purple-800">{ticket.aiSummary}</p>
            </div>
          )}

          {/* Checklist */}
          <ChecklistSection items={ticket.checklistItems} />

          {/* Comments */}
          <CommentsSection projectId={projectId} ticketId={ticketId} />
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <div className="rounded-lg border border-border bg-card p-4 space-y-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Status
              </label>
              <div className="mt-1">
                <StatusBadge status={ticket.status} />
              </div>
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Priority
              </label>
              <div className="mt-1">
                <PriorityBadge priority={ticket.priority} />
              </div>
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Assignee
              </label>
              <div className="mt-1 flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm text-foreground">
                  {ticket.assignee?.displayName || "Unassigned"}
                </span>
              </div>
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Reporter
              </label>
              <div className="mt-1 flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm text-foreground">
                  {ticket.reporter.displayName}
                </span>
              </div>
            </div>

            {ticket.dueDate && (
              <div>
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Due Date
                </label>
                <div className="mt-1 flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground">
                    {format(new Date(ticket.dueDate), "MMM d, yyyy")}
                  </span>
                </div>
              </div>
            )}

            {ticket.estimatedHours && (
              <div>
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Estimated Hours
                </label>
                <div className="mt-1 flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground">
                    {ticket.estimatedHours}h
                  </span>
                </div>
              </div>
            )}

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Created
              </label>
              <p className="text-sm text-foreground mt-1">
                {format(new Date(ticket.createdAt), "MMM d, yyyy HH:mm")}
              </p>
            </div>

            {ticket.updatedAt && (
              <div>
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Updated
                </label>
                <p className="text-sm text-foreground mt-1">
                  {format(new Date(ticket.updatedAt), "MMM d, yyyy HH:mm")}
                </p>
              </div>
            )}
          </div>

          {/* Attachments section */}
          <div className="rounded-lg border border-border bg-card p-4">
            <h3 className="text-sm font-semibold text-foreground flex items-center gap-2 mb-3">
              <Paperclip className="h-4 w-4" />
              Attachments ({ticket.attachmentsCount})
            </h3>
            {ticket.attachmentsCount === 0 ? (
              <p className="text-sm text-muted-foreground">
                No attachments yet.
              </p>
            ) : (
              <p className="text-sm text-muted-foreground">
                {ticket.attachmentsCount} file(s) attached.
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
