"use client";

import { use } from "react";
import { useProject, useUpdateProject } from "@/queries/projects";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { Settings, Users, LayoutGrid, Link2 } from "lucide-react";
import { toast } from "sonner";
import { ProjectStatus } from "@/types";

export default function ProjectSettingsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id: projectId } = use(params);
  const { data: project, isLoading, error } = useProject(projectId);
  const updateProject = useUpdateProject();

  if (isLoading) {
    return (
      <div className="space-y-4 max-w-3xl">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-96" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        Failed to load project settings.
      </div>
    );
  }

  const handleStatusChange = async (status: ProjectStatus) => {
    try {
      await updateProject.mutateAsync({ id: projectId, status });
      toast.success("Project status updated");
    } catch {
      toast.error("Failed to update status");
    }
  };

  return (
    <div className="space-y-8 max-w-3xl">
      <p className="text-sm text-muted-foreground">
        Configure project settings and integrations
      </p>

      {/* General settings */}
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <Settings className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">
            General
          </h2>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Name
            </label>
            <p className="text-sm text-foreground">{project.name}</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Code
            </label>
            <p className="text-sm text-foreground font-mono">{project.code}</p>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-card-foreground mb-1">
            Status
          </label>
          <select
            value={project.status}
            onChange={(e) =>
              handleStatusChange(e.target.value as ProjectStatus)
            }
            className="rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {Object.values(ProjectStatus).map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={project.clientAccessEnabled}
              readOnly
              className="rounded"
            />
            Client portal access enabled
          </label>
        </div>
      </section>

      {/* Members */}
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <Users className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">
            Members
          </h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Manage project members and their roles. Member management will be
          available in a future update.
        </p>
      </section>

      {/* Board configuration */}
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <LayoutGrid className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">
            Board Configuration
          </h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Configure kanban board columns and WIP limits. Board configuration
          will be available in a future update.
        </p>
      </section>

      {/* Integrations */}
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <Link2 className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">
            Integrations
          </h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Configure Jira, Redmine, and other external integrations. Integration
          settings will be available in a future update.
        </p>
      </section>
    </div>
  );
}
