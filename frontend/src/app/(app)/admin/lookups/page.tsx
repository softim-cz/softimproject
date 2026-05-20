"use client";

import { useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import { cn } from "@/lib/utils";
import { localizedName } from "@/lib/localized-name";
import type { Locale } from "@/i18n/config";
import {
  Building2,
  FolderTree,
  CircleDot,
  Tag,
  Shield,
  SlidersHorizontal,
  Copy,
  Plus,
  Pencil,
  Trash2,
  X,
  Check,
  ChevronDown,
  ChevronRight,
  ListTodo,
} from "lucide-react";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import {
  useCompanies,
  useCreateCompany,
  useUpdateCompany,
  useDeleteCompany,
  useProjectTypes,
  useCreateProjectType,
  useUpdateProjectType,
  useDeleteProjectType,
  useProjectStates,
  useCreateProjectState,
  useUpdateProjectState,
  useDeleteProjectState,
  useTaskTypes,
  useCreateTaskType,
  useUpdateTaskType,
  useDeleteTaskType,
  useTaskStates,
  useCreateTaskState,
  useUpdateTaskState,
  useDeleteTaskState,
  useApplicationRoles,
  useCreateApplicationRole,
  useUpdateApplicationRole,
  useDeleteApplicationRole,
  useCustomFieldDefinitions,
  useCreateCustomFieldDefinition,
  useUpdateCustomFieldDefinition,
  useDeleteCustomFieldDefinition,
  useProjectTemplates,
  useCreateProjectTemplate,
  useUpdateProjectTemplate,
  useDeleteProjectTemplate,
  useDuplicateProjectTemplate,
  useCreateTicketPriority,
  useUpdateTicketPriority,
  useDeleteTicketPriority,
} from "@/queries/lookups";
import type {
  Company,
  ProjectType,
  ProjectState,
  TaskType,
  TaskState,
  ApplicationRoleEntity,
  CustomFieldDefinition,
  ProjectTemplate,
  TicketPriorityLookup,
} from "@/types";
import { CustomFieldType } from "@/types";

const tabConfig = [
  { key: "companies", labelKey: "tabCompanies", icon: Building2 },
  { key: "project-types", labelKey: "tabProjectTypes", icon: FolderTree },
  { key: "project-states", labelKey: "tabProjectStates", icon: CircleDot },
  { key: "task-types", labelKey: "tabTaskTypes", icon: Tag },
  { key: "task-states", labelKey: "tabTaskStatesGlobal", icon: ListTodo },
  { key: "application-roles", labelKey: "tabAppRoles", icon: Shield },
  { key: "custom-fields", labelKey: "tabCustomFields", icon: SlidersHorizontal },
  { key: "templates", labelKey: "tabTemplates", icon: Copy },
] as const;

type TabKey = (typeof tabConfig)[number]["key"];

// === Generic inline-edit table for simple lookups ===

function CompaniesTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const { data, isLoading } = useCompanies();
  const createMutation = useCreateCompany();
  const updateMutation = useUpdateCompany();
  const deleteMutation = useDeleteCompany();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", description: "" });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<Building2 className="h-10 w-10" />} title={t("common.empty")} />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", description: "" });
  };

  const startEdit = (item: Company) => {
    setEditId(item.id);
    setAdding(false);
    setForm({ name: item.name, description: item.description || "" });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updateMutation.mutateAsync({
        ...existing,
        name: form.name,
        description: form.description || undefined,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
        description: form.description || undefined,
      });
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("company.newCompany")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.description")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.isActive")}
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-4 py-2">
                  <input
                    value={form.name}
                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("common.name")}
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("common.description")}
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">{tCommon("yes")}</td>
                <td className="px-4 py-2 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-4 w-4" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </td>
              </tr>
            )}
            {data.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-4 py-2">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      value={form.description}
                      onChange={(e) => setForm({ ...form, description: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">
                    {item.isActive ? tCommon("yes") : tCommon("no")}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm font-medium">{item.name}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {item.description || "—"}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">{tCommon("yes")}</span>
                    ) : (
                      <span className="text-muted-foreground">{tCommon("no")}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => deleteMutation.mutate(item.id)}
                      className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function ProjectTypesTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useProjectTypes();
  const createMutation = useCreateProjectType();
  const updateMutation = useUpdateProjectType();
  const deleteMutation = useDeleteProjectType();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    nameCs: "",
    nameEn: "",
    description: "",
    sortOrder: 0,
  });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<FolderTree className="h-10 w-10" />} title={t("common.empty")} />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      nameCs: "",
      nameEn: "",
      description: "",
      sortOrder: (data.length + 1) * 10,
    });
  };
  const startEdit = (item: ProjectType) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      description: item.description || "",
      sortOrder: item.sortOrder,
    });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updateMutation.mutateAsync({
        ...existing,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
      });
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("projectType.newType")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.description")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.sortOrder")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.isActive")}
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-4 py-2">
                  <div className="flex gap-1.5">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.name")}
                      autoFocus
                    />
                    <input
                      value={form.nameCs}
                      onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.nameCs")}
                    />
                    <input
                      value={form.nameEn}
                      onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.nameEn")}
                    />
                  </div>
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("common.description")}
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    type="number"
                    value={form.sortOrder}
                    onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">{tCommon("yes")}</td>
                <td className="px-4 py-2 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-4 w-4" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </td>
              </tr>
            )}
            {data.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-4 py-2">
                    <div className="flex gap-1.5">
                      <input
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.name")}
                      />
                      <input
                        value={form.nameCs}
                        onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.nameCs")}
                      />
                      <input
                        value={form.nameEn}
                        onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.nameEn")}
                      />
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <input
                      value={form.description}
                      onChange={(e) => setForm({ ...form, description: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">
                    {item.isActive ? tCommon("yes") : tCommon("no")}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm font-medium">{localizedName(item, locale)}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {item.description || "—"}
                  </td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">{tCommon("yes")}</span>
                    ) : (
                      <span className="text-muted-foreground">{tCommon("no")}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => deleteMutation.mutate(item.id)}
                      className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function StateTable() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useProjectStates();
  const createPS = useCreateProjectState();
  const updatePS = useUpdateProjectState();
  const deletePS = useDeleteProjectState();

  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    nameCs: "",
    nameEn: "",
    color: "#3b82f6",
    sortOrder: 0,
    isDefault: false,
  });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<CircleDot className="h-10 w-10" />} title={t("common.empty")} />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      nameCs: "",
      nameEn: "",
      color: "#3b82f6",
      sortOrder: (data.length + 1) * 10,
      isDefault: false,
    });
  };
  const startEdit = (item: ProjectState) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isDefault: item.isDefault,
    });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updatePS.mutateAsync({
        ...existing,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      } as ProjectState);
      setEditId(null);
    } else {
      await createPS.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      });
      setAdding(false);
    }
  };

  const handleDelete = (id: string) => {
    deletePS.mutate(id);
  };
  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("projectState.newState")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.color")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.sortOrder")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.isDefault")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.isActive")}
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-4 py-2">
                  <input
                    type="color"
                    value={form.color}
                    onChange={(e) => setForm({ ...form, color: e.target.value })}
                    className="w-8 h-8 rounded cursor-pointer"
                  />
                </td>
                <td className="px-4 py-2">
                  <div className="flex gap-1.5">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.name")}
                      autoFocus
                    />
                    <input
                      value={form.nameCs}
                      onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.nameCs")}
                    />
                    <input
                      value={form.nameEn}
                      onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.nameEn")}
                    />
                  </div>
                </td>
                <td className="px-4 py-2">
                  <input
                    type="number"
                    value={form.sortOrder}
                    onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    type="checkbox"
                    checked={form.isDefault}
                    onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">{tCommon("yes")}</td>
                <td className="px-4 py-2 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-4 w-4" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </td>
              </tr>
            )}
            {data.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-4 py-2">
                    <input
                      type="color"
                      value={form.color}
                      onChange={(e) => setForm({ ...form, color: e.target.value })}
                      className="w-8 h-8 rounded cursor-pointer"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex gap-1.5">
                      <input
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.name")}
                      />
                      <input
                        value={form.nameCs}
                        onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.nameCs")}
                      />
                      <input
                        value={form.nameEn}
                        onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.nameEn")}
                      />
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      type="checkbox"
                      checked={form.isDefault}
                      onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">
                    {item.isActive ? tCommon("yes") : tCommon("no")}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3">
                    <div
                      className="w-6 h-6 rounded-full border"
                      style={{ backgroundColor: item.color }}
                    />
                  </td>
                  <td className="px-4 py-3 text-sm font-medium">{localizedName(item, locale)}</td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isDefault ? <Check className="h-4 w-4 text-green-600" /> : null}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">{tCommon("yes")}</span>
                    ) : (
                      <span className="text-muted-foreground">{tCommon("no")}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => handleDelete(item.id)}
                      className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function TaskTypesTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useTaskTypes();
  const createMutation = useCreateTaskType();
  const updateMutation = useUpdateTaskType();
  const deleteMutation = useDeleteTaskType();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    nameCs: "",
    nameEn: "",
    icon: "",
    sortOrder: 0,
  });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data) return <EmptyState icon={<Tag className="h-10 w-10" />} title={t("common.empty")} />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", nameCs: "", nameEn: "", icon: "", sortOrder: (data.length + 1) * 10 });
  };
  const startEdit = (item: TaskType) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      icon: item.icon || "",
      sortOrder: item.sortOrder,
    });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updateMutation.mutateAsync({
        ...existing,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        icon: form.icon || undefined,
        sortOrder: form.sortOrder,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        icon: form.icon || undefined,
        sortOrder: form.sortOrder,
      });
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("taskType.newType")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.icon")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.sortOrder")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.isActive")}
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-4 py-2">
                  <div className="flex gap-1.5">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.name")}
                      autoFocus
                    />
                    <input
                      value={form.nameCs}
                      onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.nameCs")}
                    />
                    <input
                      value={form.nameEn}
                      onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      placeholder={t("common.nameEn")}
                    />
                  </div>
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.icon}
                    onChange={(e) => setForm({ ...form, icon: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("common.iconPlaceholder")}
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    type="number"
                    value={form.sortOrder}
                    onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">{tCommon("yes")}</td>
                <td className="px-4 py-2 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-4 w-4" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </td>
              </tr>
            )}
            {data.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-4 py-2">
                    <div className="flex gap-1.5">
                      <input
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.name")}
                      />
                      <input
                        value={form.nameCs}
                        onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.nameCs")}
                      />
                      <input
                        value={form.nameEn}
                        onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                        className="w-full px-2 py-1 text-sm border rounded"
                        placeholder={t("common.nameEn")}
                      />
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <input
                      value={form.icon}
                      onChange={(e) => setForm({ ...form, icon: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">
                    {item.isActive ? tCommon("yes") : tCommon("no")}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm font-medium">{localizedName(item, locale)}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{item.icon || "—"}</td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">{tCommon("yes")}</span>
                    ) : (
                      <span className="text-muted-foreground">{tCommon("no")}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => deleteMutation.mutate(item.id)}
                      className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function ApplicationRolesTab() {
  const t = useTranslations("Lookups");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useApplicationRoles();
  const createMutation = useCreateApplicationRole();
  const updateMutation = useUpdateApplicationRole();
  const deleteMutation = useDeleteApplicationRole();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const emptyPerms = {
    projectsCreate: false,
    projectsRead: true,
    projectsUpdate: false,
    projectsDelete: false,
    timeTrackingCreate: false,
    timeTrackingRead: true,
    timeTrackingUpdate: false,
    timeTrackingDelete: false,
    reportsCreate: false,
    reportsRead: true,
    reportsUpdate: false,
    reportsDelete: false,
  };
  const [form, setForm] = useState<
    {
      name: string;
      nameCs: string;
      nameEn: string;
      description: string;
      sortOrder: number;
    } & typeof emptyPerms
  >({ name: "", nameCs: "", nameEn: "", description: "", sortOrder: 0, ...emptyPerms });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<Shield className="h-10 w-10" />} title={t("common.empty")} />;

  const areas = ["projects", "timeTracking", "reports"] as const;
  const ops = ["Create", "Read", "Update", "Delete"] as const;
  const opLabelKey = {
    Create: "appRole.create",
    Read: "appRole.read",
    Update: "appRole.update",
    Delete: "appRole.delete",
  } as const;
  const areaLabelKey = {
    projects: "appRole.projects",
    timeTracking: "appRole.timeTracking",
    reports: "appRole.reports",
  } as const;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      nameCs: "",
      nameEn: "",
      description: "",
      sortOrder: (data.length + 1) * 10,
      ...emptyPerms,
    });
  };
  const startEdit = (item: ApplicationRoleEntity) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      description: item.description || "",
      sortOrder: item.sortOrder,
      projectsCreate: item.projectsCreate,
      projectsRead: item.projectsRead,
      projectsUpdate: item.projectsUpdate,
      projectsDelete: item.projectsDelete,
      timeTrackingCreate: item.timeTrackingCreate,
      timeTrackingRead: item.timeTrackingRead,
      timeTrackingUpdate: item.timeTrackingUpdate,
      timeTrackingDelete: item.timeTrackingDelete,
      reportsCreate: item.reportsCreate,
      reportsRead: item.reportsRead,
      reportsUpdate: item.reportsUpdate,
      reportsDelete: item.reportsDelete,
    });
  };

  const save = async () => {
    const body = {
      name: form.name,
      nameCs: form.nameCs || undefined,
      nameEn: form.nameEn || undefined,
      description: form.description || undefined,
      sortOrder: form.sortOrder,
      projectsCreate: form.projectsCreate,
      projectsRead: form.projectsRead,
      projectsUpdate: form.projectsUpdate,
      projectsDelete: form.projectsDelete,
      timeTrackingCreate: form.timeTrackingCreate,
      timeTrackingRead: form.timeTrackingRead,
      timeTrackingUpdate: form.timeTrackingUpdate,
      timeTrackingDelete: form.timeTrackingDelete,
      reportsCreate: form.reportsCreate,
      reportsRead: form.reportsRead,
      reportsUpdate: form.reportsUpdate,
      reportsDelete: form.reportsDelete,
    };
    if (editId) {
      await updateMutation.mutateAsync({ id: editId, ...body } as ApplicationRoleEntity);
      setEditId(null);
    } else {
      await createMutation.mutateAsync(body as Omit<ApplicationRoleEntity, "id">);
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };
  const isEditing = adding || editId !== null;

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("appRole.newRole")}
        </button>
      </div>

      {isEditing && (
        <div className="mb-4 p-4 rounded-lg border border-border bg-card space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.name")}
              </label>
              <input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
                autoFocus
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.nameCs")}
              </label>
              <input
                value={form.nameCs}
                onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.nameEn")}
              </label>
              <input
                value={form.nameEn}
                onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.description")}
              </label>
              <input
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.sortOrder")}
              </label>
              <input
                type="number"
                value={form.sortOrder}
                onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
              />
            </div>
          </div>

          <div>
            <p className="text-xs font-medium text-muted-foreground mb-2">
              {t("appRole.permissions")}
            </p>
            <div className="rounded-lg border border-border overflow-hidden">
              <table className="w-full">
                <thead>
                  <tr className="bg-muted/50">
                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                      {t("appRole.permissions")}
                    </th>
                    {ops.map((op) => (
                      <th
                        key={op}
                        className="px-3 py-2 text-center text-xs font-medium text-muted-foreground uppercase"
                      >
                        {t(opLabelKey[op])}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {areas.map((area) => (
                    <tr key={area}>
                      <td className="px-3 py-2 text-sm font-medium">{t(areaLabelKey[area])}</td>
                      {ops.map((op) => {
                        const key = `${area}${op}` as keyof typeof emptyPerms;
                        return (
                          <td key={op} className="px-3 py-2 text-center">
                            <input
                              type="checkbox"
                              checked={form[key]}
                              onChange={(e) => setForm({ ...form, [key]: e.target.checked })}
                            />
                          </td>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="flex justify-end gap-2">
            <button
              onClick={cancel}
              className="px-3 py-1.5 text-sm font-medium text-muted-foreground hover:bg-muted/50 rounded-lg"
            >
              {t("common.cancel")}
            </button>
            <button
              onClick={save}
              disabled={!form.name}
              className="px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90 disabled:opacity-30"
            >
              {t("common.save")}
            </button>
          </div>
        </div>
      )}

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.description")}
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">
                {t("appRole.projects")}
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">
                {t("appRole.timeTracking")}
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">
                {t("appRole.reports")}
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {data.map((item) => (
              <tr key={item.id} className="hover:bg-muted/30">
                <td className="px-4 py-3 text-sm font-medium">{localizedName(item, locale)}</td>
                <td className="px-4 py-3 text-sm text-muted-foreground">
                  {item.description || "—"}
                </td>
                <td className="px-4 py-3 text-xs text-center text-muted-foreground">
                  {[
                    item.projectsCreate && "C",
                    item.projectsRead && "R",
                    item.projectsUpdate && "U",
                    item.projectsDelete && "D",
                  ]
                    .filter(Boolean)
                    .join("")}
                </td>
                <td className="px-4 py-3 text-xs text-center text-muted-foreground">
                  {[
                    item.timeTrackingCreate && "C",
                    item.timeTrackingRead && "R",
                    item.timeTrackingUpdate && "U",
                    item.timeTrackingDelete && "D",
                  ]
                    .filter(Boolean)
                    .join("")}
                </td>
                <td className="px-4 py-3 text-xs text-center text-muted-foreground">
                  {[
                    item.reportsCreate && "C",
                    item.reportsRead && "R",
                    item.reportsUpdate && "U",
                    item.reportsDelete && "D",
                  ]
                    .filter(Boolean)
                    .join("")}
                </td>
                <td className="px-4 py-3 text-right">
                  <button
                    onClick={() => startEdit(item)}
                    className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </button>
                  <button
                    onClick={() => deleteMutation.mutate(item.id)}
                    className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function CustomFieldDefinitionsTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const { data, isLoading } = useCustomFieldDefinitions();
  const createMutation = useCreateCustomFieldDefinition();
  const updateMutation = useUpdateCustomFieldDefinition();
  const deleteMutation = useDeleteCustomFieldDefinition();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    description: "",
    fieldType: "Text",
    isRequired: false,
    options: "",
    sortOrder: 0,
  });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return (
      <EmptyState icon={<SlidersHorizontal className="h-10 w-10" />} title={t("common.empty")} />
    );

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      description: "",
      fieldType: "Text",
      isRequired: false,
      options: "",
      sortOrder: (data.length + 1) * 10,
    });
  };
  const startEdit = (item: CustomFieldDefinition) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      description: item.description || "",
      fieldType: item.fieldType,
      isRequired: item.isRequired,
      options: item.options || "",
      sortOrder: item.sortOrder,
    });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updateMutation.mutateAsync({
        ...existing,
        name: form.name,
        description: form.description || undefined,
        fieldType: form.fieldType,
        isRequired: form.isRequired,
        options: form.options || undefined,
        sortOrder: form.sortOrder,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
        description: form.description || undefined,
        fieldType: form.fieldType,
        isRequired: form.isRequired,
        options: form.options || undefined,
        sortOrder: form.sortOrder,
      });
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  const fieldTypeOptions = Object.values(CustomFieldType);

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("customField.newField")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.description")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-24">
                {t("customField.fieldType")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("customField.required")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("customField.options")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.sortOrder")}
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.isActive")}
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-4 py-2">
                  <input
                    value={form.name}
                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("common.name")}
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("common.description")}
                  />
                </td>
                <td className="px-4 py-2">
                  <select
                    value={form.fieldType}
                    onChange={(e) => setForm({ ...form, fieldType: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                  >
                    {fieldTypeOptions.map((ft) => (
                      <option key={ft} value={ft}>
                        {t(`customField.types.${ft}` as "customField.types.Text")}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="px-4 py-2">
                  <input
                    type="checkbox"
                    checked={form.isRequired}
                    onChange={(e) => setForm({ ...form, isRequired: e.target.checked })}
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.options}
                    onChange={(e) => setForm({ ...form, options: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder={t("customField.optionsPlaceholder")}
                    disabled={form.fieldType !== "Select"}
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    type="number"
                    value={form.sortOrder}
                    onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">{tCommon("yes")}</td>
                <td className="px-4 py-2 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-4 w-4" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </td>
              </tr>
            )}
            {data.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-4 py-2">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      value={form.description}
                      onChange={(e) => setForm({ ...form, description: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <select
                      value={form.fieldType}
                      onChange={(e) => setForm({ ...form, fieldType: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    >
                      {fieldTypeOptions.map((ft) => (
                        <option key={ft} value={ft}>
                          {t(`customField.types.${ft}` as "customField.types.Text")}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-4 py-2">
                    <input
                      type="checkbox"
                      checked={form.isRequired}
                      onChange={(e) => setForm({ ...form, isRequired: e.target.checked })}
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      value={form.options}
                      onChange={(e) => setForm({ ...form, options: e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                      disabled={form.fieldType !== "Select"}
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">
                    {item.isActive ? tCommon("yes") : tCommon("no")}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm font-medium">{item.name}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {item.description || "—"}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {t(`customField.types.${item.fieldType}` as "customField.types.Text")}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {item.isRequired ? <Check className="h-4 w-4 text-green-600" /> : null}
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {item.fieldType === "Select" ? item.options || "—" : "—"}
                  </td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">{tCommon("yes")}</span>
                    ) : (
                      <span className="text-muted-foreground">{tCommon("no")}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => deleteMutation.mutate(item.id)}
                      className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// === Inline sub-table for task states / ticket priorities within a template ===

function TemplateTaskStatesSection({
  templateId,
  taskStates,
}: {
  templateId: string;
  taskStates: TaskState[];
}) {
  const t = useTranslations("Lookups");
  const locale = useLocale() as Locale;
  const createTS = useCreateTaskState();
  const updateTS = useUpdateTaskState();
  const deleteTS = useDeleteTaskState();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    nameCs: "",
    nameEn: "",
    color: "#3b82f6",
    sortOrder: 0,
    isDefault: false,
    isClosedState: false,
  });

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      nameCs: "",
      nameEn: "",
      color: "#3b82f6",
      sortOrder: (taskStates.length + 1) * 10,
      isDefault: false,
      isClosedState: false,
    });
  };
  const startEdit = (item: TaskState) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isDefault: item.isDefault,
      isClosedState: item.isClosedState,
    });
  };

  const save = async () => {
    if (editId) {
      const existing = taskStates.find((c) => c.id === editId)!;
      await updateTS.mutateAsync({
        ...existing,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        isClosedState: form.isClosedState,
      });
      setEditId(null);
    } else {
      await createTS.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        isClosedState: form.isClosedState,
        projectTemplateId: templateId,
      });
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <p className="text-xs font-medium text-muted-foreground uppercase">
          {t("template.taskStates")}
        </p>
        <button
          onClick={startAdd}
          className="flex items-center gap-1 px-2 py-1 text-xs font-medium text-accent-orange hover:bg-accent-orange/10 rounded"
        >
          <Plus className="h-3 w-3" /> {t("template.addTaskState")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.color")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.sortOrder")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.isDefault")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.isClosed")}
              </th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-3 py-1.5">
                  <input
                    type="color"
                    value={form.color}
                    onChange={(e) => setForm({ ...form, color: e.target.value })}
                    className="w-7 h-7 rounded cursor-pointer"
                  />
                </td>
                <td className="px-3 py-1.5">
                  <div className="flex gap-1.5">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                      placeholder={t("common.name")}
                      autoFocus
                    />
                    <input
                      value={form.nameCs}
                      onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                      placeholder={t("common.nameCs")}
                    />
                    <input
                      value={form.nameEn}
                      onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                      placeholder={t("common.nameEn")}
                    />
                  </div>
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="number"
                    value={form.sortOrder}
                    onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                    className="w-full px-2 py-1 text-xs border rounded"
                  />
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="checkbox"
                    checked={form.isDefault}
                    onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                  />
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="checkbox"
                    checked={form.isClosedState}
                    onChange={(e) => setForm({ ...form, isClosedState: e.target.checked })}
                  />
                </td>
                <td className="px-3 py-1.5 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-3.5 w-3.5" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                </td>
              </tr>
            )}
            {taskStates.length === 0 && !adding && (
              <tr>
                <td colSpan={6} className="px-3 py-3 text-xs text-center text-muted-foreground">
                  {t("common.empty")}
                </td>
              </tr>
            )}
            {taskStates.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-3 py-1.5">
                    <input
                      type="color"
                      value={form.color}
                      onChange={(e) => setForm({ ...form, color: e.target.value })}
                      className="w-7 h-7 rounded cursor-pointer"
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <div className="flex gap-1.5">
                      <input
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.name")}
                      />
                      <input
                        value={form.nameCs}
                        onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.nameCs")}
                      />
                      <input
                        value={form.nameEn}
                        onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.nameEn")}
                      />
                    </div>
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="checkbox"
                      checked={form.isDefault}
                      onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="checkbox"
                      checked={form.isClosedState}
                      onChange={(e) => setForm({ ...form, isClosedState: e.target.checked })}
                    />
                  </td>
                  <td className="px-3 py-1.5 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-3 py-2">
                    <div
                      className="w-5 h-5 rounded-full border"
                      style={{ backgroundColor: item.color }}
                    />
                  </td>
                  <td className="px-3 py-2 text-xs font-medium">{localizedName(item, locale)}</td>
                  <td className="px-3 py-2 text-xs">{item.sortOrder}</td>
                  <td className="px-3 py-2 text-xs">
                    {item.isDefault ? <Check className="h-3.5 w-3.5 text-green-600" /> : null}
                  </td>
                  <td className="px-3 py-2 text-xs">
                    {item.isClosedState ? <Check className="h-3.5 w-3.5 text-red-500" /> : null}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-0.5 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3 w-3" />
                    </button>
                    <button
                      onClick={() => deleteTS.mutate(item.id)}
                      className="p-0.5 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function TemplateTicketPrioritiesSection({
  templateId,
  ticketPriorities,
}: {
  templateId: string;
  ticketPriorities: TicketPriorityLookup[];
}) {
  const t = useTranslations("Lookups");
  const locale = useLocale() as Locale;
  const createTP = useCreateTicketPriority();
  const updateTP = useUpdateTicketPriority();
  const deleteTP = useDeleteTicketPriority();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    nameCs: "",
    nameEn: "",
    color: "#f59e0b",
    sortOrder: 0,
    isDefault: false,
  });

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      nameCs: "",
      nameEn: "",
      color: "#f59e0b",
      sortOrder: (ticketPriorities.length + 1) * 10,
      isDefault: false,
    });
  };
  const startEdit = (item: TicketPriorityLookup) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isDefault: item.isDefault,
    });
  };

  const save = async () => {
    if (editId) {
      const existing = ticketPriorities.find((c) => c.id === editId)!;
      await updateTP.mutateAsync({
        ...existing,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      });
      setEditId(null);
    } else {
      await createTP.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        projectTemplateId: templateId,
      });
      setAdding(false);
    }
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <p className="text-xs font-medium text-muted-foreground uppercase">
          {t("template.ticketPriorities")}
        </p>
        <button
          onClick={startAdd}
          className="flex items-center gap-1 px-2 py-1 text-xs font-medium text-accent-orange hover:bg-accent-orange/10 rounded"
        >
          <Plus className="h-3 w-3" /> {t("template.addPriority")}
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.color")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.sortOrder")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.isDefault")}
              </th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                <td className="px-3 py-1.5">
                  <input
                    type="color"
                    value={form.color}
                    onChange={(e) => setForm({ ...form, color: e.target.value })}
                    className="w-7 h-7 rounded cursor-pointer"
                  />
                </td>
                <td className="px-3 py-1.5">
                  <div className="flex gap-1.5">
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                      placeholder={t("common.name")}
                      autoFocus
                    />
                    <input
                      value={form.nameCs}
                      onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                      placeholder={t("common.nameCs")}
                    />
                    <input
                      value={form.nameEn}
                      onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                      placeholder={t("common.nameEn")}
                    />
                  </div>
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="number"
                    value={form.sortOrder}
                    onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                    className="w-full px-2 py-1 text-xs border rounded"
                  />
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="checkbox"
                    checked={form.isDefault}
                    onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                  />
                </td>
                <td className="px-3 py-1.5 text-right">
                  <button
                    onClick={save}
                    disabled={!form.name}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className="h-3.5 w-3.5" />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                </td>
              </tr>
            )}
            {ticketPriorities.length === 0 && !adding && (
              <tr>
                <td colSpan={5} className="px-3 py-3 text-xs text-center text-muted-foreground">
                  {t("common.empty")}
                </td>
              </tr>
            )}
            {ticketPriorities.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-3 py-1.5">
                    <input
                      type="color"
                      value={form.color}
                      onChange={(e) => setForm({ ...form, color: e.target.value })}
                      className="w-7 h-7 rounded cursor-pointer"
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <div className="flex gap-1.5">
                      <input
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.name")}
                      />
                      <input
                        value={form.nameCs}
                        onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.nameCs")}
                      />
                      <input
                        value={form.nameEn}
                        onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.nameEn")}
                      />
                    </div>
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="checkbox"
                      checked={form.isDefault}
                      onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                    />
                  </td>
                  <td className="px-3 py-1.5 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-3 py-2">
                    <div
                      className="w-5 h-5 rounded-full border"
                      style={{ backgroundColor: item.color }}
                    />
                  </td>
                  <td className="px-3 py-2 text-xs font-medium">{localizedName(item, locale)}</td>
                  <td className="px-3 py-2 text-xs">{item.sortOrder}</td>
                  <td className="px-3 py-2 text-xs">
                    {item.isDefault ? <Check className="h-3.5 w-3.5 text-green-600" /> : null}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-0.5 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3 w-3" />
                    </button>
                    <button
                      onClick={() => deleteTP.mutate(item.id)}
                      className="p-0.5 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function TaskStatesGlobalTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useTaskStates();
  const { data: templates } = useProjectTemplates();
  const updateTS = useUpdateTaskState();
  const deleteTS = useDeleteTaskState();
  const [editId, setEditId] = useState<string | null>(null);
  const [templateFilter, setTemplateFilter] = useState<string>("");
  const [form, setForm] = useState({
    name: "",
    nameCs: "",
    nameEn: "",
    color: "#3b82f6",
    sortOrder: 0,
    isActive: true,
    isDefault: false,
    isClosedState: false,
  });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<ListTodo className="h-10 w-10" />} title={t("common.empty")} />;

  const templateNameById = (id: string) => templates?.find((tpl) => tpl.id === id)?.name || "—";

  const filtered = templateFilter
    ? data.filter((ts) => ts.projectTemplateId === templateFilter)
    : data;

  const startEdit = (item: TaskState) => {
    setEditId(item.id);
    setForm({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isActive: item.isActive,
      isDefault: item.isDefault,
      isClosedState: item.isClosedState,
    });
  };

  const save = async () => {
    if (!editId) return;
    const existing = data.find((c) => c.id === editId)!;
    await updateTS.mutateAsync({
      ...existing,
      name: form.name,
      nameCs: form.nameCs || undefined,
      nameEn: form.nameEn || undefined,
      color: form.color,
      sortOrder: form.sortOrder,
      isActive: form.isActive,
      isDefault: form.isDefault,
      isClosedState: form.isClosedState,
    });
    setEditId(null);
  };

  const cancel = () => setEditId(null);

  return (
    <div>
      <div className="mb-3">
        <p className="text-sm text-muted-foreground">{t("taskStatesGlobal.subtitle")}</p>
      </div>
      <div className="flex items-center gap-2 mb-3">
        <label className="text-xs font-medium text-muted-foreground">
          {t("taskStatesGlobal.templateColumn")}:
        </label>
        <select
          value={templateFilter}
          onChange={(e) => setTemplateFilter(e.target.value)}
          className="px-2 py-1 text-sm border rounded"
        >
          <option value="">{tCommon("all")}</option>
          {templates?.map((tpl) => (
            <option key={tpl.id} value={tpl.id}>
              {tpl.name}
            </option>
          ))}
        </select>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("taskStatesGlobal.templateColumn")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.color")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                {t("common.name")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.sortOrder")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.isActive")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.isDefault")}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                {t("common.isClosed")}
              </th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase w-20">
                {t("common.actions")}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {filtered.length === 0 && (
              <tr>
                <td colSpan={8} className="px-3 py-3 text-xs text-center text-muted-foreground">
                  {t("common.empty")}
                </td>
              </tr>
            )}
            {filtered.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  <td className="px-3 py-1.5 text-xs text-muted-foreground">
                    {templateNameById(item.projectTemplateId)}
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="color"
                      value={form.color}
                      onChange={(e) => setForm({ ...form, color: e.target.value })}
                      className="w-7 h-7 rounded cursor-pointer"
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <div className="flex gap-1.5">
                      <input
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.name")}
                      />
                      <input
                        value={form.nameCs}
                        onChange={(e) => setForm({ ...form, nameCs: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.nameCs")}
                      />
                      <input
                        value={form.nameEn}
                        onChange={(e) => setForm({ ...form, nameEn: e.target.value })}
                        className="w-full px-2 py-1 text-xs border rounded"
                        placeholder={t("common.nameEn")}
                      />
                    </div>
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="checkbox"
                      checked={form.isActive}
                      onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="checkbox"
                      checked={form.isDefault}
                      onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                    />
                  </td>
                  <td className="px-3 py-1.5">
                    <input
                      type="checkbox"
                      checked={form.isClosedState}
                      onChange={(e) => setForm({ ...form, isClosedState: e.target.checked })}
                    />
                  </td>
                  <td className="px-3 py-1.5 text-right">
                    <button
                      onClick={save}
                      disabled={!form.name}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  <td className="px-3 py-2 text-xs text-muted-foreground">
                    {templateNameById(item.projectTemplateId)}
                  </td>
                  <td className="px-3 py-2">
                    <div
                      className="w-5 h-5 rounded-full border"
                      style={{ backgroundColor: item.color }}
                    />
                  </td>
                  <td className="px-3 py-2 text-xs font-medium">{localizedName(item, locale)}</td>
                  <td className="px-3 py-2 text-xs">{item.sortOrder}</td>
                  <td className="px-3 py-2 text-xs">
                    {item.isActive ? (
                      <Check className="h-3.5 w-3.5 text-green-600" />
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-xs">
                    {item.isDefault ? <Check className="h-3.5 w-3.5 text-green-600" /> : null}
                  </td>
                  <td className="px-3 py-2 text-xs">
                    {item.isClosedState ? <Check className="h-3.5 w-3.5 text-red-500" /> : null}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <button
                      onClick={() => startEdit(item)}
                      className="p-0.5 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                    >
                      <Pencil className="h-3 w-3" />
                    </button>
                    <button
                      onClick={() => deleteTS.mutate(item.id)}
                      className="p-0.5 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function ProjectTemplatesTab() {
  const t = useTranslations("Lookups");
  const { data, isLoading } = useProjectTemplates();
  const { data: fieldDefs } = useCustomFieldDefinitions();
  const createMutation = useCreateProjectTemplate();
  const updateMutation = useUpdateProjectTemplate();
  const deleteMutation = useDeleteProjectTemplate();
  const duplicateMutation = useDuplicateProjectTemplate();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    description: "",
    isActive: true,
    selectedFieldIds: [] as string[],
  });
  const [duplicateName, setDuplicateName] = useState("");
  const [duplicatingId, setDuplicatingId] = useState<string | null>(null);

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data) return <EmptyState icon={<Copy className="h-10 w-10" />} title={t("common.empty")} />;

  const activeFields = fieldDefs?.filter((f) => f.isActive) || [];

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", description: "", isActive: true, selectedFieldIds: [] });
  };
  const startEdit = (item: ProjectTemplate) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      description: item.description || "",
      isActive: item.isActive,
      selectedFieldIds: item.fields.map((f) => f.customFieldDefinitionId),
    });
  };

  const toggleField = (id: string) => {
    setForm((prev) => ({
      ...prev,
      selectedFieldIds: prev.selectedFieldIds.includes(id)
        ? prev.selectedFieldIds.filter((fid) => fid !== id)
        : [...prev.selectedFieldIds, id],
    }));
  };

  const save = async () => {
    if (editId) {
      await updateMutation.mutateAsync({
        id: editId,
        name: form.name,
        description: form.description || undefined,
        isActive: form.isActive,
        customFieldDefinitionIds: form.selectedFieldIds,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
        description: form.description || undefined,
        customFieldDefinitionIds: form.selectedFieldIds,
      });
      setAdding(false);
    }
  };

  const handleDuplicate = async (id: string) => {
    if (!duplicateName.trim()) return;
    await duplicateMutation.mutateAsync({ id, newName: duplicateName.trim() });
    setDuplicatingId(null);
    setDuplicateName("");
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };
  const isEditing = adding || editId !== null;

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button
          onClick={startAdd}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
        >
          <Plus className="h-4 w-4" /> {t("template.newTemplate")}
        </button>
      </div>

      {isEditing && (
        <div className="mb-4 p-4 rounded-lg border border-border bg-card space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.name")}
              </label>
              <input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
                autoFocus
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                {t("common.description")}
              </label>
              <input
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
              />
            </div>
          </div>

          {activeFields.length > 0 && (
            <div>
              <p className="text-xs font-medium text-muted-foreground mb-2">
                {t("customField.title")}
              </p>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                {activeFields.map((field) => (
                  <label
                    key={field.id}
                    className="flex items-center gap-2 text-sm p-2 rounded-lg border border-border hover:bg-muted/30 cursor-pointer"
                  >
                    <input
                      type="checkbox"
                      checked={form.selectedFieldIds.includes(field.id)}
                      onChange={() => toggleField(field.id)}
                      className="rounded"
                    />
                    <span className="font-medium">{field.name}</span>
                    <span className="text-xs text-muted-foreground">({field.fieldType})</span>
                  </label>
                ))}
              </div>
            </div>
          )}

          <div className="flex justify-end gap-2">
            <button
              onClick={cancel}
              className="px-3 py-1.5 text-sm font-medium text-muted-foreground hover:bg-muted/50 rounded-lg"
            >
              {t("common.cancel")}
            </button>
            <button
              onClick={save}
              disabled={!form.name}
              className="px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90 disabled:opacity-30"
            >
              {t("common.save")}
            </button>
          </div>
        </div>
      )}

      <div className="space-y-2">
        {data.map((item) => (
          <div key={item.id} className="rounded-lg border border-border bg-card">
            {/* Template header row */}
            <div className="flex items-center gap-2 px-4 py-3">
              <button
                onClick={() => setExpandedId(expandedId === item.id ? null : item.id)}
                className="text-muted-foreground hover:text-foreground"
                title={t("template.expandTemplate")}
              >
                {expandedId === item.id ? (
                  <ChevronDown className="h-4 w-4" />
                ) : (
                  <ChevronRight className="h-4 w-4" />
                )}
              </button>
              <span className="flex-1 text-sm font-medium">{item.name}</span>
              <span className="text-xs text-muted-foreground">{item.description || ""}</span>
              <span className="text-xs text-muted-foreground px-2">
                {item.taskStates.length} {t("template.taskStates")}
              </span>
              <span className="text-xs text-muted-foreground px-2">
                {item.ticketPriorities.length} {t("template.ticketPriorities")}
              </span>
              <span className="text-xs text-muted-foreground px-2">
                {item.fields.length} {t("customField.title")}
              </span>
              <span className="text-xs px-2">
                {item.isActive ? (
                  <span className="text-green-600">{t("common.isActive")}</span>
                ) : (
                  <span className="text-muted-foreground">—</span>
                )}
              </span>
              <button
                onClick={() => startEdit(item)}
                className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                title={t("common.edit")}
              >
                <Pencil className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={() => {
                  setDuplicatingId(item.id);
                  setDuplicateName(`${item.name} (Copy)`);
                }}
                className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                title={t("template.duplicate")}
              >
                <Copy className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={() => deleteMutation.mutate(item.id)}
                className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                title={t("common.delete")}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </div>

            {/* Duplicate prompt */}
            {duplicatingId === item.id && (
              <div className="px-4 pb-3 flex items-center gap-2">
                <input
                  value={duplicateName}
                  onChange={(e) => setDuplicateName(e.target.value)}
                  className="flex-1 px-2 py-1 text-sm border rounded"
                  placeholder={t("template.newTemplate")}
                  autoFocus
                />
                <button
                  onClick={() => handleDuplicate(item.id)}
                  disabled={!duplicateName.trim() || duplicateMutation.isPending}
                  className="px-3 py-1 text-sm font-medium bg-accent-orange text-white rounded hover:bg-accent-orange/90 disabled:opacity-30"
                >
                  {duplicateMutation.isPending ? "..." : t("template.duplicate")}
                </button>
                <button
                  onClick={() => {
                    setDuplicatingId(null);
                    setDuplicateName("");
                  }}
                  className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            )}

            {/* Expanded content: task states + ticket priorities */}
            {expandedId === item.id && (
              <div className="px-4 pb-4 space-y-4 border-t border-border pt-3">
                <TemplateTaskStatesSection templateId={item.id} taskStates={item.taskStates} />
                <TemplateTicketPrioritiesSection
                  templateId={item.id}
                  ticketPriorities={item.ticketPriorities}
                />

                {item.fields.length > 0 && (
                  <div>
                    <p className="text-xs font-medium text-muted-foreground uppercase mb-2">
                      {t("customField.title")}
                    </p>
                    <div className="flex flex-wrap gap-1.5">
                      {item.fields.map((f) => (
                        <span
                          key={f.customFieldDefinitionId}
                          className="px-2 py-0.5 text-xs rounded-full bg-muted text-muted-foreground"
                        >
                          {f.customFieldName}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

export default function LookupsPage() {
  const t = useTranslations("Lookups");
  const [activeTab, setActiveTab] = useState<TabKey>("companies");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">{t("pageTitle")}</h1>
        <p className="text-sm text-muted-foreground mt-1">{t("pageSubtitle")}</p>
      </div>

      <div className="flex gap-1 border-b border-border overflow-x-auto">
        {tabConfig.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={cn(
              "flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium whitespace-nowrap border-b-2 -mb-px transition-colors",
              activeTab === tab.key
                ? "border-accent-orange text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground"
            )}
          >
            <tab.icon className="h-4 w-4" />
            {t(tab.labelKey as "tabCompanies")}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === "companies" && <CompaniesTab />}
      {activeTab === "project-types" && <ProjectTypesTab />}
      {activeTab === "project-states" && <StateTable />}
      {activeTab === "task-types" && <TaskTypesTab />}
      {activeTab === "task-states" && <TaskStatesGlobalTab />}
      {activeTab === "application-roles" && <ApplicationRolesTab />}
      {activeTab === "custom-fields" && <CustomFieldDefinitionsTab />}
      {activeTab === "templates" && <ProjectTemplatesTab />}
    </div>
  );
}
