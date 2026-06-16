"use client";

import { useMemo, useState } from "react";
import {
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameMonth,
  isToday,
  startOfMonth,
  startOfWeek,
  subMonths,
} from "date-fns";
import { useForm, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { ChevronLeft, ChevronRight, Plus, Trash2, X, AlertTriangle, Layers } from "lucide-react";
import { useProjects } from "@/queries/projects";
import { useTickets } from "@/queries/tickets";
import { useWorklogs, useCreateWorklog, useDeleteWorklog } from "@/queries/worklogs";
import { useCurrentUser } from "@/queries/auth";
import {
  dayWorklogSchema,
  type DayWorklogInput,
  singleDayWorklogSchema,
  type SingleDayWorklogInput,
  MAX_DAY_ITEMS,
  MAX_DAY_HOURS,
} from "@/schemas/worklog";
import type { Project, Worklog } from "@/types";

const inputClass =
  "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

type TicketFilter = { projectId: string; ticketId: string; label: string };

export function WorklogCalendar() {
  const t = useTranslations("WorklogsPage");
  const { data: currentUser } = useCurrentUser();
  const { data: projects } = useProjects();

  // The ticket allows the current month and exactly one month back.
  const thisMonth = useMemo(() => startOfMonth(new Date()), []);
  const minMonth = useMemo(() => subMonths(thisMonth, 1), [thisMonth]);
  const [month, setMonth] = useState(thisMonth);

  // Optional calendar-wide ticket filter. When set, the day dialog collapses to a
  // single-entry form and the calendar shows only that ticket's worklogs.
  const [filterProjectId, setFilterProjectId] = useState("");
  const [filterTicketId, setFilterTicketId] = useState("");
  const { data: filterTicketsPage } = useTickets(filterProjectId || "", { pageSize: 200 });
  const filterTickets = filterTicketsPage?.items ?? [];

  const [selectedDay, setSelectedDay] = useState<Date | null>(null);

  const monthStart = startOfMonth(month);
  const monthEnd = endOfMonth(month);
  const gridStart = startOfWeek(monthStart, { weekStartsOn: 1 });
  const gridEnd = endOfWeek(monthEnd, { weekStartsOn: 1 });
  const days = eachDayOfInterval({ start: gridStart, end: gridEnd });

  const { data: worklogs } = useWorklogs({
    from: format(monthStart, "yyyy-MM-dd"),
    to: format(monthEnd, "yyyy-MM-dd"),
    userId: currentUser?.id,
    ticketId: filterTicketId || undefined,
  });

  // Aggregate hours/count per day for the overview, plus quick lookup of a day's logs.
  const byDay = useMemo(() => {
    const map = new Map<string, { hours: number; logs: Worklog[] }>();
    for (const w of worklogs ?? []) {
      const key = w.date.slice(0, 10);
      const entry = map.get(key) ?? { hours: 0, logs: [] };
      entry.hours += w.hours;
      entry.logs.push(w);
      map.set(key, entry);
    }
    return map;
  }, [worklogs]);

  const activeFilter: TicketFilter | null = useMemo(() => {
    if (!filterTicketId) return null;
    const tk = filterTickets.find((x) => x.id === filterTicketId);
    return {
      projectId: filterProjectId,
      ticketId: filterTicketId,
      label: tk ? `#${tk.number} — ${tk.title}` : "",
    };
  }, [filterTicketId, filterProjectId, filterTickets]);

  const canGoPrev = month > minMonth;
  const canGoNext = month < thisMonth;

  const weekdayLabels = t("calendar.weekdays").split(",");

  return (
    <div className="rounded-lg border border-border bg-card p-5">
      {/* Header: month navigation + ticket filter */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => canGoPrev && setMonth(subMonths(month, 1))}
            disabled={!canGoPrev}
            className="p-1.5 rounded-lg border border-border hover:bg-muted transition-colors disabled:opacity-30"
            aria-label={t("calendar.prevMonth")}
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="text-sm font-semibold text-card-foreground min-w-[9rem] text-center">
            {format(month, "LLLL yyyy")}
          </span>
          <button
            type="button"
            onClick={() => canGoNext && setMonth(addMonths(month, 1))}
            disabled={!canGoNext}
            className="p-1.5 rounded-lg border border-border hover:bg-muted transition-colors disabled:opacity-30"
            aria-label={t("calendar.nextMonth")}
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>

        <div className="flex items-center gap-2">
          <select
            value={filterProjectId}
            onChange={(e) => {
              setFilterProjectId(e.target.value);
              setFilterTicketId("");
            }}
            className="rounded-lg border border-input bg-background px-3 py-1.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">{t("calendar.allProjects")}</option>
            {projects?.map((p: Project) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
          <select
            value={filterTicketId}
            onChange={(e) => setFilterTicketId(e.target.value)}
            disabled={!filterProjectId}
            className="rounded-lg border border-input bg-background px-3 py-1.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-50"
          >
            <option value="">{t("calendar.allTickets")}</option>
            {filterTickets.map((tk) => (
              <option key={tk.id} value={tk.id}>
                #{tk.number} — {tk.title}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Weekday header */}
      <div className="grid grid-cols-7 gap-1 mb-1">
        {weekdayLabels.map((d) => (
          <div key={d} className="text-center text-[11px] font-medium text-muted-foreground py-1">
            {d}
          </div>
        ))}
      </div>

      {/* Day grid */}
      <div className="grid grid-cols-7 gap-1">
        {days.map((day) => {
          const key = format(day, "yyyy-MM-dd");
          const inMonth = isSameMonth(day, month);
          const dayData = byDay.get(key);
          const interactive = inMonth;
          return (
            <button
              key={key}
              type="button"
              disabled={!interactive}
              onClick={() => interactive && setSelectedDay(day)}
              className={[
                "min-h-[64px] rounded-lg border p-1.5 text-left transition-colors",
                inMonth ? "border-border hover:bg-muted" : "border-transparent opacity-40",
                isToday(day) ? "ring-1 ring-accent-orange" : "",
              ].join(" ")}
            >
              <div className="flex items-center justify-between">
                <span
                  className={`text-xs ${isToday(day) ? "font-bold text-accent-orange" : "text-foreground"}`}
                >
                  {format(day, "d")}
                </span>
              </div>
              {dayData && dayData.hours > 0 && (
                <div className="mt-1">
                  <span
                    className={`inline-block text-[10px] px-1.5 py-0.5 rounded font-medium ${
                      dayData.hours > MAX_DAY_HOURS
                        ? "bg-warning/15 text-warning"
                        : "bg-accent-orange/10 text-accent-orange"
                    }`}
                  >
                    {t("calendar.hoursShort", { hours: dayData.hours.toFixed(2) })}
                  </span>
                </div>
              )}
            </button>
          );
        })}
      </div>

      <p className="mt-3 text-xs text-muted-foreground">{t("calendar.hint")}</p>

      {selectedDay && (
        <DayWorklogDialog
          date={selectedDay}
          projects={projects ?? []}
          filter={activeFilter}
          existing={byDay.get(format(selectedDay, "yyyy-MM-dd"))?.logs ?? []}
          onClose={() => setSelectedDay(null)}
        />
      )}
    </div>
  );
}

function DayWorklogDialog({
  date,
  projects,
  filter,
  existing,
  onClose,
}: {
  date: Date;
  projects: Project[];
  filter: TicketFilter | null;
  existing: Worklog[];
  onClose: () => void;
}) {
  const t = useTranslations("WorklogsPage");
  const dateStr = format(date, "yyyy-MM-dd");
  const existingHours = existing.reduce((sum, w) => sum + w.hours, 0);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-2xl mx-4 p-6 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-lg font-semibold text-card-foreground">
              {format(date, "EEEE d. LLLL yyyy")}
            </h2>
            {filter && <p className="text-xs text-muted-foreground mt-0.5">{filter.label}</p>}
          </div>
          <button onClick={onClose} className="p-1 rounded hover:bg-muted transition-colors">
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        {existing.length > 0 && (
          <div className="mb-4 space-y-1.5">
            <p className="text-xs font-medium text-muted-foreground">
              {t("calendar.existingForDay", {
                count: existing.length,
                hours: existingHours.toFixed(2),
              })}
            </p>
            {existing.map((w) => (
              <DayExistingRow key={w.id} worklog={w} />
            ))}
          </div>
        )}

        {filter ? (
          <SingleDayForm
            date={dateStr}
            filter={filter}
            existingHours={existingHours}
            onDone={onClose}
          />
        ) : (
          <MultiDayForm
            date={dateStr}
            projects={projects}
            existingHours={existingHours}
            existingCount={existing.length}
            onDone={onClose}
          />
        )}
      </div>
    </div>
  );
}

function DayExistingRow({ worklog }: { worklog: Worklog }) {
  const t = useTranslations("WorklogsPage");
  const deleteWorklog = useDeleteWorklog();
  const handleDelete = async () => {
    if (!window.confirm(t("deleteConfirm"))) return;
    try {
      await deleteWorklog.mutateAsync({ projectId: worklog.projectId, worklogId: worklog.id });
      toast.success(t("deleted"));
    } catch {
      toast.error(t("deleteFailed"));
    }
  };
  return (
    <div className="flex items-center justify-between gap-2 rounded-lg border border-border px-2.5 py-1.5 text-xs">
      <span className="truncate text-muted-foreground">
        <span className="font-medium text-foreground">{worklog.hours.toFixed(2)}h</span>{" "}
        {worklog.ticketTitle ?? ""} — {worklog.description}
      </span>
      <button
        type="button"
        onClick={handleDelete}
        disabled={deleteWorklog.isPending}
        className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50 shrink-0"
        aria-label={t("deleteAriaLabel")}
      >
        <Trash2 className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}

function SingleDayForm({
  date,
  filter,
  existingHours,
  onDone,
}: {
  date: string;
  filter: TicketFilter;
  existingHours: number;
  onDone: () => void;
}) {
  const t = useTranslations("WorklogsPage");
  const createWorklog = useCreateWorklog();
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<SingleDayWorklogInput>({
    resolver: zodResolver(singleDayWorklogSchema),
    defaultValues: { hours: 1, description: "", isBillable: true },
  });
  const hours = Number(watch("hours")) || 0;
  const wouldExceed = existingHours + hours > MAX_DAY_HOURS;

  const onSubmit = async (data: SingleDayWorklogInput) => {
    if (existingHours + data.hours > MAX_DAY_HOURS) {
      toast.error(t("calendar.dayHoursExceeded", { max: MAX_DAY_HOURS }));
      return;
    }
    try {
      await createWorklog.mutateAsync({
        projectId: filter.projectId,
        ticketId: filter.ticketId,
        date,
        hours: data.hours,
        description: data.description,
        isBillable: data.isBillable,
      });
      toast.success(t("added"));
      onDone();
    } catch {
      toast.error(t("addFailed"));
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
      <div className="w-32">
        <label className="block text-xs font-medium text-muted-foreground mb-1">
          {t("columns.hours")}
        </label>
        <input
          type="number"
          step="0.25"
          min="0.25"
          max="24"
          {...register("hours", { valueAsNumber: true })}
          className={inputClass}
        />
        {errors.hours && <p className="text-xs text-destructive mt-1">{errors.hours.message}</p>}
      </div>
      <div>
        <label className="block text-xs font-medium text-muted-foreground mb-1">
          {t("columns.description")}
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
      <label className="flex items-center gap-2 text-sm text-muted-foreground">
        <input type="checkbox" {...register("isBillable")} className="rounded" />
        {t("billable")}
      </label>
      {wouldExceed && (
        <div className="flex items-start gap-2 rounded-lg bg-warning/15 px-3 py-2 text-xs text-warning">
          <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" />
          <span>{t("calendar.dayHoursExceeded", { max: MAX_DAY_HOURS })}</span>
        </div>
      )}
      <FormActions isSubmitting={isSubmitting} onCancel={onDone} />
    </form>
  );
}

function MultiDayForm({
  date,
  projects,
  existingHours,
  existingCount,
  onDone,
}: {
  date: string;
  projects: Project[];
  existingHours: number;
  existingCount: number;
  onDone: () => void;
}) {
  const t = useTranslations("WorklogsPage");
  const createWorklog = useCreateWorklog();
  const emptyRow = () => ({
    projectId: "",
    ticketId: "",
    hours: 1,
    description: "",
    isBillable: true,
  });
  const {
    register,
    handleSubmit,
    control,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<DayWorklogInput>({
    resolver: zodResolver(dayWorklogSchema),
    defaultValues: { items: [emptyRow()] },
  });
  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  const items = watch("items");
  const newHours = (items ?? []).reduce((sum, i) => sum + (Number(i.hours) || 0), 0);
  const projectedHours = existingHours + newHours;
  const projectedCount = existingCount + (items?.length ?? 0);
  const hoursExceeded = projectedHours > MAX_DAY_HOURS;
  const itemsExceeded = projectedCount > MAX_DAY_ITEMS;
  const canAddRow = projectedCount < MAX_DAY_ITEMS;

  const onSubmit = async (data: DayWorklogInput) => {
    if (existingHours + data.items.reduce((s, i) => s + i.hours, 0) > MAX_DAY_HOURS) {
      toast.error(t("calendar.dayHoursExceeded", { max: MAX_DAY_HOURS }));
      return;
    }
    if (existingCount + data.items.length > MAX_DAY_ITEMS) {
      toast.error(t("calendar.dayItemsExceeded", { max: MAX_DAY_ITEMS }));
      return;
    }
    try {
      // Each row can target a different project/ticket, so reuse the per-ticket
      // create endpoint (authorized per project) once per row.
      for (const item of data.items) {
        await createWorklog.mutateAsync({
          projectId: item.projectId,
          ticketId: item.ticketId,
          date,
          hours: item.hours,
          description: item.description,
          isBillable: item.isBillable,
        });
      }
      toast.success(t("calendar.dayAdded", { count: data.items.length }));
      onDone();
    } catch {
      toast.error(t("addFailed"));
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <span className="flex items-center gap-1">
          <Layers className="h-3.5 w-3.5" />
          {t("calendar.multiTitle")}
        </span>
        <span>
          {t("calendar.dayTotal", { hours: projectedHours.toFixed(2), max: MAX_DAY_HOURS })}
        </span>
      </div>

      {(hoursExceeded || itemsExceeded) && (
        <div className="flex items-start gap-2 rounded-lg bg-warning/15 px-3 py-2 text-xs text-warning">
          <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" />
          <span>
            {hoursExceeded
              ? t("calendar.dayHoursExceeded", { max: MAX_DAY_HOURS })
              : t("calendar.dayItemsExceeded", { max: MAX_DAY_ITEMS })}
          </span>
        </div>
      )}

      <div className="space-y-2">
        {fields.map((field, index) => (
          <MultiDayRow
            key={field.id}
            index={index}
            projects={projects}
            projectId={items?.[index]?.projectId ?? ""}
            register={register}
            setValue={setValue}
            errors={errors}
            onRemove={() => remove(index)}
            canRemove={fields.length > 1}
          />
        ))}
      </div>

      <button
        type="button"
        onClick={() => append(emptyRow())}
        disabled={!canAddRow}
        className="inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium text-primary hover:bg-muted transition-colors disabled:opacity-50"
      >
        <Plus className="h-3.5 w-3.5" />
        {t("calendar.addRow")}
      </button>

      <FormActions
        isSubmitting={isSubmitting}
        disabled={hoursExceeded || itemsExceeded}
        onCancel={onDone}
      />
    </form>
  );
}

// Isolated so each row can own its ticket query keyed by that row's project.
function MultiDayRow({
  index,
  projects,
  projectId,
  register,
  setValue,
  errors,
  onRemove,
  canRemove,
}: {
  index: number;
  projects: Project[];
  projectId: string;
  register: ReturnType<typeof useForm<DayWorklogInput>>["register"];
  setValue: ReturnType<typeof useForm<DayWorklogInput>>["setValue"];
  errors: ReturnType<typeof useForm<DayWorklogInput>>["formState"]["errors"];
  onRemove: () => void;
  canRemove: boolean;
}) {
  const t = useTranslations("WorklogsPage");
  const { data: ticketsPage } = useTickets(projectId || "", { pageSize: 200 });
  const tickets = ticketsPage?.items ?? [];
  const projectReg = register(`items.${index}.projectId`);
  const rowErrors = errors.items?.[index];

  return (
    <div className="rounded-lg border border-border p-2 space-y-2">
      <div className="grid grid-cols-[1fr_1fr_auto_auto] gap-2 items-start">
        <div>
          <select
            {...projectReg}
            onChange={(e) => {
              projectReg.onChange(e);
              setValue(`items.${index}.ticketId`, "");
            }}
            className={inputClass}
          >
            <option value="">{t("selectProject")}</option>
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
          {rowErrors?.projectId && (
            <p className="text-xs text-destructive mt-1">{rowErrors.projectId.message}</p>
          )}
        </div>
        <div>
          <select
            {...register(`items.${index}.ticketId`)}
            disabled={!projectId}
            className={`${inputClass} disabled:opacity-50`}
          >
            <option value="">{t("selectTicketEllipsis")}</option>
            {tickets.map((tk) => (
              <option key={tk.id} value={tk.id}>
                #{tk.number} — {tk.title}
              </option>
            ))}
          </select>
          {rowErrors?.ticketId && (
            <p className="text-xs text-destructive mt-1">{rowErrors.ticketId.message}</p>
          )}
        </div>
        <div className="w-20">
          <input
            type="number"
            step="0.25"
            min="0.25"
            max="24"
            placeholder={t("columns.hours")}
            {...register(`items.${index}.hours`, { valueAsNumber: true })}
            className={inputClass}
          />
          {rowErrors?.hours && (
            <p className="text-xs text-destructive mt-1">{rowErrors.hours.message}</p>
          )}
        </div>
        <button
          type="button"
          onClick={onRemove}
          disabled={!canRemove}
          className="p-2 text-muted-foreground hover:text-destructive rounded disabled:opacity-30"
          aria-label={t("calendar.removeRow")}
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
        {rowErrors?.description && (
          <p className="text-xs text-destructive mt-1">{rowErrors.description.message}</p>
        )}
      </div>
      <label className="flex items-center gap-2 text-xs text-muted-foreground">
        <input type="checkbox" {...register(`items.${index}.isBillable`)} className="rounded" />
        {t("billable")}
      </label>
    </div>
  );
}

function FormActions({
  isSubmitting,
  disabled,
  onCancel,
}: {
  isSubmitting: boolean;
  disabled?: boolean;
  onCancel: () => void;
}) {
  const t = useTranslations("WorklogsPage");
  const tCommon = useTranslations("Common");
  return (
    <div className="flex items-center justify-end gap-2 pt-1">
      <button
        type="button"
        onClick={onCancel}
        className="px-3 py-1.5 rounded-lg border border-border text-sm text-muted-foreground hover:bg-muted transition-colors"
      >
        {tCommon("cancel")}
      </button>
      <button
        type="submit"
        disabled={isSubmitting || disabled}
        className="px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50"
      >
        {isSubmitting ? t("saving") : t("calendar.saveDay")}
      </button>
    </div>
  );
}
