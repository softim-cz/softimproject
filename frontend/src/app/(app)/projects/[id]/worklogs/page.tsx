"use client";

import { use, useState } from "react";
import { useWorklogs, useCreateWorklog } from "@/queries/worklogs";
import { useProject } from "@/queries/projects";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { Clock, Plus, X } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createWorklogSchema,
  type CreateWorklogInput,
} from "@/schemas/worklog";
import { toast } from "sonner";
import { format } from "date-fns";
import type { Worklog } from "@/types";

function AddWorklogDialog({
  open,
  onClose,
  projectId,
}: {
  open: boolean;
  onClose: () => void;
  projectId: string;
}) {
  const createWorklog = useCreateWorklog();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateWorklogInput>({
    resolver: zodResolver(createWorklogSchema),
    defaultValues: {
      projectId,
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
            Add Worklog
          </h2>
          <button
            onClick={onClose}
            className="p-1 rounded hover:bg-muted transition-colors"
          >
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <input type="hidden" {...register("projectId")} />

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Date
            </label>
            <input
              {...register("date")}
              type="date"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.date && (
              <p className="text-xs text-destructive mt-1">
                {errors.date.message}
              </p>
            )}
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
              max="24"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="1.0"
            />
            {errors.hours && (
              <p className="text-xs text-destructive mt-1">
                {errors.hours.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Description
            </label>
            <textarea
              {...register("description")}
              rows={3}
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
              {isSubmitting ? "Adding..." : "Add Worklog"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function ProjectWorklogsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id: projectId } = use(params);
  const { data: project } = useProject(projectId);
  const { data: worklogs, isLoading, error } = useWorklogs({ projectId });
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">
            {project?.name} - Worklogs
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Time entries for this project
          </p>
        </div>
        <button
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
        >
          <Plus className="h-4 w-4" />
          Add Worklog
        </button>
      </div>

      {isLoading && <TableSkeleton rows={8} />}

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          Failed to load worklogs. Please try again.
        </div>
      )}

      {worklogs && worklogs.length === 0 && (
        <EmptyState
          icon={<Clock className="h-12 w-12" />}
          title="No worklogs yet"
          description="Start tracking time for this project."
        />
      )}

      {worklogs && worklogs.length > 0 && (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="bg-muted/50">
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Date
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  User
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Hours
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Description
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Billable
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Source
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {worklogs.map((worklog: Worklog) => (
                <tr key={worklog.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm text-foreground">
                    {format(new Date(worklog.date), "MMM d, yyyy")}
                  </td>
                  <td className="px-4 py-3 text-sm text-foreground">
                    {worklog.user.displayName}
                  </td>
                  <td className="px-4 py-3 text-sm font-medium text-foreground">
                    {worklog.hours.toFixed(2)}h
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground max-w-xs truncate">
                    {worklog.description || "-"}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {worklog.isBillable ? (
                      <span className="text-green-600 text-xs font-medium">
                        Yes
                      </span>
                    ) : (
                      <span className="text-muted-foreground text-xs">No</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {worklog.source}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <AddWorklogDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        projectId={projectId}
      />
    </div>
  );
}
