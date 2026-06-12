"use client";

import { useState, useEffect } from "react";
import { useWorklogs, useCreateWorklog, useDeleteWorklog } from "@/queries/worklogs";
import { useProjects } from "@/queries/projects";
import { useTickets } from "@/queries/tickets";
import { useAdminUsers } from "@/queries/admin";
import { useCurrentUser } from "@/queries/auth";
import { useTimerStore } from "@/stores/timer-store";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { EditWorklogDialog } from "@/components/shared/edit-worklog-dialog";
import { Clock, Plus, X, Play, Square, Timer, Pencil, Trash2 } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createWorklogSchema, type CreateWorklogInput } from "@/schemas/worklog";
import { toast } from "sonner";
import { format, startOfWeek, endOfWeek } from "date-fns";
import { useTranslations } from "next-intl";
import { GlobalRole, ProjectRole, type Worklog, type Project } from "@/types";
import { formatElapsedTime } from "@/lib/utils";

function TimerDisplay() {
  const t = useTranslations("WorklogsPage");
  const { isRunning, elapsed, tick, start, stop, reset, description } = useTimerStore();
  const { data: projects } = useProjects();
  const createWorklog = useCreateWorklog();
  const [timerProjectId, setTimerProjectId] = useState("");
  const [timerTicketId, setTimerTicketId] = useState("");
  const [timerDescription, setTimerDescription] = useState("");
  const { data: timerTicketsPage } = useTickets(timerProjectId || "", { pageSize: 200 });
  const timerTickets = timerTicketsPage?.items ?? [];

  useEffect(() => {
    if (!isRunning) return;
    const interval = setInterval(() => tick(), 1000);
    return () => clearInterval(interval);
  }, [isRunning, tick]);

  const handleStart = () => {
    if (!timerProjectId) {
      toast.error(t("selectProjectFirst"));
      return;
    }
    if (!timerTicketId) {
      toast.error(t("selectTicketFirst"));
      return;
    }
    start(timerProjectId, timerTicketId, timerDescription);
  };

  const handleStop = async () => {
    const result = stop();
    if (result.elapsed === 0 || !result.projectId || !result.ticketId) {
      reset();
      return;
    }
    const trimmed = (result.description ?? "").trim();
    const finalDescription =
      trimmed.length >= 16 ? trimmed : `Timer session: ${trimmed || t("untitled")}`.padEnd(16, " ");
    try {
      await createWorklog.mutateAsync({
        projectId: result.projectId,
        ticketId: result.ticketId,
        date: format(new Date(), "yyyy-MM-dd"),
        hours: parseFloat((result.elapsed / 3600).toFixed(2)),
        description: finalDescription,
        isBillable: true,
      });
      toast.success(t("timerSavedFromTimer"));
      reset();
    } catch {
      toast.error(t("timerSaveFailed"));
    }
  };

  return (
    <div className="rounded-lg border border-border bg-card p-5">
      <div className="flex items-center gap-2 mb-4">
        <Timer className="h-5 w-5 text-accent-orange" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("timer")}</h2>
      </div>

      {!isRunning ? (
        <div className="space-y-3">
          <select
            value={timerProjectId}
            onChange={(e) => {
              setTimerProjectId(e.target.value);
              setTimerTicketId("");
            }}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">{t("selectProject")}</option>
            {projects?.map((p: Project) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
          <select
            value={timerTicketId}
            onChange={(e) => setTimerTicketId(e.target.value)}
            disabled={!timerProjectId}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-50"
          >
            <option value="">{t("selectTicket")}</option>
            {timerTickets.map((tk) => (
              <option key={tk.id} value={tk.id}>
                #{tk.number} — {tk.title}
              </option>
            ))}
          </select>
          <input
            type="text"
            placeholder={t("whatWorking")}
            value={timerDescription}
            onChange={(e) => setTimerDescription(e.target.value)}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
          <button
            onClick={handleStart}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-green-600 text-white text-sm font-medium hover:bg-green-700 transition-colors"
          >
            <Play className="h-4 w-4" />
            {t("startTimer")}
          </button>
        </div>
      ) : (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">{description || t("timerRunning")}</p>
          <p className="text-3xl font-mono font-bold text-accent-orange">
            {formatElapsedTime(elapsed)}
          </p>
          <button
            onClick={handleStop}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-red-600 text-white text-sm font-medium hover:bg-red-700 transition-colors"
          >
            <Square className="h-4 w-4" />
            {t("stopAndSave")}
          </button>
        </div>
      )}
    </div>
  );
}

function QuickLogDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useTranslations("WorklogsPage");
  const tCommon = useTranslations("Common");
  const { data: projects } = useProjects();
  const createWorklog = useCreateWorklog();
  const { data: currentUser } = useCurrentUser();
  const isAdmin = currentUser?.globalRole === GlobalRole.Admin;
  const { data: adminUsers } = useAdminUsers();
  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateWorklogInput>({
    resolver: zodResolver(createWorklogSchema),
    defaultValues: {
      date: format(new Date(), "yyyy-MM-dd"),
      isBillable: true,
      description: "",
    },
  });
  const description = watch("description") ?? "";
  const selectedProjectId = watch("projectId");
  const { data: ticketsPage } = useTickets(selectedProjectId || "", { pageSize: 200 });
  const tickets = ticketsPage?.items ?? [];

  const onSubmit = async (data: CreateWorklogInput) => {
    try {
      await createWorklog.mutateAsync({
        ...data,
        overrideUserId: data.overrideUserId || undefined,
      });
      toast.success(t("added"));
      reset();
      onClose();
    } catch {
      toast.error(t("addFailed"));
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-card-foreground">{t("quickLogTitle")}</h2>
          <button onClick={onClose} className="p-1 rounded hover:bg-muted transition-colors">
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {tCommon("project")}
            </label>
            <select
              {...register("projectId")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">{t("selectProject")}</option>
              {projects?.map((p: Project) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
            {errors.projectId && (
              <p className="text-xs text-destructive mt-1">{errors.projectId.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("columns.ticket")} <span className="text-destructive">*</span>
            </label>
            <select
              {...register("ticketId")}
              disabled={!selectedProjectId}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-50"
            >
              <option value="">{t("selectTicketEllipsis")}</option>
              {tickets.map((tk) => (
                <option key={tk.id} value={tk.id}>
                  #{tk.number} — {tk.title}
                </option>
              ))}
            </select>
            {errors.ticketId && (
              <p className="text-xs text-destructive mt-1">{errors.ticketId.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                {t("columns.date")}
              </label>
              <input
                {...register("date")}
                type="date"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                {t("columns.hours")}
              </label>
              <input
                {...register("hours", { valueAsNumber: true })}
                type="number"
                step="0.25"
                min="0.25"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
          </div>

          <div>
            <label className="flex items-center justify-between text-sm font-medium text-card-foreground mb-1">
              <span>
                {t("columns.description")} <span className="text-destructive">*</span>
              </span>
              <span className="text-xs font-normal text-muted-foreground">
                {t("descriptionCharCounter", { count: description.length })}
              </span>
            </label>
            <textarea
              {...register("description")}
              rows={2}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
              placeholder={t("descriptionPlaceholder")}
            />
            {errors.description && (
              <p className="text-xs text-destructive mt-1">{errors.description.message}</p>
            )}
          </div>

          {isAdmin && (
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                {t("logOnBehalf")}
              </label>
              <select
                {...register("overrideUserId")}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">{t("logAsMyself")}</option>
                {(adminUsers ?? [])
                  .filter((u) => u.isActive)
                  .map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.displayName} ({u.email})
                    </option>
                  ))}
              </select>
            </div>
          )}

          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register("isBillable")} className="rounded" />
            {t("billable")}
          </label>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
            >
              {tCommon("cancel")}
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {isSubmitting ? t("saving") : t("logTime")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function WorklogsPage() {
  const t = useTranslations("WorklogsPage");
  const now = new Date();
  const [from, setFrom] = useState(format(startOfWeek(now, { weekStartsOn: 1 }), "yyyy-MM-dd"));
  const [to, setTo] = useState(format(endOfWeek(now, { weekStartsOn: 1 }), "yyyy-MM-dd"));
  const { data: worklogs, isLoading, error } = useWorklogs({ from, to });
  const { data: currentUser } = useCurrentUser();
  const deleteWorklog = useDeleteWorklog();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingWorklog, setEditingWorklog] = useState<Worklog | null>(null);

  const isProjectManager = (projectId: string) =>
    !!currentUser &&
    currentUser.projectRoles.some(
      (pr) => pr.projectId === projectId && pr.role === ProjectRole.ProjectManager
    );

  const canEdit = (worklog: Worklog) =>
    !!currentUser &&
    currentUser.permissions.timeTrackingUpdate &&
    (worklog.userId === currentUser.id ||
      currentUser.globalRole === GlobalRole.Admin ||
      isProjectManager(worklog.projectId));

  const canDelete = (worklog: Worklog) =>
    !!currentUser &&
    currentUser.permissions.timeTrackingDelete &&
    (worklog.userId === currentUser.id ||
      currentUser.globalRole === GlobalRole.Admin ||
      isProjectManager(worklog.projectId));

  const handleDelete = async (worklog: Worklog) => {
    if (!window.confirm(t("deleteConfirm"))) return;
    try {
      await deleteWorklog.mutateAsync({ projectId: worklog.projectId, worklogId: worklog.id });
      toast.success(t("deleted"));
    } catch {
      toast.error(t("deleteFailed"));
    }
  };

  const totalHours = worklogs?.reduce((sum, w) => sum + w.hours, 0).toFixed(2) || "0.00";

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
          <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
        </div>
        <button
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
        >
          <Plus className="h-4 w-4" />
          {t("quickLog")}
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1">
          <TimerDisplay />
        </div>

        <div className="lg:col-span-2">
          <div className="rounded-lg border border-border bg-card p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-card-foreground">{t("timeEntries")}</h2>
              <p className="text-lg font-bold text-accent-orange">
                {t("totalHours", { hours: totalHours })}
              </p>
            </div>

            <div className="flex gap-3 mb-4">
              <div>
                <label className="block text-xs text-muted-foreground mb-1">{t("from")}</label>
                <input
                  type="date"
                  value={from}
                  onChange={(e) => setFrom(e.target.value)}
                  className="rounded-lg border border-input bg-background px-3 py-1.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-1">{t("to")}</label>
                <input
                  type="date"
                  value={to}
                  onChange={(e) => setTo(e.target.value)}
                  className="rounded-lg border border-input bg-background px-3 py-1.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
            </div>

            {isLoading && <TableSkeleton rows={5} />}

            {error && (
              <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
                {t("loadFailed")}
              </div>
            )}

            {worklogs && worklogs.length === 0 && (
              <EmptyState
                icon={<Clock className="h-10 w-10" />}
                title={t("noWorklogsInRange")}
                description={t("tryDifferentRange")}
              />
            )}

            {worklogs && worklogs.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border">
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.date")}
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.ticket")}
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.user")}
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.hours")}
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.description")}
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.source")}
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        {t("columns.aiSummary")}
                      </th>
                      <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase w-20">
                        {t("columns.actions")}
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {worklogs.map((worklog: Worklog) => (
                      <tr key={worklog.id} className="hover:bg-muted/30">
                        <td className="px-3 py-2 text-sm text-foreground">
                          {format(new Date(worklog.date), "MMM d")}
                        </td>
                        <td className="px-3 py-2 text-sm text-muted-foreground truncate max-w-[14rem]">
                          {worklog.ticketTitle ? `${worklog.ticketTitle}` : "-"}
                        </td>
                        <td className="px-3 py-2 text-sm text-foreground">
                          {worklog.user.displayName}
                        </td>
                        <td className="px-3 py-2 text-sm font-medium text-foreground">
                          {worklog.hours.toFixed(2)}h
                        </td>
                        <td className="px-3 py-2 text-sm text-muted-foreground truncate max-w-xs">
                          {worklog.description || "-"}
                        </td>
                        <td className="px-3 py-2 text-sm text-muted-foreground">
                          {worklog.source}
                        </td>
                        <td
                          className="px-3 py-2 text-sm text-muted-foreground truncate max-w-xs"
                          title={worklog.aiSummary || undefined}
                        >
                          {worklog.aiSummary || "-"}
                        </td>
                        <td className="px-3 py-2 text-sm">
                          <div className="flex items-center justify-end gap-1">
                            {canEdit(worklog) && (
                              <button
                                onClick={() => setEditingWorklog(worklog)}
                                className="p-1 text-muted-foreground hover:text-foreground rounded"
                                title={t("editAriaLabel")}
                                aria-label={t("editAriaLabel")}
                              >
                                <Pencil className="h-3.5 w-3.5" />
                              </button>
                            )}
                            {canDelete(worklog) && (
                              <button
                                onClick={() => handleDelete(worklog)}
                                disabled={deleteWorklog.isPending}
                                className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                                title={t("deleteAriaLabel")}
                                aria-label={t("deleteAriaLabel")}
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      <QuickLogDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
      <EditWorklogDialog
        worklog={editingWorklog}
        open={!!editingWorklog}
        onClose={() => setEditingWorklog(null)}
      />
    </div>
  );
}
