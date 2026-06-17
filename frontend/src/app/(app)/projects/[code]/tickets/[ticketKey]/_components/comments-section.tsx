"use client";

import { useState } from "react";
import {
  useComments,
  useCreateComment,
  useDeleteComment,
  useUpdateComment,
} from "@/queries/comments";
import { useCurrentUser } from "@/queries/auth";
import { GlobalRole, ProjectRole } from "@/types";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { MarkdownContent } from "@/components/shared/markdown-content";
import { MarkdownEditor } from "@/components/shared/markdown-editor";
import { MessageSquare, Send, Trash2, Pencil, X } from "lucide-react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createCommentSchema, type CreateCommentInput } from "@/schemas/comment";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import type { Comment } from "@/types";
import { format } from "date-fns";

export function CommentCard({
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
  const [draftInternal, setDraftInternal] = useState(comment.isInternal);

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
        isInternal: draftInternal,
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
          {comment.externalUser && (
            <span
              className="text-[10px] px-1.5 py-0.5 rounded bg-blue-100 text-blue-700 font-medium"
              title={t("externalUserTitle")}
            >
              {t("externalUserBadge", { user: comment.externalUser })}
            </span>
          )}
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
                  setDraftInternal(comment.isInternal);
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
          <MarkdownEditor
            value={draft}
            onChange={setDraft}
            projectId={projectId}
            ticketId={ticketId}
            rows={3}
            autoFocus
          />
          <div className="flex items-center justify-between gap-2">
            <label className="flex items-center gap-2 text-xs text-muted-foreground">
              <input
                type="checkbox"
                checked={draftInternal}
                onChange={(e) => setDraftInternal(e.target.checked)}
                className="rounded"
              />
              {t("internalOnly")}
            </label>
            <div className="flex items-center gap-2">
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
        </div>
      ) : (
        <MarkdownContent content={comment.content} />
      )}
    </div>
  );
}

export function CommentsSection({ projectId, ticketId }: { projectId: string; ticketId: string }) {
  const t = useTranslations("TicketDetail");
  const { data: comments, isLoading } = useComments(projectId, ticketId);
  const { data: currentUser } = useCurrentUser();
  const createComment = useCreateComment();
  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateCommentInput>({
    resolver: zodResolver(createCommentSchema),
    defaultValues: { content: "", isInternal: true },
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
        <Controller
          control={control}
          name="content"
          render={({ field }) => (
            <MarkdownEditor
              value={field.value ?? ""}
              onChange={field.onChange}
              projectId={projectId}
              ticketId={ticketId}
              rows={3}
              placeholder={t("writeComment")}
            />
          )}
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
