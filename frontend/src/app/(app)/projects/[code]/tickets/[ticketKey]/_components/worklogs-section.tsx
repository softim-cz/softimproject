"use client";

import { useState } from "react";
import { useCurrentUser } from "@/queries/auth";
import {
  useWorklogs,
  useCreateWorklog,
  useCreateWorklogsBatch,
  useDeleteWorklog,
} from "@/queries/worklogs";
import { EditWorklogDialog } from "@/components/shared/edit-worklog-dialog";
import {
  createWorklogSchema,
  type CreateWorklogInput,
  worklogBatchSchema,
  type WorklogBatchInput,
} from "@/schemas/worklog";
import { GlobalRole, ProjectRole, type Worklog } from "@/types";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { Clock, Trash2, Pencil, Send, Plus, Layers, AlertTriangle } from "lucide-react";
import { useForm, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { format } from "date-fns";

export function BulkWorklogForm({
  projectId,
  ticketId,
  currentTotalHours,
  estimatedHours,
  onDone,
}: {
  projectId: string;
  ticketId: string;
  currentTotalHours: number;
  estimatedHours?: number;
  onDone: () => void;
}) {
  const t = useTranslations("TicketWorklogs");
  const createBatch = useCreateWorklogsBatch();

  const emptyRow = () => ({
    date: format(new Date(), "yyyy-MM-dd"),
    hours: 1,
    description: "",
    isBillable: true,
  });

  const {
    register,
    handleSubmit,
    control,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<WorklogBatchInput>({
    resolver: zodResolver(worklogBatchSchema),
    defaultValues: { items: [emptyRow()] },
  });
  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  const items = watch("items");
  const batchHours = (items ?? []).reduce((sum, i) => sum + (Number(i.hours) || 0), 0);
  const projectedTotal = currentTotalHours + batchHours;
  const hasBudget = typeof estimatedHours === "number" && estimatedHours > 0;
  const willExceed = hasBudget && projectedTotal > (estimatedHours as number);

  const inputClass =
    "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

  const onSubmit = async (data: WorklogBatchInput) => {
    try {
      await createBatch.mutateAsync({ projectId, ticketId, items: data.items });
      toast.success(t("bulkAdded", { count: data.items.length }));
      onDone();
    } catch {
      toast.error(t("addFailed"));
    }
  };

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-3 rounded-lg border border-border p-3"
    >
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold text-foreground flex items-center gap-1">
          <Layers className="h-3.5 w-3.5" />
          {t("bulkTitle")}
        </span>
        <span className="text-xs text-muted-foreground">
          {hasBudget
            ? t("bulkProjected", {
                hours: projectedTotal.toFixed(2),
                budget: (estimatedHours as number).toFixed(2),
              })
            : t("bulkSum", { hours: batchHours.toFixed(2) })}
        </span>
      </div>

      {willExceed && (
        <div className="flex items-start gap-2 rounded-lg bg-warning/15 px-3 py-2 text-xs text-warning">
          <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" />
          <span>{t("bulkOverBudgetWarning")}</span>
        </div>
      )}

      <div className="space-y-2">
        {fields.map((field, index) => (
          <div key={field.id} className="rounded-lg border border-border p-2 space-y-2">
            <div className="grid grid-cols-[1fr_auto_auto] gap-2 items-start">
              <div>
                <input type="date" {...register(`items.${index}.date`)} className={inputClass} />
                {errors.items?.[index]?.date && (
                  <p className="text-xs text-destructive mt-1">
                    {errors.items[index]?.date?.message}
                  </p>
                )}
              </div>
              <div className="w-24">
                <input
                  type="number"
                  step="0.25"
                  min="0.25"
                  max="24"
                  placeholder={t("hours")}
                  {...register(`items.${index}.hours`, { valueAsNumber: true })}
                  className={inputClass}
                />
                {errors.items?.[index]?.hours && (
                  <p className="text-xs text-destructive mt-1">
                    {errors.items[index]?.hours?.message}
                  </p>
                )}
              </div>
              <button
                type="button"
                onClick={() => remove(index)}
                disabled={fields.length === 1}
                className="p-2 text-muted-foreground hover:text-destructive rounded disabled:opacity-30"
                title={t("removeRow")}
                aria-label={t("removeRow")}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </div>
            <div>
              <textarea
                {...register(`items.${index}.description`)}
                rows={2}
                placeholder={t("descriptionPlaceholder")}
                className={`${inputClass} resize-y`}
              />
              {errors.items?.[index]?.description && (
                <p className="text-xs text-destructive mt-1">
                  {errors.items[index]?.description?.message}
                </p>
              )}
            </div>
            <label className="flex items-center gap-2 text-xs text-muted-foreground">
              <input
                type="checkbox"
                {...register(`items.${index}.isBillable`)}
                className="rounded"
              />
              {t("billable")}
            </label>
          </div>
        ))}
      </div>

      <div className="flex items-center justify-between">
        <button
          type="button"
          onClick={() => append(emptyRow())}
          disabled={fields.length >= 50}
          className="inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium text-primary hover:bg-muted transition-colors disabled:opacity-50"
        >
          <Plus className="h-3.5 w-3.5" />
          {t("addRow")}
        </button>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={onDone}
            className="px-2.5 py-1 rounded text-xs text-muted-foreground hover:text-foreground"
          >
            {t("cancel")}
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center gap-1 px-2.5 py-1 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
          >
            <Send className="h-3.5 w-3.5" />
            {isSubmitting ? t("saving") : t("saveAll")}
          </button>
        </div>
      </div>
    </form>
  );
}

export function WorklogsSection({
  projectId,
  ticketId,
  estimatedHours,
}: {
  projectId: string;
  ticketId: string;
  estimatedHours?: number;
}) {
  const t = useTranslations("TicketWorklogs");
  const { data: worklogs, isLoading } = useWorklogs({ projectId, ticketId });
  const { data: currentUser } = useCurrentUser();
  const createWorklog = useCreateWorklog();
  const deleteWorklog = useDeleteWorklog();
  const [isAdding, setIsAdding] = useState(false);
  const [isBulkAdding, setIsBulkAdding] = useState(false);
  const [editingWorklog, setEditingWorklog] = useState<Worklog | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateWorklogInput>({
    resolver: zodResolver(createWorklogSchema),
    defaultValues: {
      projectId,
      ticketId,
      date: format(new Date(), "yyyy-MM-dd"),
      hours: 1,
      description: "",
      isBillable: true,
    },
  });
  const description = watch("description") ?? "";

  const canManage = (w: Worklog) =>
    !!currentUser &&
    (currentUser.id === w.userId ||
      currentUser.globalRole === GlobalRole.Admin ||
      currentUser.projectRoles.some(
        (pr) => pr.projectId === projectId && pr.role === ProjectRole.ProjectManager
      ));

  const totalHours = (worklogs ?? []).reduce((sum, w) => sum + w.hours, 0);
  const hasBudget = typeof estimatedHours === "number" && estimatedHours > 0;
  const isOverBudget = hasBudget && totalHours > (estimatedHours as number);

  const onSubmit = async (data: CreateWorklogInput) => {
    try {
      await createWorklog.mutateAsync({
        projectId,
        ticketId,
        date: data.date,
        hours: data.hours,
        description: data.description,
        isBillable: data.isBillable,
      });
      reset({
        projectId,
        ticketId,
        date: format(new Date(), "yyyy-MM-dd"),
        hours: 1,
        description: "",
        isBillable: true,
      });
      setIsAdding(false);
      toast.success(t("added"));
    } catch {
      toast.error(t("addFailed"));
    }
  };

  const handleDelete = async (w: Worklog) => {
    if (!window.confirm(t("deleteConfirm"))) return;
    try {
      await deleteWorklog.mutateAsync({ projectId, worklogId: w.id });
      toast.success(t("deleted"));
    } catch {
      toast.error(t("deleteFailed"));
    }
  };

  const inputClass =
    "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground flex items-center gap-2 flex-wrap">
          <Clock className="h-4 w-4" />
          {t("title")}
          {(worklogs?.length ?? 0) > 0 && (
            <span className="text-xs font-normal text-muted-foreground">
              {hasBudget
                ? t("totalOfBudget", {
                    hours: totalHours.toFixed(2),
                    budget: (estimatedHours as number).toFixed(2),
                  })
                : t("totalHours", { hours: totalHours.toFixed(2) })}
            </span>
          )}
          {isOverBudget && (
            <span className="inline-flex items-center gap-1 text-[10px] px-1.5 py-0.5 rounded bg-warning/15 text-warning font-medium">
              <AlertTriangle className="h-3 w-3" />
              {t("overBudget")}
            </span>
          )}
        </h3>
        {!isAdding && !isBulkAdding && (
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => setIsAdding(true)}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium text-primary hover:bg-muted transition-colors"
            >
              <Plus className="h-3.5 w-3.5" />
              {t("add")}
            </button>
            <button
              type="button"
              onClick={() => setIsBulkAdding(true)}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium text-primary hover:bg-muted transition-colors"
            >
              <Layers className="h-3.5 w-3.5" />
              {t("addBulk")}
            </button>
          </div>
        )}
      </div>

      {isBulkAdding && (
        <BulkWorklogForm
          projectId={projectId}
          ticketId={ticketId}
          currentTotalHours={totalHours}
          estimatedHours={hasBudget ? (estimatedHours as number) : undefined}
          onDone={() => setIsBulkAdding(false)}
        />
      )}

      {isAdding && (
        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-3 rounded-lg border border-border p-3"
        >
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("date")}
              </label>
              <input type="date" {...register("date")} className={inputClass} />
              {errors.date && (
                <p className="text-xs text-destructive mt-1">{errors.date.message}</p>
              )}
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("hours")}
              </label>
              <input
                type="number"
                step="0.25"
                min="0.25"
                max="24"
                {...register("hours", { valueAsNumber: true })}
                className={inputClass}
              />
              {errors.hours && (
                <p className="text-xs text-destructive mt-1">{errors.hours.message}</p>
              )}
            </div>
          </div>
          <div>
            <label className="flex items-center justify-between text-xs font-medium text-muted-foreground mb-1">
              <span>{t("description")}</span>
              <span className="font-normal">
                {t("descriptionCounter", { count: description.length })}
              </span>
            </label>
            <textarea
              {...register("description")}
              rows={2}
              placeholder={t("descriptionPlaceholder")}
              className={`${inputClass} resize-y`}
            />
            {errors.description && (
              <p className="text-xs text-destructive mt-1">{errors.description.message}</p>
            )}
          </div>
          <div className="flex items-center justify-between">
            <label className="flex items-center gap-2 text-sm text-muted-foreground">
              <input type="checkbox" {...register("isBillable")} className="rounded" />
              {t("billable")}
            </label>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => {
                  setIsAdding(false);
                  reset();
                }}
                className="px-2.5 py-1 rounded text-xs text-muted-foreground hover:text-foreground"
              >
                {t("cancel")}
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex items-center gap-1 px-2.5 py-1 rounded bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 disabled:opacity-50"
              >
                <Send className="h-3.5 w-3.5" />
                {isSubmitting ? t("saving") : t("save")}
              </button>
            </div>
          </div>
        </form>
      )}

      {isLoading && <Skeleton className="h-16 w-full" />}

      {worklogs && worklogs.length > 0 ? (
        <div className="space-y-2">
          {worklogs.map((w) => (
            <div key={w.id} className="rounded-lg border border-border p-3">
              <div className="flex items-center justify-between gap-2 mb-1">
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <span className="font-medium text-foreground">{w.user.displayName}</span>
                  <span>{format(new Date(w.date), "yyyy-MM-dd")}</span>
                  <span className="font-medium text-foreground">{w.hours.toFixed(2)}h</span>
                  {w.isBillable && (
                    <span className="text-[10px] px-1.5 py-0.5 rounded bg-green-100 text-green-700 font-medium">
                      {t("billable")}
                    </span>
                  )}
                </div>
                {canManage(w) && (
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => setEditingWorklog(w)}
                      className="p-1 text-muted-foreground hover:text-foreground rounded"
                      title={t("editAria")}
                      aria-label={t("editAria")}
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(w)}
                      disabled={deleteWorklog.isPending}
                      className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                      title={t("deleteAria")}
                      aria-label={t("deleteAria")}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                )}
              </div>
              {w.description && (
                <p className="whitespace-pre-wrap text-sm text-foreground">{w.description}</p>
              )}
            </div>
          ))}
        </div>
      ) : (
        !isLoading && <p className="text-sm text-muted-foreground">{t("none")}</p>
      )}

      <EditWorklogDialog
        worklog={editingWorklog}
        open={!!editingWorklog}
        onClose={() => setEditingWorklog(null)}
      />
    </div>
  );
}
