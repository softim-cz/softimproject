"use client";

import { use, useRef, useState } from "react";
import { useTicketByNumber } from "@/queries/tickets";
import { useProjectByCode } from "@/queries/projects";
import {
  useComments,
  useCreateComment,
  useDeleteComment,
  useUpdateComment,
} from "@/queries/comments";
import { useCurrentUser } from "@/queries/auth";
import { GlobalRole, ProjectRole } from "@/types";
import {
  MAX_ATTACHMENT_SIZE_BYTES,
  useAttachments,
  useDeleteAttachment,
  useUploadAttachment,
} from "@/queries/attachments";
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
  Download,
  Trash2,
  FileText,
  UploadCloud,
  Pencil,
  X,
} from "lucide-react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createCommentSchema, type CreateCommentInput } from "@/schemas/comment";
import { toast } from "sonner";
import type { Comment, ChecklistItem } from "@/types";
import { format } from "date-fns";

function ChecklistSection({ items }: { items: ChecklistItem[] }) {
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

function CommentCard({
  comment,
  projectId,
  ticketId,
  canManage,
}: {
  comment: Comment;
  projectId: string;
  ticketId: string;
  canManage: boolean;
}) {
  const updateComment = useUpdateComment();
  const deleteComment = useDeleteComment();
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState(comment.content);

  const handleSave = async () => {
    const content = draft.trim();
    if (!content) {
      toast.error("Comment cannot be empty");
      return;
    }
    try {
      await updateComment.mutateAsync({
        projectId,
        ticketId,
        commentId: comment.id,
        content,
      });
      toast.success("Comment updated");
      setIsEditing(false);
    } catch {
      toast.error("Failed to update comment");
    }
  };

  const handleDelete = async () => {
    if (!window.confirm("Delete this comment?")) return;
    try {
      await deleteComment.mutateAsync({ projectId, ticketId, commentId: comment.id });
      toast.success("Comment deleted");
    } catch {
      toast.error("Failed to delete comment");
    }
  };

  return (
    <div className="rounded-lg border border-border p-4">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <div className="h-6 w-6 rounded-full bg-primary-navy text-white flex items-center justify-center text-[10px] font-bold">
            {comment.author.displayName
              .split(" ")
              .map((n) => n[0])
              .join("")
              .slice(0, 2)}
          </div>
          <span className="text-sm font-medium text-foreground">{comment.author.displayName}</span>
          {comment.isInternal && (
            <span className="text-[10px] px-1.5 py-0.5 rounded bg-yellow-100 text-yellow-700 font-medium">
              Internal
            </span>
          )}
          {comment.updatedAt && (
            <span
              className="text-[10px] text-muted-foreground italic"
              title={format(new Date(comment.updatedAt), "MMM d, yyyy HH:mm")}
            >
              edited
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">
            {format(new Date(comment.createdAt), "MMM d, yyyy HH:mm")}
          </span>
          {canManage && !isEditing && (
            <>
              <button
                onClick={() => {
                  setDraft(comment.content);
                  setIsEditing(true);
                }}
                className="p-1 text-muted-foreground hover:text-foreground rounded"
                title="Edit"
                aria-label="Edit comment"
              >
                <Pencil className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteComment.isPending}
                className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                title="Delete"
                aria-label="Delete comment"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </>
          )}
        </div>
      </div>

      {isEditing ? (
        <div className="space-y-2">
          <textarea
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            rows={3}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
          />
          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={() => setIsEditing(false)}
              disabled={updateComment.isPending}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
            >
              <X className="h-3.5 w-3.5" />
              Cancel
            </button>
            <button
              type="button"
              onClick={handleSave}
              disabled={updateComment.isPending}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
            >
              <Send className="h-3.5 w-3.5" />
              Save
            </button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-foreground whitespace-pre-wrap">{comment.content}</p>
      )}
    </div>
  );
}

function CommentsSection({ projectId, ticketId }: { projectId: string; ticketId: string }) {
  const { data: comments, isLoading } = useComments(projectId, ticketId);
  const { data: currentUser } = useCurrentUser();
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
        {errors.content && <p className="text-xs text-destructive">{errors.content.message}</p>}
        <div className="flex items-center justify-between">
          <label className="flex items-center gap-2 text-sm text-muted-foreground">
            <input type="checkbox" {...register("isInternal")} className="rounded" />
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
          {comments.map((comment: Comment) => {
            const canManage =
              !!currentUser &&
              (currentUser.id === comment.author.id ||
                currentUser.globalRole === GlobalRole.Admin ||
                currentUser.projectRoles.some(
                  (pr) => pr.projectId === projectId && pr.role === ProjectRole.ProjectManager
                ));
            return (
              <CommentCard
                key={comment.id}
                comment={comment}
                projectId={projectId}
                ticketId={ticketId}
                canManage={canManage}
              />
            );
          })}
        </div>
      )}

      {comments && comments.length === 0 && (
        <p className="text-sm text-muted-foreground text-center py-4">No comments yet.</p>
      )}
    </div>
  );
}

function AttachmentsSection({ projectId, ticketId }: { projectId: string; ticketId: string }) {
  const { data: attachments, isLoading } = useAttachments(projectId, ticketId);
  const { data: currentUser } = useCurrentUser();
  const deleteAttachment = useDeleteAttachment();
  const uploadAttachment = useUploadAttachment();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [uploading, setUploading] = useState<{ name: string; percent: number } | null>(null);

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const canDelete = (uploadedById: string) =>
    !!currentUser &&
    (currentUser.id === uploadedById ||
      currentUser.globalRole === GlobalRole.Admin ||
      currentUser.projectRoles.some(
        (pr) => pr.projectId === projectId && pr.role === ProjectRole.ProjectManager
      ));

  const handleDelete = async (attachmentId: string) => {
    if (!window.confirm("Delete this attachment?")) return;
    try {
      await deleteAttachment.mutateAsync({ projectId, ticketId, attachmentId });
      toast.success("Attachment deleted");
    } catch {
      toast.error("Failed to delete attachment");
    }
  };

  const uploadFiles = async (files: FileList | File[]) => {
    const list = Array.from(files);
    for (const file of list) {
      if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
        toast.error(`${file.name} is larger than 50 MB`);
        continue;
      }
      setUploading({ name: file.name, percent: 0 });
      try {
        await uploadAttachment.mutateAsync({
          projectId,
          ticketId,
          file,
          onProgress: (percent) => setUploading({ name: file.name, percent }),
        });
        toast.success(`Uploaded ${file.name}`);
      } catch {
        toast.error(`Failed to upload ${file.name}`);
      }
    }
    setUploading(null);
  };

  const handleFileInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files.length > 0) {
      void uploadFiles(event.target.files);
    }
    event.target.value = "";
  };

  const handleDrop = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    if (event.dataTransfer.files && event.dataTransfer.files.length > 0) {
      void uploadFiles(event.dataTransfer.files);
    }
  };

  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <h3 className="text-sm font-semibold text-foreground flex items-center gap-2 mb-3">
        <Paperclip className="h-4 w-4" />
        Attachments
      </h3>

      <div
        onDragOver={(event) => {
          event.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={`mb-3 flex flex-col items-center justify-center gap-1 rounded border-2 border-dashed p-4 text-center text-sm cursor-pointer transition-colors ${
          isDragging
            ? "border-primary bg-primary/5 text-foreground"
            : "border-border text-muted-foreground hover:border-primary/60 hover:text-foreground"
        } ${uploading ? "pointer-events-none opacity-60" : ""}`}
      >
        <UploadCloud className="h-5 w-5" />
        {uploading ? (
          <>
            <span className="truncate max-w-full">Uploading {uploading.name}</span>
            <div className="w-full max-w-xs h-1.5 rounded bg-muted overflow-hidden">
              <div
                className="h-full bg-primary transition-all"
                style={{ width: `${uploading.percent}%` }}
              />
            </div>
          </>
        ) : (
          <>
            <span>Drop files here or click to upload</span>
            <span className="text-xs">Max 50 MB per file</span>
          </>
        )}
        <input
          ref={fileInputRef}
          type="file"
          multiple
          className="hidden"
          onChange={handleFileInputChange}
        />
      </div>

      {isLoading && <Skeleton className="h-16 w-full" />}

      {attachments && attachments.length === 0 && !uploading && (
        <p className="text-sm text-muted-foreground">No attachments yet.</p>
      )}

      {attachments && attachments.length > 0 && (
        <div className="space-y-2">
          {attachments.map((att) => (
            <div
              key={att.id}
              className="flex items-center gap-2 p-2 rounded border border-border bg-muted/30"
            >
              <FileText className="h-4 w-4 text-muted-foreground shrink-0" />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-foreground truncate">{att.fileName}</p>
                <p className="text-xs text-muted-foreground">
                  {formatFileSize(att.fileSizeBytes)} &middot; {att.uploadedByName}
                </p>
              </div>
              <a
                href={att.blobUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="p-1 text-muted-foreground hover:text-foreground rounded"
                title="Download"
              >
                <Download className="h-4 w-4" />
              </a>
              {canDelete(att.uploadedById) && (
                <button
                  onClick={() => handleDelete(att.id)}
                  disabled={deleteAttachment.isPending}
                  className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                  title="Delete"
                  aria-label="Delete attachment"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default function TicketDetailPage({
  params,
}: {
  params: Promise<{ code: string; ticketKey: string }>;
}) {
  const { code, ticketKey } = use(params);
  const ticketNumber = parseInt(ticketKey.split("-").pop() || "0", 10);
  const { data: project } = useProjectByCode(code);
  const projectId = project?.id ?? "";
  const { data: ticket, isLoading, error } = useTicketByNumber(projectId, ticketNumber);

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
        href={`/projects/${code}/board`}
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
              <span className="font-mono text-muted-foreground mr-2">{ticket.key}</span>
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
              <h3 className="text-sm font-semibold text-foreground mb-2">Description</h3>
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
                <h3 className="text-sm font-semibold text-purple-900">AI Summary</h3>
              </div>
              <p className="text-sm text-purple-800">{ticket.aiSummary}</p>
            </div>
          )}

          {/* Checklist */}
          <ChecklistSection items={ticket.checklistItems} />

          {/* Comments */}
          <CommentsSection projectId={projectId} ticketId={ticket.id} />
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <div className="rounded-lg border border-border bg-card p-4 space-y-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Status
              </label>
              <div className="mt-1">
                <StatusBadge name={ticket.taskStateName} color={ticket.taskStateColor} />
              </div>
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Priority
              </label>
              <div className="mt-1">
                <PriorityBadge
                  name={ticket.ticketPriorityName}
                  color={ticket.ticketPriorityColor}
                />
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
                  {ticket.reporter?.displayName ?? "Unknown"}
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
                  <span className="text-sm text-foreground">{ticket.estimatedHours}h</span>
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
          <AttachmentsSection projectId={projectId} ticketId={ticket.id} />
        </div>
      </div>
    </div>
  );
}
