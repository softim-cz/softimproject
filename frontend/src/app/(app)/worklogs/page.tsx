"use client";

import { useState, useEffect } from "react";
import { useWorklogs, useCreateWorklog } from "@/queries/worklogs";
import { useProjects } from "@/queries/projects";
import { useTimerStore } from "@/stores/timer-store";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { Clock, Plus, X, Play, Square, Timer } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createWorklogSchema,
  type CreateWorklogInput,
} from "@/schemas/worklog";
import { toast } from "sonner";
import { format, startOfWeek, endOfWeek } from "date-fns";
import type { Worklog, Project } from "@/types";
import { formatElapsedTime } from "@/lib/utils";

function TimerDisplay() {
  const {
    isRunning,
    elapsed,
    tick,
    start,
    stop,
    reset,
    description,
  } = useTimerStore();
  const { data: projects } = useProjects();
  const createWorklog = useCreateWorklog();
  const [timerProjectId, setTimerProjectId] = useState("");
  const [timerDescription, setTimerDescription] = useState("");

  useEffect(() => {
    if (!isRunning) return;
    const interval = setInterval(() => tick(), 1000);
    return () => clearInterval(interval);
  }, [isRunning, tick]);

  const handleStart = () => {
    if (!timerProjectId) {
      toast.error("Select a project first");
      return;
    }
    start(timerProjectId, undefined, timerDescription);
  };

  const handleStop = async () => {
    const result = stop();
    if (result.elapsed > 0 && result.projectId) {
      try {
        await createWorklog.mutateAsync({
          projectId: result.projectId,
          date: format(new Date(), "yyyy-MM-dd"),
          hours: parseFloat((result.elapsed / 3600).toFixed(2)),
          description: result.description,
          isBillable: true,
        });
        toast.success("Worklog saved from timer");
        reset();
      } catch {
        toast.error("Failed to save worklog");
      }
    }
  };

  return (
    <div className="rounded-lg border border-border bg-card p-5">
      <div className="flex items-center gap-2 mb-4">
        <Timer className="h-5 w-5 text-accent-orange" />
        <h2 className="text-lg font-semibold text-card-foreground">Timer</h2>
      </div>

      {!isRunning ? (
        <div className="space-y-3">
          <select
            value={timerProjectId}
            onChange={(e) => setTimerProjectId(e.target.value)}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Select project</option>
            {projects?.map((p: Project) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
          <input
            type="text"
            placeholder="What are you working on?"
            value={timerDescription}
            onChange={(e) => setTimerDescription(e.target.value)}
            className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
          <button
            onClick={handleStart}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-green-600 text-white text-sm font-medium hover:bg-green-700 transition-colors"
          >
            <Play className="h-4 w-4" />
            Start Timer
          </button>
        </div>
      ) : (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">
            {description || "Timer running..."}
          </p>
          <p className="text-3xl font-mono font-bold text-accent-orange">
            {formatElapsedTime(elapsed)}
          </p>
          <button
            onClick={handleStop}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-red-600 text-white text-sm font-medium hover:bg-red-700 transition-colors"
          >
            <Square className="h-4 w-4" />
            Stop & Save
          </button>
        </div>
      )}
    </div>
  );
}

function QuickLogDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const { data: projects } = useProjects();
  const createWorklog = useCreateWorklog();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateWorklogInput>({
    resolver: zodResolver(createWorklogSchema),
    defaultValues: {
      date: format(new Date(), "yyyy-MM-dd"),
      isBillable: true,
    },
  });

  const onSubmit = async (data: CreateWorklogInput) => {
    try {
      await createWorklog.mutateAsync(data);
      toast.success("Worklog added");
      reset();
      onClose();
    } catch {
      toast.error("Failed to add worklog");
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-card-foreground">
            Quick Log
          </h2>
          <button
            onClick={onClose}
            className="p-1 rounded hover:bg-muted transition-colors"
          >
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Project
            </label>
            <select
              {...register("projectId")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select project</option>
              {projects?.map((p: Project) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
            {errors.projectId && (
              <p className="text-xs text-destructive mt-1">
                {errors.projectId.message}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Date
              </label>
              <input
                {...register("date")}
                type="date"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Hours
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
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Description
            </label>
            <textarea
              {...register("description")}
              rows={2}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
              placeholder="What did you work on?"
            />
          </div>

          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              {...register("isBillable")}
              className="rounded"
            />
            Billable
          </label>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {isSubmitting ? "Saving..." : "Log Time"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function WorklogsPage() {
  const now = new Date();
  const [from, setFrom] = useState(
    format(startOfWeek(now, { weekStartsOn: 1 }), "yyyy-MM-dd")
  );
  const [to, setTo] = useState(
    format(endOfWeek(now, { weekStartsOn: 1 }), "yyyy-MM-dd")
  );
  const { data: worklogs, isLoading, error } = useWorklogs({ from, to });
  const [dialogOpen, setDialogOpen] = useState(false);

  const totalHours =
    worklogs?.reduce((sum, w) => sum + w.hours, 0).toFixed(2) || "0.00";

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Worklogs</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Track and manage your time entries
          </p>
        </div>
        <button
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
        >
          <Plus className="h-4 w-4" />
          Quick Log
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1">
          <TimerDisplay />
        </div>

        <div className="lg:col-span-2">
          <div className="rounded-lg border border-border bg-card p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-card-foreground">
                Time Entries
              </h2>
              <p className="text-lg font-bold text-accent-orange">
                {totalHours}h total
              </p>
            </div>

            <div className="flex gap-3 mb-4">
              <div>
                <label className="block text-xs text-muted-foreground mb-1">
                  From
                </label>
                <input
                  type="date"
                  value={from}
                  onChange={(e) => setFrom(e.target.value)}
                  className="rounded-lg border border-input bg-background px-3 py-1.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-1">
                  To
                </label>
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
                Failed to load worklogs.
              </div>
            )}

            {worklogs && worklogs.length === 0 && (
              <EmptyState
                icon={<Clock className="h-10 w-10" />}
                title="No worklogs in this range"
                description="Try a different date range or log some time."
              />
            )}

            {worklogs && worklogs.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border">
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        Date
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        User
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        Hours
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        Description
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                        Source
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {worklogs.map((worklog: Worklog) => (
                      <tr key={worklog.id} className="hover:bg-muted/30">
                        <td className="px-3 py-2 text-sm text-foreground">
                          {format(new Date(worklog.date), "MMM d")}
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
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      <QuickLogDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
      />
    </div>
  );
}
