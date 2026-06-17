"use client";

import { useRef, useState } from "react";
import { useCurrentUser } from "@/queries/auth";
import {
  MAX_ATTACHMENT_SIZE_BYTES,
  useAttachments,
  useDeleteAttachment,
  useUploadAttachment,
} from "@/queries/attachments";
import { GlobalRole, ProjectRole } from "@/types";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { Paperclip, Download, Trash2, FileText, UploadCloud } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";

export function AttachmentsSection({
  projectId,
  ticketId,
}: {
  projectId: string;
  ticketId: string;
}) {
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
