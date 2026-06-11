"use client";

import { use, useEffect, useRef, useState } from "react";
import apiClient from "@/lib/api/client";
import { useTicketByNumber, useUpdateTicket } from "@/queries/tickets";
import { useProjectByCode, useProjectUsers } from "@/queries/projects";
import { useTaskStates, useTicketPriorities } from "@/queries/lookups";
import {
  useComments,
  useCreateComment,
  useDeleteComment,
  useUpdateComment,
} from "@/queries/comments";
import { useCurrentUser } from "@/queries/auth";
import {
  GlobalRole,
  ProjectRole,
  type Ticket,
  type TicketSubTicket,
  type UserOption,
} from "@/types";
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
import { useLocale, useTranslations } from "next-intl";
import type { Comment, ChecklistItem } from "@/types";
import { format } from "date-fns";

type TicketPatch = Partial<
  Pick<
    Ticket,
    | "title"
    | "description"
    | "ticketPriorityId"
    | "taskStateId"
    | "assigneeId"
    | "dueDate"
    | "estimatedHours"
    | "taskTypeId"
    | "parentTicketId"
    | "externalBudget"
    | "externalUser"
    | "externalId"
    | "externalUrl"
    | "externalProject"
    | "implementationNotes"
  >
>;

function buildUpdatePayload(ticket: Ticket, patch: TicketPatch) {
  // Backend UpdateTicketCommand requires the full record; merge current ticket values with the patch.
  return {
    projectId: ticket.projectId,
    id: ticket.id,
    ticketId: ticket.id,
    title: patch.title ?? ticket.title,
    description: patch.description ?? ticket.description ?? null,
    ticketPriorityId: patch.ticketPriorityId ?? ticket.ticketPriorityId,
    taskStateId: patch.taskStateId ?? ticket.taskStateId,
    assigneeId: "assigneeId" in patch ? patch.assigneeId : ticket.assigneeId,
    dueDate: "dueDate" in patch ? patch.dueDate : ticket.dueDate,
    estimatedHours: "estimatedHours" in patch ? patch.estimatedHours : ticket.estimatedHours,
    taskTypeId: "taskTypeId" in patch ? patch.taskTypeId : ticket.taskTypeId,
    parentTicketId: "parentTicketId" in patch ? patch.parentTicketId : ticket.parentTicketId,
    externalBudget: "externalBudget" in patch ? patch.externalBudget : ticket.externalBudget,
    externalUser: "externalUser" in patch ? patch.externalUser : ticket.externalUser,
    externalId: "externalId" in patch ? patch.externalId : ticket.externalId,
    externalUrl: "externalUrl" in patch ? patch.externalUrl : ticket.externalUrl,
    externalProject: "externalProject" in patch ? patch.externalProject : ticket.externalProject,
    implementationNotes:
      "implementationNotes" in patch ? patch.implementationNotes : ticket.implementationNotes,
  };
}

function EditableTitle({ ticket, canEdit }: { ticket: Ticket; canEdit: boolean }) {
  const t = useTranslations("TicketDetail");
  const updateTicket = useUpdateTicket();
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState(ticket.title);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isEditing) inputRef.current?.focus();
  }, [isEditing]);

  const startEdit = () => {
    setDraft(ticket.title);
    setIsEditing(true);
  };

  const cancel = () => {
    setDraft(ticket.title);
    setIsEditing(false);
  };

  const save = async () => {
    const trimmed = draft.trim();
    if (!trimmed) {
      toast.error(t("titleRequired"));
      return;
    }
    if (trimmed === ticket.title) {
      setIsEditing(false);
      return;
    }
    try {
      await updateTicket.mutateAsync(buildUpdatePayload(ticket, { title: trimmed }));
      toast.success(t("titleUpdated"));
      setIsEditing(false);
    } catch {
      toast.error(t("updateFailed"));
    }
  };

  if (isEditing) {
    return (
      <div className="space-y-2">
        <div className="flex items-start gap-2">
          <span className="font-mono text-muted-foreground text-2xl font-bold mt-0.5">
            {ticket.key}
          </span>
          <input
            ref={inputRef}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void save();
              if (e.key === "Escape") cancel();
            }}
            className="flex-1 rounded-lg border border-input bg-background px-3 py-1.5 text-2xl font-bold text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
        <div className="flex items-center justify-end gap-2">
          <button
            type="button"
            onClick={cancel}
            disabled={updateTicket.isPending}
            className="inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
          >
            <X className="h-3.5 w-3.5" />
            {t("cancel")}
          </button>
          <button
            type="button"
            onClick={save}
            disabled={updateTicket.isPending}
            className="inline-flex items-center gap-1 px-2.5 py-1 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
          >
            <Send className="h-3.5 w-3.5" />
            {t("save")}
          </button>
        </div>
      </div>
    );
  }

  return (
    <h1 className="text-2xl font-bold text-foreground group flex items-start gap-2">
      <span className="font-mono text-muted-foreground mr-2">{ticket.key}</span>
      <span className="flex-1">{ticket.title}</span>
      {canEdit && (
        <button
          type="button"
          onClick={startEdit}
          className="opacity-0 group-hover:opacity-100 transition-opacity p-1 text-muted-foreground hover:text-foreground rounded"
          title={t("editTitleAriaLabel")}
          aria-label={t("editTitleAriaLabel")}
        >
          <Pencil className="h-4 w-4" />
        </button>
      )}
    </h1>
  );
}

function EditableSidebarSelect({
  label,
  displayValue,
  options,
  onSave,
  canEdit,
  placeholder,
  ariaLabel,
}: {
  label: string;
  displayValue: React.ReactNode;
  options: { id: string; label: string }[];
  onSave: (id: string | null) => Promise<void>;
  canEdit: boolean;
  placeholder?: string;
  ariaLabel: string;
}) {
  const t = useTranslations("TicketDetail");
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState<string>("");
  const [saving, setSaving] = useState(false);

  const startEdit = (initial: string) => {
    setDraft(initial);
    setIsEditing(true);
  };

  const save = async () => {
    setSaving(true);
    try {
      await onSave(draft || null);
      setIsEditing(false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="group">
      <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
        {label}
      </label>
      {isEditing ? (
        <div className="mt-1 space-y-2">
          <select
            autoFocus
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            className="w-full rounded-lg border border-input bg-background px-2 py-1 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {placeholder !== undefined && <option value="">{placeholder}</option>}
            {options.map((o) => (
              <option key={o.id} value={o.id}>
                {o.label}
              </option>
            ))}
          </select>
          <div className="flex items-center justify-end gap-1">
            <button
              type="button"
              onClick={() => setIsEditing(false)}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
            >
              <X className="h-3 w-3" />
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={save}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
            >
              <Send className="h-3 w-3" />
              {t("save")}
            </button>
          </div>
        </div>
      ) : (
        <div className="mt-1 flex items-center justify-between gap-2">
          <div className="flex-1 min-w-0">{displayValue}</div>
          {canEdit && (
            <button
              type="button"
              onClick={() => startEdit(options.length > 0 ? (options[0]?.id ?? "") : "")}
              className="opacity-0 group-hover:opacity-100 transition-opacity p-1 text-muted-foreground hover:text-foreground rounded"
              title={ariaLabel}
              aria-label={ariaLabel}
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

function EditableSidebarText({
  label,
  displayValue,
  initialValue,
  inputType,
  step,
  min,
  onSave,
  canEdit,
  ariaLabel,
  placeholder,
}: {
  label: string;
  displayValue: React.ReactNode;
  initialValue: string;
  inputType: "date" | "number" | "text" | "url";
  step?: string;
  min?: string;
  onSave: (value: string) => Promise<void>;
  canEdit: boolean;
  ariaLabel: string;
  placeholder?: string;
}) {
  const t = useTranslations("TicketDetail");
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState(initialValue);
  const [saving, setSaving] = useState(false);

  const startEdit = () => {
    setDraft(initialValue);
    setIsEditing(true);
  };

  const save = async () => {
    setSaving(true);
    try {
      await onSave(draft);
      setIsEditing(false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="group">
      <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
        {label}
      </label>
      {isEditing ? (
        <div className="mt-1 space-y-2">
          <input
            autoFocus
            type={inputType}
            step={step}
            min={min}
            placeholder={placeholder}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void save();
              if (e.key === "Escape") setIsEditing(false);
            }}
            className="w-full rounded-lg border border-input bg-background px-2 py-1 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
          <div className="flex items-center justify-end gap-1">
            <button
              type="button"
              onClick={() => setIsEditing(false)}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
            >
              <X className="h-3 w-3" />
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={save}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
            >
              <Send className="h-3 w-3" />
              {t("save")}
            </button>
          </div>
        </div>
      ) : (
        <div className="mt-1 flex items-center justify-between gap-2">
          <div className="flex-1 min-w-0">{displayValue}</div>
          {canEdit && (
            <button
              type="button"
              onClick={startEdit}
              className="opacity-0 group-hover:opacity-100 transition-opacity p-1 text-muted-foreground hover:text-foreground rounded"
              title={ariaLabel}
              aria-label={ariaLabel}
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

function EditableTextSection({
  icon,
  title,
  value,
  emptyLabel,
  placeholder,
  onSave,
  canEdit,
  ariaLabel,
}: {
  icon: React.ReactNode;
  title: string;
  value: string | null | undefined;
  emptyLabel: string;
  placeholder: string;
  onSave: (value: string | null) => Promise<void>;
  canEdit: boolean;
  ariaLabel: string;
}) {
  const t = useTranslations("TicketDetail");
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState(value ?? "");
  const [saving, setSaving] = useState(false);
  const hasValue = !!value && value.trim().length > 0;

  const startEdit = () => {
    setDraft(value ?? "");
    setIsEditing(true);
  };

  const save = async () => {
    const trimmed = draft.trim();
    setSaving(true);
    try {
      await onSave(trimmed.length === 0 ? null : trimmed);
      setIsEditing(false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-2 group">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
          {icon}
          {title}
        </h3>
        {canEdit && !isEditing && (
          <button
            type="button"
            onClick={startEdit}
            className="opacity-0 group-hover:opacity-100 transition-opacity p-1 text-muted-foreground hover:text-foreground rounded"
            title={ariaLabel}
            aria-label={ariaLabel}
          >
            <Pencil className="h-3.5 w-3.5" />
          </button>
        )}
      </div>
      {isEditing ? (
        <div className="space-y-2">
          <textarea
            autoFocus
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            rows={4}
            placeholder={placeholder}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-y"
          />
          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={() => setIsEditing(false)}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
            >
              <X className="h-3.5 w-3.5" />
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={save}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
            >
              <Send className="h-3.5 w-3.5" />
              {t("save")}
            </button>
          </div>
        </div>
      ) : hasValue ? (
        <div className="rounded-lg border border-border p-4 bg-muted/30">
          <p className="whitespace-pre-wrap text-sm text-foreground">{value}</p>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground italic">{emptyLabel}</p>
      )}
    </div>
  );
}

function EditableParentField({
  ticket,
  projectCode,
  canEdit,
  onSave,
}: {
  ticket: Ticket;
  projectCode: string;
  canEdit: boolean;
  onSave: (parentId: string | null) => Promise<void>;
}) {
  const t = useTranslations("TicketDetail");
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState("");
  const [saving, setSaving] = useState(false);

  const startEdit = () => {
    setDraft(ticket.parentTicketKey ?? "");
    setIsEditing(true);
  };

  const save = async () => {
    const trimmed = draft.trim();
    setSaving(true);
    try {
      if (!trimmed) {
        if (ticket.parentTicketId == null) {
          setIsEditing(false);
          return;
        }
        await onSave(null);
        setIsEditing(false);
        return;
      }

      // Expecting "CODE-NUMBER" format. Same project only (BE enforces this).
      const match = trimmed.match(/-(\d+)$/);
      if (!match) {
        toast.error(t("parentKeyInvalid"));
        return;
      }
      const number = parseInt(match[1], 10);
      if (!Number.isFinite(number) || number <= 0) {
        toast.error(t("parentKeyInvalid"));
        return;
      }
      try {
        const { data } = await apiClient.get<{ id: string }>(
          `/api/v1/projects/${ticket.projectId}/tickets/by-number/${number}`
        );
        if (data.id === ticket.id) {
          toast.error(t("parentIsSelf"));
          return;
        }
        if (data.id === ticket.parentTicketId) {
          setIsEditing(false);
          return;
        }
        await onSave(data.id);
        setIsEditing(false);
      } catch {
        toast.error(t("parentNotFound"));
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="group">
      <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
        {t("parentTicket")}
      </label>
      {isEditing ? (
        <div className="mt-1 space-y-2">
          <input
            autoFocus
            type="text"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void save();
              if (e.key === "Escape") setIsEditing(false);
            }}
            placeholder={t("parentTicketPlaceholder")}
            className="w-full rounded-lg border border-input bg-background px-2 py-1 text-sm text-foreground font-mono focus:outline-none focus:ring-2 focus:ring-ring"
          />
          <div className="flex items-center justify-end gap-1">
            <button
              type="button"
              onClick={() => setIsEditing(false)}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs text-muted-foreground hover:text-foreground disabled:opacity-50"
            >
              <X className="h-3 w-3" />
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={save}
              disabled={saving}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
            >
              <Send className="h-3 w-3" />
              {t("save")}
            </button>
          </div>
        </div>
      ) : (
        <div className="mt-1 flex items-center justify-between gap-2">
          <div className="flex-1 min-w-0">
            {ticket.parentTicketKey ? (
              <Link
                href={`/projects/${projectCode}/tickets/${ticket.parentTicketKey}`}
                className="inline-flex items-center gap-1 text-sm text-foreground hover:text-primary"
              >
                <span className="font-mono text-muted-foreground">{ticket.parentTicketKey}</span>
                <span className="truncate">{ticket.parentTicketTitle}</span>
              </Link>
            ) : (
              <span className="text-sm text-muted-foreground">{t("noParentTicket")}</span>
            )}
          </div>
          {canEdit && (
            <button
              type="button"
              onClick={startEdit}
              className="opacity-0 group-hover:opacity-100 transition-opacity p-1 text-muted-foreground hover:text-foreground rounded"
              title={t("editParentTicketAriaLabel")}
              aria-label={t("editParentTicketAriaLabel")}
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

function SubTicketsSection({
  subTickets,
  projectCode,
}: {
  subTickets: TicketSubTicket[] | undefined;
  projectCode: string;
}) {
  const t = useTranslations("TicketDetail");
  if (!subTickets || subTickets.length === 0) return null;

  return (
    <div className="space-y-3">
      <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
        <FileText className="h-4 w-4 text-muted-foreground" />
        {t("subTickets")}
        <span className="text-xs text-muted-foreground font-normal">({subTickets.length})</span>
      </h3>
      <ul className="space-y-2">
        {subTickets.map((sub) => (
          <li key={sub.id}>
            <Link
              href={`/projects/${projectCode}/tickets/${sub.key}`}
              className="flex items-center gap-3 p-2 rounded border border-border hover:bg-muted/30"
            >
              <span className="font-mono text-xs text-muted-foreground shrink-0">{sub.key}</span>
              <span className="text-sm text-foreground flex-1 min-w-0 truncate">{sub.title}</span>
              <StatusBadge name={sub.taskStateName} color={sub.taskStateColor} />
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}

function localizedLookupName(
  locale: string,
  name: string,
  nameCs?: string | null,
  nameEn?: string | null
): string {
  if (locale.startsWith("cs")) return nameCs?.trim() || name;
  return nameEn?.trim() || name;
}

function userPickerLabel(u: UserOption) {
  return u.displayName?.trim() ? u.displayName : u.email;
}

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
  const locale = useLocale();
  const { code, ticketKey } = use(params);
  const ticketNumber = parseInt(ticketKey.split("-").pop() || "0", 10);
  const { data: project } = useProjectByCode(code);
  const projectId = project?.id ?? "";
  const { data: ticket, isLoading, error } = useTicketByNumber(projectId, ticketNumber);
  const { data: currentUser } = useCurrentUser();
  const { data: taskStates } = useTaskStates(project?.projectTemplateId);
  const { data: priorities } = useTicketPriorities(project?.projectTemplateId);
  const { data: projectUsers } = useProjectUsers();
  const updateTicket = useUpdateTicket();

  const canEditTicket =
    !!currentUser &&
    !!project &&
    (currentUser.globalRole === GlobalRole.Admin ||
      currentUser.projectRoles.some(
        (pr) => pr.projectId === project.id && pr.role !== ProjectRole.Guest
      ));

  const saveTicketPatch = async (patch: TicketPatch, successKey: string) => {
    if (!ticket) return;
    try {
      await updateTicket.mutateAsync(buildUpdatePayload(ticket, patch));
      toast.success(t(successKey));
    } catch {
      toast.error(t("updateFailed"));
    }
  };

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
            <EditableTitle ticket={ticket} canEdit={canEditTicket} />
          </div>

          {ticket.description && (
            <div className="prose prose-sm max-w-none text-foreground">
              <h3 className="text-sm font-semibold text-foreground mb-2">{t("description")}</h3>
              <div className="rounded-lg border border-border p-4 bg-muted/30">
                <p className="whitespace-pre-wrap text-sm">{ticket.description}</p>
              </div>
            </div>
          )}

          <EditableTextSection
            icon={<FileText className="h-4 w-4 text-muted-foreground" />}
            title={t("implementationNotes")}
            value={ticket.implementationNotes}
            emptyLabel={t("noImplementationNotes")}
            placeholder={t("implementationNotesPlaceholder")}
            canEdit={canEditTicket}
            ariaLabel={t("editImplementationNotesAriaLabel")}
            onSave={async (next) => {
              const current = ticket.implementationNotes ?? null;
              if (next === current) return;
              await saveTicketPatch(
                { implementationNotes: next ?? undefined },
                "implementationNotesUpdated"
              );
            }}
          />

          <SubTicketsSection subTickets={ticket.subTickets} projectCode={code} />

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
            <EditableSidebarSelect
              label={t("status")}
              displayValue={
                <StatusBadge name={ticket.taskStateName} color={ticket.taskStateColor} />
              }
              options={
                taskStates
                  ?.filter((s) => s.isActive)
                  .sort((a, b) => a.sortOrder - b.sortOrder)
                  .map((s) => ({
                    id: s.id,
                    label: localizedLookupName(locale, s.name, s.nameCs, s.nameEn),
                  })) ?? []
              }
              canEdit={canEditTicket}
              ariaLabel={t("editStatusAriaLabel")}
              onSave={async (id) => {
                if (!id || id === ticket.taskStateId) return;
                await saveTicketPatch({ taskStateId: id }, "statusUpdated");
              }}
            />

            <EditableSidebarSelect
              label={t("priority")}
              displayValue={
                <PriorityBadge
                  name={ticket.ticketPriorityName}
                  color={ticket.ticketPriorityColor}
                />
              }
              options={
                priorities
                  ?.filter((p) => p.isActive)
                  .sort((a, b) => a.sortOrder - b.sortOrder)
                  .map((p) => ({
                    id: p.id,
                    label: localizedLookupName(locale, p.name, p.nameCs, p.nameEn),
                  })) ?? []
              }
              canEdit={canEditTicket}
              ariaLabel={t("editPriorityAriaLabel")}
              onSave={async (id) => {
                if (!id || id === ticket.ticketPriorityId) return;
                await saveTicketPatch({ ticketPriorityId: id }, "priorityUpdated");
              }}
            />

            <EditableSidebarSelect
              label={t("assignee")}
              displayValue={
                <div className="flex items-center gap-2">
                  <User className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground">
                    {ticket.assignee?.displayName || t("unassigned")}
                  </span>
                </div>
              }
              options={projectUsers?.map((u) => ({ id: u.id, label: userPickerLabel(u) })) ?? []}
              placeholder={t("unassigned")}
              canEdit={canEditTicket}
              ariaLabel={t("editAssigneeAriaLabel")}
              onSave={async (id) => {
                const newAssignee = id ?? null;
                if (newAssignee === (ticket.assigneeId ?? null)) return;
                await saveTicketPatch({ assigneeId: newAssignee ?? undefined }, "assigneeUpdated");
              }}
            />

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

            <EditableParentField
              ticket={ticket}
              projectCode={code}
              canEdit={canEditTicket}
              onSave={async (parentId) => {
                if ((parentId ?? null) === (ticket.parentTicketId ?? null)) return;
                await saveTicketPatch(
                  { parentTicketId: parentId ?? undefined },
                  "parentTicketUpdated"
                );
              }}
            />

            <EditableSidebarText
              label={t("dueDate")}
              displayValue={
                <div className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground">
                    {ticket.dueDate
                      ? format(new Date(ticket.dueDate), "MMM d, yyyy")
                      : t("noDueDate")}
                  </span>
                </div>
              }
              initialValue={ticket.dueDate ?? ""}
              inputType="date"
              canEdit={canEditTicket}
              ariaLabel={t("editDueDateAriaLabel")}
              onSave={async (value) => {
                const next = value ? value : null;
                const current = ticket.dueDate ?? null;
                if (next === current) return;
                await saveTicketPatch({ dueDate: next ?? undefined }, "dueDateUpdated");
              }}
            />

            <EditableSidebarText
              label={t("estimatedHours")}
              displayValue={
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground">
                    {ticket.estimatedHours ? `${ticket.estimatedHours}h` : t("noEstimate")}
                  </span>
                </div>
              }
              initialValue={ticket.estimatedHours?.toString() ?? ""}
              inputType="number"
              step="0.5"
              min="0"
              canEdit={canEditTicket}
              ariaLabel={t("editEstimatedHoursAriaLabel")}
              onSave={async (value) => {
                const trimmed = value.trim();
                if (!trimmed) {
                  if (ticket.estimatedHours === undefined || ticket.estimatedHours === null) return;
                  await saveTicketPatch({ estimatedHours: undefined }, "estimatedHoursUpdated");
                  return;
                }
                const parsed = Number(trimmed);
                if (!Number.isFinite(parsed) || parsed <= 0) {
                  toast.error(t("estimatedHoursInvalid"));
                  return;
                }
                if (parsed === ticket.estimatedHours) return;
                await saveTicketPatch({ estimatedHours: parsed }, "estimatedHoursUpdated");
              }}
            />

            <EditableSidebarText
              label={t("externalBudgetLabel")}
              displayValue={
                <span className="text-sm text-foreground">
                  {ticket.externalBudget != null && ticket.externalBudget !== 0
                    ? ticket.externalBudget.toString()
                    : t("noExternalBudget")}
                </span>
              }
              initialValue={ticket.externalBudget?.toString() ?? ""}
              inputType="number"
              step="0.5"
              min="0"
              canEdit={canEditTicket}
              ariaLabel={t("editExternalBudgetAriaLabel")}
              onSave={async (value) => {
                const trimmed = value.trim();
                if (!trimmed) {
                  if (ticket.externalBudget == null) return;
                  await saveTicketPatch({ externalBudget: undefined }, "externalBudgetUpdated");
                  return;
                }
                const parsed = Number(trimmed);
                if (!Number.isFinite(parsed) || parsed < 0) {
                  toast.error(t("externalBudgetInvalid"));
                  return;
                }
                if (parsed === ticket.externalBudget) return;
                await saveTicketPatch({ externalBudget: parsed }, "externalBudgetUpdated");
              }}
            />

            <EditableSidebarText
              label={t("externalUserLabel")}
              displayValue={
                <span className="text-sm text-foreground">
                  {ticket.externalUser?.trim() ? ticket.externalUser : t("noExternalUser")}
                </span>
              }
              initialValue={ticket.externalUser ?? ""}
              inputType="text"
              placeholder={t("externalUserPlaceholder")}
              canEdit={canEditTicket}
              ariaLabel={t("editExternalUserAriaLabel")}
              onSave={async (value) => {
                const trimmed = value.trim();
                const next = trimmed.length === 0 ? null : trimmed;
                const current = ticket.externalUser?.trim() ? ticket.externalUser : null;
                if (next === current) return;
                await saveTicketPatch({ externalUser: next ?? undefined }, "externalUserUpdated");
              }}
            />

            <EditableSidebarText
              label={t("externalIdLabel")}
              displayValue={
                <span className="text-sm text-foreground font-mono">
                  {ticket.externalId?.trim() ? ticket.externalId : t("noExternalId")}
                </span>
              }
              initialValue={ticket.externalId ?? ""}
              inputType="text"
              placeholder={t("externalIdPlaceholder")}
              canEdit={canEditTicket}
              ariaLabel={t("editExternalIdAriaLabel")}
              onSave={async (value) => {
                const trimmed = value.trim();
                const next = trimmed.length === 0 ? null : trimmed;
                const current = ticket.externalId?.trim() ? ticket.externalId : null;
                if (next === current) return;
                await saveTicketPatch({ externalId: next ?? undefined }, "externalIdUpdated");
              }}
            />

            <EditableSidebarText
              label={t("externalUrlLabel")}
              displayValue={
                ticket.externalUrl?.trim() ? (
                  <a
                    href={ticket.externalUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm text-accent-orange hover:underline inline-flex items-center gap-1 break-all"
                  >
                    <ExternalLink className="h-3 w-3 shrink-0" />
                    {ticket.externalUrl}
                  </a>
                ) : (
                  <span className="text-sm text-foreground">{t("noExternalUrl")}</span>
                )
              }
              initialValue={ticket.externalUrl ?? ""}
              inputType="url"
              placeholder={t("externalUrlPlaceholder")}
              canEdit={canEditTicket}
              ariaLabel={t("editExternalUrlAriaLabel")}
              onSave={async (value) => {
                const trimmed = value.trim();
                const next = trimmed.length === 0 ? null : trimmed;
                const current = ticket.externalUrl?.trim() ? ticket.externalUrl : null;
                if (next === current) return;
                await saveTicketPatch({ externalUrl: next ?? undefined }, "externalUrlUpdated");
              }}
            />

            <EditableSidebarText
              label={t("externalProjectLabel")}
              displayValue={
                <span className="text-sm text-foreground">
                  {ticket.externalProject?.trim() ? ticket.externalProject : t("noExternalProject")}
                </span>
              }
              initialValue={ticket.externalProject ?? ""}
              inputType="text"
              placeholder={t("externalProjectPlaceholder")}
              canEdit={canEditTicket}
              ariaLabel={t("editExternalProjectAriaLabel")}
              onSave={async (value) => {
                const trimmed = value.trim();
                const next = trimmed.length === 0 ? null : trimmed;
                const current = ticket.externalProject?.trim() ? ticket.externalProject : null;
                if (next === current) return;
                await saveTicketPatch(
                  { externalProject: next ?? undefined },
                  "externalProjectUpdated"
                );
              }}
            />

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
