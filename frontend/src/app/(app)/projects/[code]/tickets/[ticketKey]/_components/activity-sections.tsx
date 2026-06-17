"use client";

import { useState } from "react";
import { Skeleton } from "@/components/shared/loading-skeleton";
import {
  Sparkles,
  CheckSquare,
  Square,
  X,
  GitBranch,
  GitPullRequest,
  GitMerge,
  ExternalLink,
} from "lucide-react";
import { useLinkedPullRequests, useCreateTicketBranch, useLinkedCommits } from "@/queries/github";
import { useTicketAiHistory, useResummarizeTicket, type AiInvocation } from "@/queries/ai";
import { cn } from "@/lib/utils";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import type { ChecklistItem } from "@/types";
import { format } from "date-fns";
import { checksBadgeClass } from "./shared";

export function LinkedPullRequestsSection({
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
  const { data: commits } = useLinkedCommits(projectId, ticketId);
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
                <div className="text-xs text-muted-foreground flex flex-wrap items-center gap-x-1">
                  <span className="font-mono">{pr.branch}</span>
                  {pr.authorLogin && <span>· by @{pr.authorLogin}</span>}
                  <span>· {pr.state.toLowerCase()}</span>
                  {pr.commitsCount > 0 && <span>· {pr.commitsCount} commits</span>}
                  {pr.mergedAt && <span>· merged {format(new Date(pr.mergedAt), "MMM d")}</span>}
                  {pr.checksStatus && (
                    <span
                      className={`ml-1 inline-flex items-center gap-1 rounded px-1.5 py-0.5 font-medium ${checksBadgeClass(
                        pr.checksStatus
                      )}`}
                      title={`CI: ${pr.checksStatus}`}
                    >
                      <span className="h-1.5 w-1.5 rounded-full bg-current" />
                      {pr.checksStatus}
                    </span>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}

      {commits && commits.length > 0 && (
        <div className="space-y-1.5">
          <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            {t("linkedCommits")}
          </h4>
          <ul className="space-y-1">
            {commits.map((c) => (
              <li key={c.id} className="flex items-start gap-2 text-xs">
                <a
                  href={c.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="font-mono text-muted-foreground hover:underline shrink-0"
                  title={c.sha}
                >
                  {c.sha.slice(0, 7)}
                </a>
                <span className="text-foreground truncate">{c.message.split("\n")[0]}</span>
                {c.authorLogin && (
                  <span className="text-muted-foreground shrink-0">@{c.authorLogin}</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

export function AiHistorySection({ projectId, ticketId }: { projectId: string; ticketId: string }) {
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
                      : "bg-muted text-muted-foreground"
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

export function ChecklistSection({ items }: { items: ChecklistItem[] }) {
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
