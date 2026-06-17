"use client";

import { useEffect, useRef, useState } from "react";
import apiClient from "@/lib/api/client";
import { useUpdateTicket } from "@/queries/tickets";
import { MarkdownContent } from "@/components/shared/markdown-content";
import { MarkdownEditor } from "@/components/shared/markdown-editor";
import { StatusBadge } from "@/components/shared/status-badge";
import { Pencil, X, Send, FileText } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import type { Ticket, TicketSubTicket } from "@/types";
import { buildUpdatePayload } from "./shared";

export function EditableTitle({ ticket, canEdit }: { ticket: Ticket; canEdit: boolean }) {
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

export function EditableSidebarSelect({
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
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                void save();
              }
              if (e.key === "Escape") setIsEditing(false);
            }}
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

export function EditableSidebarText({
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

export function EditableTextSection({
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
            onKeyDown={(e) => {
              if (e.key === "Enter" && (e.metaKey || e.ctrlKey)) {
                e.preventDefault();
                void save();
              }
              if (e.key === "Escape") setIsEditing(false);
            }}
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

export function EditableMarkdownSection({
  icon,
  title,
  value,
  emptyLabel,
  placeholder,
  onSave,
  canEdit,
  ariaLabel,
  projectId,
  ticketId,
}: {
  icon: React.ReactNode;
  title: string;
  value: string | null | undefined;
  emptyLabel: string;
  placeholder: string;
  onSave: (value: string | null) => Promise<void>;
  canEdit: boolean;
  ariaLabel: string;
  projectId: string;
  ticketId: string;
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
        <div
          className="space-y-2"
          onKeyDown={(e) => {
            if (e.key === "Enter" && (e.metaKey || e.ctrlKey)) {
              e.preventDefault();
              void save();
            }
            if (e.key === "Escape") setIsEditing(false);
          }}
        >
          <MarkdownEditor
            value={draft}
            onChange={setDraft}
            projectId={projectId}
            ticketId={ticketId}
            placeholder={placeholder}
            rows={8}
            autoFocus
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
          <MarkdownContent content={value!} />
        </div>
      ) : (
        <p className="text-sm text-muted-foreground italic">{emptyLabel}</p>
      )}
    </div>
  );
}

export function EditableParentField({
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

export function SubTicketsSection({
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
