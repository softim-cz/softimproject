"use client";

import { useState } from "react";
import { useProjects, useCreateProject } from "@/queries/projects";
import { HealthIndicator } from "@/components/shared/health-indicator";
import { CardSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import {
  FolderKanban,
  Plus,
  Search,
  X,
  AlertTriangle,
  Clock,
} from "lucide-react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createProjectSchema,
  type CreateProjectInput,
} from "@/schemas/project";
import { toast } from "sonner";
import { ProjectStatus } from "@/types";
import type { Project } from "@/types";

const statusColors: Record<ProjectStatus, string> = {
  [ProjectStatus.Active]: "bg-green-100 text-green-700",
  [ProjectStatus.OnHold]: "bg-yellow-100 text-yellow-700",
  [ProjectStatus.Completed]: "bg-blue-100 text-blue-700",
  [ProjectStatus.Archived]: "bg-gray-100 text-gray-500",
};

function CreateProjectDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const createProject = useCreateProject();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateProjectInput>({
    resolver: zodResolver(createProjectSchema),
  });

  const onSubmit = async (data: CreateProjectInput) => {
    try {
      await createProject.mutateAsync(data);
      toast.success("Project created successfully");
      reset();
      onClose();
    } catch {
      toast.error("Failed to create project");
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div
        className="absolute inset-0 bg-black/50"
        onClick={onClose}
      />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-lg mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-card-foreground">
            New Project
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
              Name
            </label>
            <input
              {...register("name")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Project name"
            />
            {errors.name && (
              <p className="text-xs text-destructive mt-1">
                {errors.name.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Code
            </label>
            <input
              {...register("code")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring uppercase"
              placeholder="PROJ"
              maxLength={6}
            />
            {errors.code && (
              <p className="text-xs text-destructive mt-1">
                {errors.code.message}
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
              placeholder="Optional description"
            />
            {errors.description && (
              <p className="text-xs text-destructive mt-1">
                {errors.description.message}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Start Date
              </label>
              <input
                {...register("startDate")}
                type="date"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                End Date
              </label>
              <input
                {...register("endDate")}
                type="date"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Budget Hours
              </label>
              <input
                {...register("budgetHours", { valueAsNumber: true })}
                type="number"
                step="0.5"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="0"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Budget Amount
              </label>
              <input
                {...register("budgetAmount", { valueAsNumber: true })}
                type="number"
                step="100"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="0"
              />
            </div>
          </div>

          <div className="flex justify-end gap-3 pt-4">
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
              {isSubmitting ? "Creating..." : "Create Project"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function ProjectsPage() {
  const { data: projects, isLoading, error } = useProjects();
  const [search, setSearch] = useState("");
  const [dialogOpen, setDialogOpen] = useState(false);

  const filteredProjects = projects?.filter(
    (p: Project) =>
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      p.code.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Projects</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage your projects and track progress
          </p>
        </div>
        <button
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
        >
          <Plus className="h-4 w-4" />
          New Project
        </button>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input
          type="text"
          placeholder="Search projects..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full rounded-lg border border-input bg-background pl-10 pr-4 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
        />
      </div>

      {/* Project list */}
      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <CardSkeleton key={i} />
          ))}
        </div>
      )}

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          Failed to load projects. Please try again.
        </div>
      )}

      {filteredProjects && filteredProjects.length === 0 && (
        <EmptyState
          icon={<FolderKanban className="h-12 w-12" />}
          title={search ? "No matching projects" : "No projects yet"}
          description={
            search
              ? "Try adjusting your search terms."
              : "Create your first project to get started."
          }
        />
      )}

      {filteredProjects && filteredProjects.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredProjects.map((project: Project) => (
            <Link
              key={project.id}
              href={`/projects/${project.id}/board`}
              className="block rounded-lg border border-border bg-card p-5 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between mb-3">
                <div>
                  <h3 className="font-semibold text-card-foreground">
                    {project.name}
                  </h3>
                  <p className="text-xs text-muted-foreground font-mono">
                    {project.code}
                  </p>
                </div>
                <span
                  className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[project.status]}`}
                >
                  {project.status}
                </span>
              </div>

              {project.description && (
                <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                  {project.description}
                </p>
              )}

              <div className="flex items-center justify-between">
                <HealthIndicator score={project.healthScore} size="sm" />
                <div className="flex gap-2">
                  {project.isOverBudget && (
                    <AlertTriangle className="h-4 w-4 text-red-500" />
                  )}
                  {project.isOverDeadline && (
                    <Clock className="h-4 w-4 text-orange-500" />
                  )}
                </div>
              </div>

              {project.budgetHours && (
                <div className="mt-3">
                  <div className="flex justify-between text-xs text-muted-foreground mb-1">
                    <span>Hours</span>
                    <span>
                      {project.spentHours.toFixed(1)} / {project.budgetHours}h
                    </span>
                  </div>
                  <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                    <div
                      className="h-full rounded-full bg-accent-orange transition-all"
                      style={{
                        width: `${Math.min((project.spentHours / project.budgetHours) * 100, 100)}%`,
                      }}
                    />
                  </div>
                </div>
              )}
            </Link>
          ))}
        </div>
      )}

      <CreateProjectDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
      />
    </div>
  );
}
