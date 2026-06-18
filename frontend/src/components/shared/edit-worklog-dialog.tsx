"use client";

import { useEffect } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { X } from "lucide-react";
import { format } from "date-fns";
import { useUpdateWorklog } from "@/queries/worklogs";
import { useTickets } from "@/queries/tickets";
import { useAdminUsers } from "@/queries/admin";
import { useCurrentUser } from "@/queries/auth";
import { updateWorklogSchema, type UpdateWorklogInput } from "@/schemas/worklog";
import { GlobalRole, type Worklog } from "@/types";

export function EditWorklogDialog({
  worklog,
  open,
  onClose,
}: {
  worklog: Worklog | null;
  open: boolean;
  onClose: () => void;
}) {
  const t = useTranslations("EditWorklog");
  const tCommon = useTranslations("Common");
  const updateWorklog = useUpdateWorklog();
  const { data: currentUser } = useCurrentUser();
  const isAdmin = currentUser?.globalRole === GlobalRole.Admin;
  const { data: adminUsers } = useAdminUsers();
  const { data: ticketsPage } = useTickets(worklog?.projectId ?? "", { pageSize: 200 });
  const tickets = ticketsPage?.items ?? [];
  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<UpdateWorklogInput>({
    resolver: zodResolver(updateWorklogSchema),
  });
  const description = useWatch({ control, name: "description" }) ?? "";

  useEffect(() => {
    if (worklog && open) {
      reset({
        ticketId: worklog.ticketId,
        date: format(new Date(worklog.date), "yyyy-MM-dd"),
        hours: worklog.hours,
        description: worklog.description ?? "",
        isBillable: worklog.isBillable,
        invoiced: worklog.invoiced ?? "",
        overrideUserId: worklog.userId,
      });
    }
  }, [worklog, open, reset]);

  const onSubmit = async (data: UpdateWorklogInput) => {
    if (!worklog) return;
    try {
      await updateWorklog.mutateAsync({
        projectId: worklog.projectId,
        worklogId: worklog.id,
        ticketId: data.ticketId,
        date: data.date,
        hours: data.hours,
        description: data.description,
        isBillable: data.isBillable,
        invoiced: data.invoiced?.trim() ? data.invoiced : undefined,
        overrideUserId: isAdmin && data.overrideUserId ? data.overrideUserId : undefined,
      });
      toast.success(t("updated"));
      onClose();
    } catch {
      toast.error(t("updateFailed"));
    }
  };

  if (!open || !worklog) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-card-foreground">{t("title")}</h2>
          <button
            onClick={onClose}
            className="p-1 rounded hover:bg-muted transition-colors"
            aria-label={t("close")}
          >
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("ticket")} <span className="text-destructive">*</span>
            </label>
            <select
              {...register("ticketId")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">{t("selectTicket")}</option>
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

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("date")}
            </label>
            <input
              {...register("date")}
              type="date"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.date && <p className="text-xs text-destructive mt-1">{errors.date.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("hours")}
            </label>
            <input
              {...register("hours", { valueAsNumber: true })}
              type="number"
              step="0.25"
              min="0.25"
              max="24"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.hours && (
              <p className="text-xs text-destructive mt-1">{errors.hours.message}</p>
            )}
          </div>

          <div>
            <label className="flex items-center justify-between text-sm font-medium text-card-foreground mb-1">
              <span>
                {t("description")} <span className="text-destructive">*</span>
              </span>
              <span className="text-xs font-normal text-muted-foreground">
                {t("descriptionCounter", { count: description.length })}
              </span>
            </label>
            <textarea
              {...register("description")}
              rows={3}
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
                {t("ownerAdmin")}
              </label>
              <select
                {...register("overrideUserId")}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              >
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

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("invoiced")}
            </label>
            <input
              {...register("invoiced")}
              type="text"
              placeholder="e.g. INV-2026-0042"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.invoiced && (
              <p className="text-xs text-destructive mt-1">{errors.invoiced.message}</p>
            )}
          </div>

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
              {isSubmitting ? t("saving") : t("save")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
