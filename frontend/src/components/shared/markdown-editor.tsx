"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Eye, Pencil, ImagePlus } from "lucide-react";
import { toast } from "sonner";
import { MarkdownContent } from "./markdown-content";
import { MAX_ATTACHMENT_SIZE_BYTES, useUploadAttachment } from "@/queries/attachments";
import { cn } from "@/lib/utils";

/**
 * Controlled Markdown editor with a Write/Preview toggle and inline image
 * upload. Dropping or pasting an image uploads it as a ticket attachment and
 * inserts a Markdown image reference at the caret.
 */
export function MarkdownEditor({
  value,
  onChange,
  projectId,
  ticketId,
  placeholder,
  rows = 6,
  disabled = false,
  autoFocus = false,
  className,
}: {
  value: string;
  onChange: (value: string) => void;
  projectId: string;
  // Optional: inline image upload requires a ticket to attach to. When absent
  // (e.g. project-level comments) the editor still works, just without uploads.
  ticketId?: string;
  placeholder?: string;
  rows?: number;
  disabled?: boolean;
  autoFocus?: boolean;
  className?: string;
}) {
  const t = useTranslations("Markdown");
  const [tab, setTab] = useState<"write" | "preview">("write");
  const [isDragging, setIsDragging] = useState(false);
  const [uploadingCount, setUploadingCount] = useState(0);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const uploadAttachment = useUploadAttachment();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const insertAtCursor = (snippet: string) => {
    const el = textareaRef.current;
    if (!el) {
      onChange(value + snippet);
      return;
    }
    const start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? value.length;
    const next = value.slice(0, start) + snippet + value.slice(end);
    onChange(next);
    requestAnimationFrame(() => {
      el.focus();
      const pos = start + snippet.length;
      el.setSelectionRange(pos, pos);
    });
  };

  const uploadImages = async (files: File[]) => {
    if (!ticketId) return;
    const images = files.filter((f) => f.type.startsWith("image/"));
    if (images.length === 0) return;
    for (const file of images) {
      if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
        toast.error(t("imageTooLarge", { name: file.name }));
        continue;
      }
      setUploadingCount((c) => c + 1);
      try {
        const att = await uploadAttachment.mutateAsync({ projectId, ticketId, file });
        const alt = file.name.replace(/[[\]]/g, "");
        insertAtCursor(`\n![${alt}](${att.blobUrl})\n`);
      } catch {
        toast.error(t("imageUploadFailed", { name: file.name }));
      } finally {
        setUploadingCount((c) => c - 1);
      }
    }
  };

  const handleDrop = (event: React.DragEvent<HTMLTextAreaElement>) => {
    if (!event.dataTransfer.files?.length) return;
    event.preventDefault();
    setIsDragging(false);
    void uploadImages(Array.from(event.dataTransfer.files));
  };

  const handlePaste = (event: React.ClipboardEvent<HTMLTextAreaElement>) => {
    const files = Array.from(event.clipboardData.files);
    if (files.some((f) => f.type.startsWith("image/"))) {
      event.preventDefault();
      void uploadImages(files);
    }
  };

  const handleFileInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files?.length) {
      void uploadImages(Array.from(event.target.files));
    }
    event.target.value = "";
  };

  return (
    <div className={cn("rounded-lg border border-input bg-background", className)}>
      <div className="flex items-center justify-between border-b border-border px-2 py-1">
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setTab("write")}
            className={cn(
              "inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium transition-colors",
              tab === "write"
                ? "bg-muted text-foreground"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <Pencil className="h-3.5 w-3.5" />
            {t("write")}
          </button>
          <button
            type="button"
            onClick={() => setTab("preview")}
            className={cn(
              "inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium transition-colors",
              tab === "preview"
                ? "bg-muted text-foreground"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <Eye className="h-3.5 w-3.5" />
            {t("preview")}
          </button>
        </div>
        {ticketId && (
          <>
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={disabled || uploadingCount > 0}
              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
              title={t("addImage")}
            >
              <ImagePlus className="h-3.5 w-3.5" />
              {t("addImage")}
            </button>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              multiple
              className="hidden"
              onChange={handleFileInputChange}
            />
          </>
        )}
      </div>

      {tab === "write" ? (
        <div className="relative">
          <textarea
            ref={textareaRef}
            autoFocus={autoFocus}
            value={value}
            onChange={(event) => onChange(event.target.value)}
            onPaste={handlePaste}
            onDrop={handleDrop}
            onDragOver={(event) => {
              if (event.dataTransfer.types.includes("Files")) {
                event.preventDefault();
                setIsDragging(true);
              }
            }}
            onDragLeave={() => setIsDragging(false)}
            rows={rows}
            placeholder={placeholder}
            disabled={disabled}
            className={cn(
              "w-full resize-y bg-transparent px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none disabled:opacity-50",
              isDragging && "ring-2 ring-inset ring-ring"
            )}
          />
          {uploadingCount > 0 && (
            <div className="absolute bottom-2 right-3 rounded bg-primary/90 px-2 py-0.5 text-[10px] font-medium text-primary-foreground">
              {t("uploadingImage")}
            </div>
          )}
        </div>
      ) : (
        <div className="px-3 py-2 min-h-[5rem]">
          {value.trim() ? (
            <MarkdownContent content={value} />
          ) : (
            <p className="text-sm text-muted-foreground italic">{t("previewEmpty")}</p>
          )}
        </div>
      )}

      <div className="border-t border-border px-3 py-1">
        <p className="text-[10px] text-muted-foreground">{t("hint")}</p>
      </div>
    </div>
  );
}
