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
  GitBranch,
  GitPullRequest,
  GitMerge,
  ExternalLink,
} from "lucide-react";
import { useLinkedPullRequests, useCreateTicketBranch } from "@/queries/github";
import { useTicketAiHistory, useResummarizeTicket, type AiInvocation } from "@/queries/ai";
import { cn } from "@/lib/utils";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createCommentSchema, type CreateCommentInput } from "@/schemas/comment";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import type { Comment, ChecklistItem } from "@/types";
import { format } from "date-fns";

function LinkedPullRequestsSection({
  projectId,
  ticketId,
  githubLinked,
}: {
  projectId: string;
  ticketId: string;
  githubLinked: boolean;
}) {
  const t = useTranslations("TicketDetail");
  const { data: prs, isLoading } = useLinkedPullRequests(projectId, ticketId);
  const createBranch = useCreateTicketBranch(projectId, ticketId);

  const handleCreateBranch = async () => {
    try {
      const result = await createBranch.mutateAsync();
      await navigator.clipboard?.writeText(result.branchName).catch(() => {});
      toast.success(t("branchCreated", { name: result.branchName }));
    } catch (err: unknown) {
      const data = (err as { response?: { data?: { message?: string; errors?: string[] } } })
        .response?.data;
      toast.error(data?.errors?.[0] ?? data?.message ?? t("branchCreateFailed"));
    }
  };

  if (!githubLinked) return null;

  const iconFor = (state: "Open" | "Closed" | "Merged") =>
    state === "Merged" ? (
      <GitMerge className="h-4 w-4 text-purple-600 shrink-0" />
    ) : state === "Closed" ? (
      <GitPullRequest className="h-4 w-4 text-muted-foreground shrink-0" />
    ) : (
      <GitPullRequest className="h-4 w-4 text-green-600 shrink-0" />
    );

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
          <GitBranch className="h-4 w-4 text-muted-foreground" />
          {t("linkedPRs")}
        </h3>
        <button
          type="button"
          onClick={handleCreateBranch}
          disabled={createBranch.isPending}
          className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded border border-border text-xs font-medium hover:bg-muted disabled:opacity-50"
        >
          <GitBranch className="h-3.5 w-3.5" />
          {createBranch.isPending ? t("creating") : t("createBranch")}
        </button>
      </div>

      {isLoading && <Skeleton className="h-16 w-full" />}

      {!isLoading && (!prs || prs.length === 0) && (
        <p className="text-xs text-muted-foreground italic">
          {t("noLinkedPRs", { example: "feat/XXX-123-slug" })}
        </p>
      )}

      {prs && prs.length > 0 && (
        <ul className="space-y-2">
          {prs.map((pr) => (
            <li
              key={pr.id}
              className="flex items-center gap-2 p-2 rounded border border-border hover:bg-muted/30"
            >
              {iconFor(pr.state)}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <a
                    href={pr.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm font-medium text-foreground hover:underline truncate"
                  >
                    #{pr.externalId} · {pr.title}
                  </a>
                  <ExternalLink className="h-3 w-3 text-muted-foreground shrink-0" />
                </div>
                <div className="text-xs text-muted-foreground">
                  <span className="font-mono">{pr.branch}</span>
                  {pr.authorLogin && <> · by @{pr.authorLogin}</>}
                  <> · {pr.state.toLowerCase()}</>
                  {pr.mergedAt && <> · merged {format(new Date(pr.mergedAt), "MMM d")}</>}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function AiHistorySection({ projectId, ticketId }: { projectId: string; ticketId: string }) {
  const t = useTranslations("TicketDetail");
  const { data: history, isLoading } = useTicketAiHistory(projectId, ticketId);
  const resummarize = useResummarizeTicket(projectId, ticketId);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [reason, setReason] = useState("");

  const handleSubmit = async () => {
    const trimmed = reason.trim();
    if (trimmed.length < 3) {
      toast.error(t("reasonTooShort"));
      return;
    }
    try {
      await resummarize.mutateAsync(trimmed);
      toast.success(t("resummarizeTriggered"));
      setDialogOpen(false);
      setReason("");
    } catch (err: unknown) {
      const resp = (
        err as { response?: { status?: number; data?: { message?: string; errors?: string[] } } }
      ).response;
      if (resp?.status === 429) {
        toast.error(resp.data?.message ?? t("aiRateLimit"));
      } else {
        toast.error(resp?.data?.errors?.[0] ?? resp?.data?.message ?? t("resummarizeFailed"));
      }
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
          <Sparkles className="h-4 w-4 text-muted-foreground" />
          {t("aiHistory")}
        </h3>
        <button
          type="button"
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded border border-border text-xs font-medium hover:bg-muted"
        >
          <Sparkles className="h-3.5 w-3.5" />
          {t("resummarize")}
        </button>
      </div>

      {isLoading && <Skeleton className="h-12 w-full" />}
      {!isLoading && (!history || history.length === 0) && (
        <p className="text-xs text-muted-foreground italic">{t("noAiActivity")}</p>
      )}

      {history && history.length > 0 && (
        <ul className="space-y-1.5 text-xs">
          {history.map((inv: AiInvocation) => (
            <li
              key={inv.id}
              className="flex items-start gap-2 p-2 rounded border border-border hover:bg-muted/30"
              title={inv.errorMessage ?? inv.outputPreview ?? undefined}
            >
              <span
                className={cn(
                  "px-1.5 py-0.5 rounded font-medium shrink-0",
                  inv.trigger === "ManualResummarize"
                    ? "bg-blue-100 text-blue-700"
                    : inv.trigger === "WeeklyReport"
                      ? "bg-purple-100 text-purple-700"
                      : "bg-gray-100 text-gray-700"
                )}
              >
                {inv.trigger}
              </span>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 text-foreground">
                  <span>{format(new Date(inv.startedAt), "MMM d, HH:mm")}</span>
                  {inv.triggeredByDisplayName && (
                    <span className="text-muted-foreground">· by {inv.triggeredByDisplayName}</span>
                  )}
                  {!inv.success && <span className="text-destructive">· failed</span>}
                </div>
                <div className="text-muted-foreground">
                  {inv.totalTokens} tokens · ${inv.estimatedCostUsd.toFixed(4)}
                  {inv.reason && <> · {inv.reason}</>}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}

      {dialogOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/50" onClick={() => setDialogOpen(false)} />
          <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-md mx-4 p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-base font-semibold text-card-foreground">
                {t("resummarizeTicket")}
              </h2>
              <button onClick={() => setDialogOpen(false)} className="p-1 rounded hover:bg-muted">
                <X className="h-4 w-4 text-muted-foreground" />
              </button>
            </div>
            <label className="block text-xs font-medium text-muted-foreground mb-1">
              {t("reasonLabel")}
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              placeholder={t("reasonPlaceholder")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground resize-none"
            />
            <div className="flex justify-end gap-2 mt-4">
              <button
                type="button"
                onClick={() => setDialogOpen(false)}
                disabled={resummarize.isPending}
                className="px-3 py-1.5 rounded border border-border text-sm hover:bg-muted disabled:opacity-50"
              >
                {t("cancel")}
              </button>
              <button
                type="button"
                onClick={handleSubmit}
                disabled={resummarize.isPending}
                className="px-3 py-1.5 rounded bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50"
              >
                {resummarize.isPending ? t("resummarizing") : t("resummarize")}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function ChecklistSection({ items }: { items: ChecklistItem[] }) {
  const t = useTranslations("TicketDetail");
  if (!items || items.length === 0) return null;

  const completed = items.filter((i) => i.isCompleted).length;
  const total = items.length;
  const percentage = Math.round((completed / total) * 100);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground">{t("checklist")}</h3>
        <span className="text-xs text-muted-foreground">
          {t("checklistProgress", { done: completed, total, percent: percentage })}
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
  const t = useTranslations("TicketDetail");
  const updateComment = useUpdateComment();
  const deleteComment = useDeleteComment();
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState(comment.content);

  const handleSave = async () => {
    const content = draft.trim();
    if (!content) {
      toast.error(t("commentEmpty"));
      return;
    }
    try {
      await updateComment.mutateAsync({
        projectId,
        ticketId,
        commentId: comment.id,
        content,
      });
      toast.success(t("commentUpdated"));
      setIsEditing(false);
    } catch {
      toast.error(t("commentUpdateFailed"));
    }
  };

  const handleDelete = async () => {
    if (!window.confirm(t("commentDeleteConfirm"))) return;
    try {
      await deleteComment.mutateAsync({ projectId, ticketId, commentId: comment.id });
      toast.success(t("commentDeleted"));
    } catch {
      toast.error(t("commentDeleteFailed"));
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
              {t("internalBadge")}
            </span>
          )}
          {comment.updatedAt && (
            <span
              className="text-[10px] text-muted-foreground italic"
              title={format(new Date(comment.updatedAt), "MMM d, yyyy HH:mm")}
            >
              {t("commentEdited")}
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
                title={t("editCommentAriaLabel")}
                aria-label={t("editCommentAriaLabel")}
              >
                <Pencil className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteComment.isPending}
                className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                title={t("deleteCommentAriaLabel")}
                aria-label={t("deleteCommentAriaLabel")}
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
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={handleSave}
              disabled={updateComment.isPending}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
            >
              <Send className="h-3.5 w-3.5" />
              {t("save")}
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
  const t = useTranslations("TicketDetail");
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
      toast.success(t("commentAdded"));
    } catch {
      toast.error(t("commentAddFailed"));
    }
  };

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
        <MessageSquare className="h-4 w-4" />
        {t("comments")}
      </h3>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <textarea
          {...register("content")}
          rows={3}
          className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
          placeholder={t("writeComment")}
        />
        {errors.content && <p className="text-xs text-destructive">{errors.content.message}</p>}
        <div className="flex items-center justify-between">
          <label className="flex items-center gap-2 text-sm text-muted-foreground">
            <input type="checkbox" {...register("isInternal")} className="rounded" />
            {t("internalOnly")}
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <Send className="h-3.5 w-3.5" />
            {t("commentSend")}
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
        <p className="text-sm text-muted-foreground text-center py-4">{t("noCommentsYet")}</p>
      )}
    </div>
  );
}

function AttachmentsSection({ projectId, ticketId }: { projectId: string; ticketId: string }) {
  const t = useTranslations("TicketDetail");
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
    if (!window.confirm(t("deleteAttachmentConfirm"))) return;
    try {
      await deleteAttachment.mutateAsync({ projectId, ticketId, attachmentId });
      toast.success(t("attachmentDeleted"));
    } catch {
      toast.error(t("attachmentDeleteFailed"));
    }
  };

  const uploadFiles = async (files: FileList | File[]) => {
    const list = Array.from(files);
    for (const file of list) {
      if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
        toast.error(t("fileTooLargeError", { name: file.name }));
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
        toast.success(t("uploadedFile", { name: file.name }));
      } catch {
        toast.error(t("uploadFileFailed", { name: file.name }));
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
        {t("attachments")}
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
            <span className="truncate max-w-full">
              {t("uploadingFile", { name: uploading.name })}
            </span>
            <div className="w-full max-w-xs h-1.5 rounded bg-muted overflow-hidden">
              <div
                className="h-full bg-primary transition-all"
                style={{ width: `${uploading.percent}%` }}
              />
            </div>
          </>
        ) : (
          <>
            <span>{t("dropFilesOrClick")}</span>
            <span className="text-xs">{t("maxFileSize")}</span>
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
        <p className="text-sm text-muted-foreground">{t("noAttachmentsYet")}</p>
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
                title={t("downloadAttachment")}
              >
                <Download className="h-4 w-4" />
              </a>
              {canDelete(att.uploadedById) && (
                <button
                  onClick={() => handleDelete(att.id)}
                  disabled={deleteAttachment.isPending}
                  className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                  title={t("deleteAttachmentAriaLabel")}
                  aria-label={t("deleteAttachmentAriaLabel")}
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
  const t = useTranslations("TicketDetail");
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
        {t("loadFailed")}
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-5xl">
      <Link
        href={`/projects/${code}/board`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ChevronLeft className="h-4 w-4" />
        {t("backToBoard")}
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
                {ticket.externalId || t("externalLink")}
              </a>
            )}
          </div>

          {ticket.description && (
            <div className="prose prose-sm max-w-none text-foreground">
              <h3 className="text-sm font-semibold text-foreground mb-2">{t("description")}</h3>
              <div className="rounded-lg border border-border p-4 bg-muted/30">
                <p className="whitespace-pre-wrap text-sm">{ticket.description}</p>
              </div>
            </div>
          )}

          {ticket.aiSummary && (
            <div className="rounded-lg border border-purple-200 bg-purple-50 p-4">
              <div className="flex items-center gap-2 mb-2">
                <Sparkles className="h-4 w-4 text-purple-600" />
                <h3 className="text-sm font-semibold text-purple-900">{t("aiSummaryTitle")}</h3>
              </div>
              <p className="text-sm text-purple-800">{ticket.aiSummary}</p>
            </div>
          )}

          {/* Checklist */}
          <ChecklistSection items={ticket.checklistItems} />

          {/* Linked pull requests (hidden for projects without a GitHub link) */}
          <LinkedPullRequestsSection
            projectId={projectId}
            ticketId={ticket.id}
            githubLinked={project?.externalSystem === "GitHub"}
          />

          {/* AI history + manual re-summarize */}
          <AiHistorySection projectId={projectId} ticketId={ticket.id} />

          {/* Comments */}
          <CommentsSection projectId={projectId} ticketId={ticket.id} />
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <div className="rounded-lg border border-border bg-card p-4 space-y-4">
            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {t("status")}
              </label>
              <div className="mt-1">
                <StatusBadge name={ticket.taskStateName} color={ticket.taskStateColor} />
              </div>
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {t("priority")}
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
                {t("assignee")}
              </label>
              <div className="mt-1 flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm text-foreground">
                  {ticket.assignee?.displayName || t("assignee")}
                </span>
              </div>
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {t("reporter")}
              </label>
              <div className="mt-1 flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm text-foreground">
                  {ticket.reporter?.displayName ?? t("unknownUser")}
                </span>
              </div>
            </div>

            {ticket.dueDate && (
              <div>
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {t("dueDate")}
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
                  {t("estimatedHours")}
                </label>
                <div className="mt-1 flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground">{ticket.estimatedHours}h</span>
                </div>
              </div>
            )}

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {t("createdAt")}
              </label>
              <p className="text-sm text-foreground mt-1">
                {format(new Date(ticket.createdAt), "MMM d, yyyy HH:mm")}
              </p>
            </div>

            {ticket.updatedAt && (
              <div>
                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {t("updatedAt")}
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
