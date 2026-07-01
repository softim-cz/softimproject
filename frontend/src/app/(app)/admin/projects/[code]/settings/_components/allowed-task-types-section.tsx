"use client";

/* eslint-disable react-hooks/set-state-in-effect */
import { useState, useEffect } from "react";
import { Tag } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { useProjectAllowedTaskTypes, useSetProjectAllowedTaskTypes } from "@/queries/projects";
import { useTaskTypes } from "@/queries/lookups";

export function AllowedTaskTypesSection({ projectId }: { projectId: string }) {
  const t = useTranslations("ProjectSettings");
  const { data: allowed, isLoading } = useProjectAllowedTaskTypes(projectId);
  const { data: taskTypes } = useTaskTypes();
  const saveMutation = useSetProjectAllowedTaskTypes(projectId);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    if (allowed) {
      setSelectedIds(allowed.overrideTaskTypeIds);
      setDirty(false);
    }
  }, [allowed]);

  if (isLoading || !allowed) return null;

  const toggle = (id: string) => {
    setSelectedIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
    setDirty(true);
  };

  const handleSave = async () => {
    try {
      await saveMutation.mutateAsync(selectedIds);
      toast.success(t("allowedTaskTypesSaved"));
      setDirty(false);
    } catch {
      toast.error(t("allowedTaskTypesSaveFailed"));
    }
  };

  // No override set: ticket creation falls back to the template default (or, if the
  // template is unrestricted too, all active types).
  const inheritsFromTemplate = selectedIds.length === 0;
  const templateRestricts = allowed.templateTaskTypeIds.length > 0;

  const visibleTypes = (taskTypes ?? []).filter((tt) => tt.isActive || selectedIds.includes(tt.id));

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <Tag className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("allowedTaskTypes")}</h2>
      </div>
      <p className="text-sm text-muted-foreground">{t("allowedTaskTypesDescription")}</p>

      {inheritsFromTemplate && (
        <p className="text-xs rounded-lg border border-border bg-muted/30 px-3 py-2 text-muted-foreground">
          {templateRestricts
            ? t("allowedTaskTypesInheritsRestricted", { count: allowed.templateTaskTypeIds.length })
            : t("allowedTaskTypesInheritsAll")}
        </p>
      )}

      {visibleTypes.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("allowedTaskTypesNone")}</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
          {visibleTypes.map((tt) => (
            <label
              key={tt.id}
              className="flex items-center gap-2 text-sm p-2 rounded-lg border border-border hover:bg-muted/30 cursor-pointer"
            >
              <input
                type="checkbox"
                checked={selectedIds.includes(tt.id)}
                onChange={() => toggle(tt.id)}
                className="rounded"
              />
              <span className="font-medium text-card-foreground">{tt.name}</span>
              {!tt.isActive && (
                <span className="text-xs text-muted-foreground">({t("inactiveTaskType")})</span>
              )}
            </label>
          ))}
        </div>
      )}

      <div className="flex justify-end pt-2">
        <button
          onClick={handleSave}
          disabled={!dirty || saveMutation.isPending}
          className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          {saveMutation.isPending ? t("saving") : t("saveAllowedTaskTypes")}
        </button>
      </div>
    </section>
  );
}
