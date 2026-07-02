"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useProjects, useCreateProject } from "@/queries/projects";
import { useProjectTemplates } from "@/queries/lookups";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { FolderKanban, Plus, Pencil, X } from "lucide-react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createProjectSchema, type CreateProjectInput } from "@/schemas/project";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { ProjectStatus } from "@/types";
import type { Project, ProjectTemplate } from "@/types";

function generateCodeFromName(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "";
  if (words.length === 1) {
    return words[0].slice(0, 3).toUpperCase();
  }
  return words
    .slice(0, 6)
    .map((w) => w[0].toUpperCase())
    .join("");
}

const statusColors: Record<ProjectStatus, string> = {
  [ProjectStatus.Active]: "bg-green-100 text-green-700",
  [ProjectStatus.OnHold]: "bg-yellow-100 text-yellow-700",
  [ProjectStatus.Completed]: "bg-blue-100 text-blue-700",
  [ProjectStatus.Archived]: "bg-muted text-muted-foreground",
};

function CreateProjectDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useTranslations("Projects");
  const tCommon = useTranslations("Common");
  const createProject = useCreateProject();
  const { data: projects } = useProjects();
  const { data: templates } = useProjectTemplates();
  const [codeManuallyEdited, setCodeManuallyEdited] = useState(false);

  const {
    control,
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<CreateProjectInput>({
    resolver: zodResolver(createProjectSchema),
  });

  const nameValue = useWatch({ control, name: "name" });
  const templateValue = useWatch({ control, name: "projectTemplateId" });

  useEffect(() => {
    if (!codeManuallyEdited && nameValue) {
      setValue("code", generateCodeFromName(nameValue));
    }
  }, [nameValue, codeManuallyEdited, setValue]);

  // Default na první aktivní šablonu, jakmile se načtou. Šablona je teď
  // povinná, takže form musí mít vždy zvolenou hodnotu.
  useEffect(() => {
    if (!templateValue && templates && templates.length > 0) {
      const firstActive = templates.find((tpl: ProjectTemplate) => tpl.isActive) ?? templates[0];
      setValue("projectTemplateId", firstActive.id);
    }
  }, [templates, templateValue, setValue]);

  const handleClose = () => {
    reset();
    setCodeManuallyEdited(false);
    onClose();
  };

  const onSubmit = async (data: CreateProjectInput) => {
    try {
      const payload = {
        ...data,
        parentProjectId: data.parentProjectId || undefined,
        projectTemplateId: data.projectTemplateId,
        description: data.description || undefined,
        startDate: data.startDate || undefined,
        endDate: data.endDate || undefined,
        budgetHours: data.budgetHours || undefined,
        budgetAmount: data.budgetAmount || undefined,
      };
      await createProject.mutateAsync(payload);
      toast.success(t("createSuccess"));
      reset();
      setCodeManuallyEdited(false);
      onClose();
    } catch {
      toast.error(t("createFailed"));
    }
  };

  if (!open) return null;

  const parentOptions = projects?.filter((p: Project) => p.status === ProjectStatus.Active);

  const activeTemplates = templates?.filter((t: ProjectTemplate) => t.isActive);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={handleClose} />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-lg mx-4 p-6 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-card-foreground">{t("newProject")}</h2>
          <button onClick={handleClose} className="p-1 rounded hover:bg-muted transition-colors">
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground">
              {tCommon("name")}
            </label>
            <p className="text-xs text-muted-foreground mb-1">{t("nameHelp")}</p>
            <input
              {...register("name")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder={t("namePlaceholder")}
            />
            {errors.name && <p className="text-xs text-destructive mt-1">{errors.name.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground">
              {t("code")}
              <span className="text-muted-foreground font-normal ml-1">
                {t("codeAutoGenerated")}
              </span>
            </label>
            <p className="text-xs text-muted-foreground mb-1">{t("codeHelp")}</p>
            <input
              {...register("code", {
                onChange: () => setCodeManuallyEdited(true),
              })}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring uppercase"
              placeholder={t("codePlaceholder")}
              maxLength={6}
            />
            {errors.code && <p className="text-xs text-destructive mt-1">{errors.code.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground">
              {t("parentProject")}
              <span className="text-muted-foreground font-normal ml-1">
                {t("parentProjectOptional")}
              </span>
            </label>
            <p className="text-xs text-muted-foreground mb-1">{t("parentProjectHelp")}</p>
            <select
              {...register("parentProjectId")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">{t("parentProjectNone")}</option>
              {parentOptions?.map((p: Project) => (
                <option key={p.id} value={p.id}>
                  {p.code} - {p.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground">
              {t("template")}
              <span className="text-destructive ml-1">*</span>
            </label>
            <p className="text-xs text-muted-foreground mb-1">{t("templateHelp")}</p>
            <select
              {...register("projectTemplateId")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">{t("templateSelect")}</option>
              {activeTemplates?.map((tpl: ProjectTemplate) => (
                <option key={tpl.id} value={tpl.id}>
                  {tpl.name}
                </option>
              ))}
            </select>
            {errors.projectTemplateId && (
              <p className="text-xs text-destructive mt-1">{errors.projectTemplateId.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground">
              {tCommon("description")}
            </label>
            <p className="text-xs text-muted-foreground mb-1">{t("descriptionHelp")}</p>
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

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground">
                {t("startDate")}
              </label>
              <p className="text-xs text-muted-foreground mb-1">{t("startDateHelp")}</p>
              <input
                {...register("startDate")}
                type="date"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground">
                {t("endDate")}
              </label>
              <p className="text-xs text-muted-foreground mb-1">{t("endDateHelp")}</p>
              <input
                {...register("endDate")}
                type="date"
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground">
                {t("budgetHours")}
              </label>
              <p className="text-xs text-muted-foreground mb-1">{t("budgetHoursHelp")}</p>
              <div className="relative">
                <input
                  {...register("budgetHours", { valueAsNumber: true })}
                  type="number"
                  step="0.5"
                  className="w-full rounded-lg border border-input bg-background px-3 py-2 pr-8 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder="0"
                />
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground pointer-events-none">
                  h
                </span>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground">
                {t("budgetAmount")}
              </label>
              <p className="text-xs text-muted-foreground mb-1">{t("budgetAmountHelp")}</p>
              <div className="relative">
                <input
                  {...register("budgetAmount", { valueAsNumber: true })}
                  type="number"
                  step="100"
                  className="w-full rounded-lg border border-input bg-background px-3 py-2 pr-10 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder="0"
                />
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground pointer-events-none">
                  Kč
                </span>
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <button
              type="button"
              onClick={handleClose}
              className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
            >
              {tCommon("cancel")}
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {isSubmitting ? t("creating") : t("createButton")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function AdminProjectsPage() {
  const t = useTranslations("Projects");
  const tCommon = useTranslations("Common");
  const tPage = useTranslations("AdminProjects");
  const { data: projects, isLoading, error } = useProjects();
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{tPage("title")}</h1>
          <p className="text-sm text-muted-foreground mt-1">{tPage("subtitle")}</p>
        </div>
        <button
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
        >
          <Plus className="h-4 w-4" />
          {t("newProject")}
        </button>
      </div>

      {isLoading && <TableSkeleton rows={6} />}

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          {t("loadFailed")}
        </div>
      )}

      {projects && projects.length === 0 && (
        <EmptyState
          icon={<FolderKanban className="h-12 w-12" />}
          title={t("noProjectsYet")}
          description={t("createFirstProject")}
        />
      )}

      {projects && projects.length > 0 && (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="bg-muted/50">
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                  {tCommon("name")}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                  {t("code")}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                  {tCommon("company")}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                  {t("externalSystem")}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                  {tCommon("status")}
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-16">
                  {tCommon("actions")}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {projects.map((project: Project) => (
                <tr key={project.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm font-medium text-foreground">{project.name}</td>
                  <td className="px-4 py-3 text-sm font-mono text-muted-foreground">
                    {project.code}
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {project.companyName || "—"}
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {project.externalSystem
                      ? `${project.externalSystem}${project.externalProjectId ? ` #${project.externalProjectId}` : ""}`
                      : "—"}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[project.status]}`}
                    >
                      {t(`status.${project.status}` as "status.Active")}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Link
                        href={`/admin/projects/${project.code}/settings`}
                        className="inline-flex items-center gap-1.5 px-2 py-1 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded"
                        title={t("editButton")}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                        {t("editButton")}
                      </Link>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <CreateProjectDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
    </div>
  );
}
