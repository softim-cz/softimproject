"use client";

import { use } from "react";
import { useTicketByNumber, useUpdateTicket, useSetTicketWatch } from "@/queries/tickets";
import { useProjectByCode, useProjectUsers } from "@/queries/projects";
import { useTaskStates, useTicketPriorities } from "@/queries/lookups";
import { useCurrentUser } from "@/queries/auth";
import { GlobalRole, ProjectRole } from "@/types";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge } from "@/components/shared/status-badge";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { MarkdownContent } from "@/components/shared/markdown-content";
import {
  User,
  Calendar,
  Clock,
  Sparkles,
  ChevronLeft,
  FileText,
  ExternalLink,
  Eye,
  EyeOff,
} from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";
import { useLocale, useTranslations } from "next-intl";
import { format } from "date-fns";
import {
  buildUpdatePayload,
  localizedLookupName,
  userPickerLabel,
  type TicketPatch,
} from "./_components/shared";
import {
  EditableTitle,
  EditableSidebarSelect,
  EditableSidebarText,
  EditableTextSection,
  EditableMarkdownSection,
  EditableParentField,
  SubTicketsSection,
} from "./_components/editable-fields";
import { WorklogsSection } from "./_components/worklogs-section";
import { CommentsSection } from "./_components/comments-section";
import { AttachmentsSection } from "./_components/attachments-section";
import {
  LinkedPullRequestsSection,
  AiHistorySection,
  ChecklistSection,
} from "./_components/activity-sections";

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
  const setWatch = useSetTicketWatch();

  const canEditTicket =
    !!currentUser &&
    !!project &&
    (currentUser.globalRole === GlobalRole.Admin ||
      currentUser.projectRoles.some(
        (pr) => pr.projectId === project.id && pr.role !== ProjectRole.Guest
      ));

  const toggleWatch = async () => {
    if (!ticket || setWatch.isPending) return;
    const nextWatching = !ticket.isWatching;
    try {
      await setWatch.mutateAsync({ projectId, ticketId: ticket.id, watching: nextWatching });
      toast.success(nextWatching ? t("watchStarted") : t("watchStopped"));
    } catch {
      toast.error(t("watchUpdateFailed"));
    }
  };

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
    <div className="space-y-6">
      <Link
        href={`/projects/${code}/board`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ChevronLeft className="h-4 w-4" />
        {t("backToBoard")}
      </Link>

      <div className="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_22rem] gap-6">
        {/* Main content — fluid, fills the available page width */}
        <div className="space-y-6 min-w-0">
          <div>
            <EditableTitle ticket={ticket} canEdit={canEditTicket} />
          </div>

          <EditableMarkdownSection
            icon={<FileText className="h-4 w-4 text-muted-foreground" />}
            title={t("description")}
            value={ticket.description}
            emptyLabel={t("noDescription")}
            placeholder={t("descriptionPlaceholder")}
            canEdit={canEditTicket}
            ariaLabel={t("editDescriptionAriaLabel")}
            projectId={projectId}
            ticketId={ticket.id}
            onSave={async (next) => {
              const current = ticket.description ?? null;
              if (next === current) return;
              await saveTicketPatch({ description: next ?? undefined }, "descriptionUpdated");
            }}
          />

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
            <div className="rounded-lg border border-purple-200 bg-purple-50 p-4 dark:border-purple-500/30 dark:bg-purple-500/10">
              <div className="flex items-center gap-2 mb-2">
                <Sparkles className="h-4 w-4 text-purple-600 dark:text-purple-300" />
                <h3 className="text-sm font-semibold text-purple-900 dark:text-purple-200">
                  {t("aiSummaryTitle")}
                </h3>
              </div>
              <MarkdownContent content={ticket.aiSummary} />
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

          {/* Worklogs */}
          <WorklogsSection
            projectId={projectId}
            ticketId={ticket.id}
            estimatedHours={ticket.estimatedHours}
          />

          {/* Comments */}
          <CommentsSection projectId={projectId} ticketId={ticket.id} />
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <div className="rounded-lg border border-border bg-card p-4 space-y-4">
            <button
              type="button"
              onClick={toggleWatch}
              disabled={setWatch.isPending}
              aria-pressed={ticket.isWatching ?? false}
              title={ticket.isWatching ? t("watchStopHint") : t("watchStartHint")}
              className={`flex w-full items-center justify-center gap-2 rounded-md border px-3 py-2 text-sm font-medium transition-colors disabled:opacity-60 ${
                ticket.isWatching
                  ? "border-accent-orange/40 bg-accent-orange/10 text-accent-orange hover:bg-accent-orange/15"
                  : "border-border text-foreground hover:bg-muted"
              }`}
            >
              {ticket.isWatching ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
              {ticket.isWatching ? t("watching") : t("notWatching")}
            </button>

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

            <div>
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {t("workedHours")}
              </label>
              <div className="mt-1 flex items-center gap-2">
                <Clock className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm text-foreground">{`${ticket.cumulativeWorkedHours ?? 0}h`}</span>
              </div>
            </div>

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
