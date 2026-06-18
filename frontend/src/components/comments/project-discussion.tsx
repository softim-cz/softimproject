"use client";

import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { format } from "date-fns";
import { MessageSquare, Send, Globe } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { useProjectComments, useCreateProjectComment } from "@/queries/comments";
import { createCommentSchema, type CreateCommentInput } from "@/schemas/comment";
import { MarkdownEditor } from "@/components/shared/markdown-editor";
import { MarkdownContent } from "@/components/shared/markdown-content";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { Avatar } from "@/components/shared/avatar";
import type { Comment } from "@/types";

/**
 * Project-level discussion: comments attached to the project itself (TicketId
 * null). The backend exposes list + create only, so cards are read-only.
 */
export function ProjectDiscussion({ projectId }: { projectId: string }) {
  const t = useTranslations("ProjectDiscussion");
  const { data: comments, isLoading } = useProjectComments(projectId);
  const createComment = useCreateProjectComment();

  const {
    control,
    register,
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
        content: data.content,
        isInternal: data.isInternal,
      });
      reset();
      toast.success(t("added"));
    } catch {
      toast.error(t("addFailed"));
    }
  };

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-foreground flex items-center gap-2">
          <MessageSquare className="h-5 w-5 text-muted-foreground" />
          {t("title")}
        </h2>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <Controller
          control={control}
          name="content"
          render={({ field }) => (
            <MarkdownEditor
              value={field.value ?? ""}
              onChange={field.onChange}
              projectId={projectId}
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
            {t("send")}
          </button>
        </div>
      </form>

      {isLoading && (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </div>
      )}

      {comments && comments.length > 0 && (
        <div className="space-y-3">
          {comments.map((comment) => (
            <ProjectCommentCard key={comment.id} comment={comment} />
          ))}
        </div>
      )}

      {comments && comments.length === 0 && (
        <p className="text-sm text-muted-foreground text-center py-8">{t("empty")}</p>
      )}
    </div>
  );
}

function ProjectCommentCard({ comment }: { comment: Comment }) {
  const t = useTranslations("ProjectDiscussion");
  const displayName = comment.externalUser || comment.author.displayName;

  return (
    <div className="rounded-lg border border-border p-4">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <Avatar name={displayName} size="sm" />
          <span className="text-sm font-medium text-foreground">{displayName}</span>
          {comment.externalUser && (
            <span
              className="inline-flex items-center gap-1 text-[10px] px-1.5 py-0.5 rounded bg-blue-100 text-blue-700 font-medium"
              title={t("externalUserTitle")}
            >
              <Globe className="h-3 w-3" />
              {t("externalBadge")}
            </span>
          )}
          {comment.isInternal && (
            <span className="text-[10px] px-1.5 py-0.5 rounded bg-yellow-100 text-yellow-700 font-medium">
              {t("internalBadge")}
            </span>
          )}
        </div>
        <span className="text-xs text-muted-foreground">
          {format(new Date(comment.createdAt), "MMM d, yyyy HH:mm")}
        </span>
      </div>
      <MarkdownContent content={comment.content} />
    </div>
  );
}
