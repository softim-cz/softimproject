"use client";

import { useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import { cn } from "@/lib/utils";
import { localizedName } from "@/lib/localized-name";
import type { Locale } from "@/i18n/config";
import {
  AlertTriangle,
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
import { useInlineCrudState } from "@/hooks/use-inline-crud-state";
import {
  InlineCrudTable,
  NameTrioInputs,
  type CrudColumn,
} from "@/components/admin/inline-crud-table";

const inputClass = "w-full px-2 py-1 text-sm border rounded";
const inputClassXs = "w-full px-2 py-1 text-xs border rounded";

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

type CompanyForm = { name: string; description: string };

function CompaniesTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const { data, isLoading } = useCompanies();
  const createMutation = useCreateCompany();
  const updateMutation = useUpdateCompany();
  const deleteMutation = useDeleteCompany();

  const crud = useInlineCrudState<Company, CompanyForm>({
    data,
    emptyForm: { name: "", description: "" },
    toForm: (item) => ({ name: item.name, description: item.description || "" }),
    create: (form) =>
      createMutation.mutateAsync({ name: form.name, description: form.description || undefined }),
    update: (item, form) =>
      updateMutation.mutateAsync({
        ...item,
        name: form.name,
        description: form.description || undefined,
      }),
    remove: (id) => deleteMutation.mutate(id),
    canSave: (form) => !!form.name,
  });

  const columns: CrudColumn<Company, CompanyForm>[] = [
    {
      header: t("common.name"),
      edit: ({ form, set, mode }) => (
        <input
          value={form.name}
          onChange={(e) => set({ name: e.target.value })}
          className={inputClass}
          placeholder={t("common.name")}
          autoFocus={mode === "add"}
        />
      ),
      display: (item) => <span className="font-medium">{item.name}</span>,
    },
    {
      header: t("common.description"),
      edit: ({ form, set }) => (
        <input
          value={form.description}
          onChange={(e) => set({ description: e.target.value })}
          className={inputClass}
          placeholder={t("common.description")}
        />
      ),
      display: (item) => <span className="text-muted-foreground">{item.description || "—"}</span>,
    },
    {
      header: t("common.isActive"),
      edit: ({ mode, item }) =>
        mode === "add" ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span>{item?.isActive ? tCommon("yes") : tCommon("no")}</span>
        ),
      display: (item) =>
        item.isActive ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span className="text-muted-foreground">{tCommon("no")}</span>
        ),
    },
  ];

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<Building2 className="h-10 w-10" />} title={t("common.empty")} />;

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      addLabel={t("company.newCompany")}
      actionsLabel={t("common.actions")}
    />
  );
}

type ProjectTypeForm = {
  name: string;
  nameCs: string;
  nameEn: string;
  description: string;
  sortOrder: number;
};

function ProjectTypesTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useProjectTypes();
  const createMutation = useCreateProjectType();
  const updateMutation = useUpdateProjectType();
  const deleteMutation = useDeleteProjectType();

  const crud = useInlineCrudState<ProjectType, ProjectTypeForm>({
    data,
    emptyForm: (items) => ({
      name: "",
      nameCs: "",
      nameEn: "",
      description: "",
      sortOrder: (items.length + 1) * 10,
    }),
    toForm: (item) => ({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      description: item.description || "",
      sortOrder: item.sortOrder,
    }),
    create: (form) =>
      createMutation.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
      }),
    update: (item, form) =>
      updateMutation.mutateAsync({
        ...item,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
      }),
    remove: (id) => deleteMutation.mutate(id),
    canSave: (form) => !!form.name,
  });

  const namePlaceholders = {
    name: t("common.name"),
    cs: t("common.nameCs"),
    en: t("common.nameEn"),
  };

  const columns: CrudColumn<ProjectType, ProjectTypeForm>[] = [
    {
      header: t("common.name"),
      edit: (ctx) => <NameTrioInputs {...ctx} placeholders={namePlaceholders} />,
      display: (item) => <span className="font-medium">{localizedName(item, locale)}</span>,
    },
    {
      header: t("common.description"),
      edit: ({ form, set }) => (
        <input
          value={form.description}
          onChange={(e) => set({ description: e.target.value })}
          className={inputClass}
          placeholder={t("common.description")}
        />
      ),
      display: (item) => <span className="text-muted-foreground">{item.description || "—"}</span>,
    },
    {
      header: t("common.sortOrder"),
      thClassName: "w-20",
      edit: ({ form, set }) => (
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => set({ sortOrder: +e.target.value })}
          className={inputClass}
        />
      ),
      display: (item) => item.sortOrder,
    },
    {
      header: t("common.isActive"),
      thClassName: "w-20",
      edit: ({ mode, item }) =>
        mode === "add" ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span>{item?.isActive ? tCommon("yes") : tCommon("no")}</span>
        ),
      display: (item) =>
        item.isActive ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span className="text-muted-foreground">{tCommon("no")}</span>
        ),
    },
  ];

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<FolderTree className="h-10 w-10" />} title={t("common.empty")} />;

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      addLabel={t("projectType.newType")}
      actionsLabel={t("common.actions")}
    />
  );
}

type ProjectStateForm = {
  name: string;
  nameCs: string;
  nameEn: string;
  color: string;
  sortOrder: number;
  isDefault: boolean;
};

function StateTable() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useProjectStates();
  const createPS = useCreateProjectState();
  const updatePS = useUpdateProjectState();
  const deletePS = useDeleteProjectState();

  const crud = useInlineCrudState<ProjectState, ProjectStateForm>({
    data,
    emptyForm: (items) => ({
      name: "",
      nameCs: "",
      nameEn: "",
      color: "#3b82f6",
      sortOrder: (items.length + 1) * 10,
      isDefault: false,
    }),
    toForm: (item) => ({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isDefault: item.isDefault,
    }),
    create: (form) =>
      createPS.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      }),
    update: (item, form) =>
      updatePS.mutateAsync({
        ...item,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      } as ProjectState),
    remove: (id) => deletePS.mutate(id),
    canSave: (form) => !!form.name,
  });

  const namePlaceholders = {
    name: t("common.name"),
    cs: t("common.nameCs"),
    en: t("common.nameEn"),
  };

  const columns: CrudColumn<ProjectState, ProjectStateForm>[] = [
    {
      header: t("common.color"),
      edit: ({ form, set }) => (
        <input
          type="color"
          value={form.color}
          onChange={(e) => set({ color: e.target.value })}
          className="w-8 h-8 rounded cursor-pointer"
        />
      ),
      display: (item) => (
        <div className="w-6 h-6 rounded-full border" style={{ backgroundColor: item.color }} />
      ),
    },
    {
      header: t("common.name"),
      edit: (ctx) => <NameTrioInputs {...ctx} placeholders={namePlaceholders} />,
      display: (item) => <span className="font-medium">{localizedName(item, locale)}</span>,
    },
    {
      header: t("common.sortOrder"),
      thClassName: "w-20",
      edit: ({ form, set }) => (
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => set({ sortOrder: +e.target.value })}
          className={inputClass}
        />
      ),
      display: (item) => item.sortOrder,
    },
    {
      header: t("common.isDefault"),
      thClassName: "w-20",
      edit: ({ form, set }) => (
        <input
          type="checkbox"
          checked={form.isDefault}
          onChange={(e) => set({ isDefault: e.target.checked })}
        />
      ),
      display: (item) => (item.isDefault ? <Check className="h-4 w-4 text-green-600" /> : null),
    },
    {
      header: t("common.isActive"),
      thClassName: "w-20",
      edit: ({ mode, item }) =>
        mode === "add" ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span>{item?.isActive ? tCommon("yes") : tCommon("no")}</span>
        ),
      display: (item) =>
        item.isActive ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span className="text-muted-foreground">{tCommon("no")}</span>
        ),
    },
  ];

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<CircleDot className="h-10 w-10" />} title={t("common.empty")} />;

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      addLabel={t("projectState.newState")}
      actionsLabel={t("common.actions")}
    />
  );
}

type TaskTypeForm = {
  name: string;
  nameCs: string;
  nameEn: string;
  icon: string;
  sortOrder: number;
};

function TaskTypesTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useTaskTypes();
  const createMutation = useCreateTaskType();
  const updateMutation = useUpdateTaskType();
  const deleteMutation = useDeleteTaskType();

  const crud = useInlineCrudState<TaskType, TaskTypeForm>({
    data,
    emptyForm: (items) => ({
      name: "",
      nameCs: "",
      nameEn: "",
      icon: "",
      sortOrder: (items.length + 1) * 10,
    }),
    toForm: (item) => ({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      icon: item.icon || "",
      sortOrder: item.sortOrder,
    }),
    create: (form) =>
      createMutation.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        icon: form.icon || undefined,
        sortOrder: form.sortOrder,
      }),
    update: (item, form) =>
      updateMutation.mutateAsync({
        ...item,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        icon: form.icon || undefined,
        sortOrder: form.sortOrder,
      }),
    remove: (id) => deleteMutation.mutate(id),
    canSave: (form) => !!form.name,
  });

  const namePlaceholders = {
    name: t("common.name"),
    cs: t("common.nameCs"),
    en: t("common.nameEn"),
  };

  const columns: CrudColumn<TaskType, TaskTypeForm>[] = [
    {
      header: t("common.name"),
      edit: (ctx) => <NameTrioInputs {...ctx} placeholders={namePlaceholders} />,
      display: (item) => <span className="font-medium">{localizedName(item, locale)}</span>,
    },
    {
      header: t("common.icon"),
      edit: ({ form, set }) => (
        <input
          value={form.icon}
          onChange={(e) => set({ icon: e.target.value })}
          className={inputClass}
          placeholder={t("common.iconPlaceholder")}
        />
      ),
      display: (item) => <span className="text-muted-foreground">{item.icon || "—"}</span>,
    },
    {
      header: t("common.sortOrder"),
      thClassName: "w-20",
      edit: ({ form, set }) => (
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => set({ sortOrder: +e.target.value })}
          className={inputClass}
        />
      ),
      display: (item) => item.sortOrder,
    },
    {
      header: t("common.isActive"),
      thClassName: "w-20",
      edit: ({ mode, item }) =>
        mode === "add" ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span>{item?.isActive ? tCommon("yes") : tCommon("no")}</span>
        ),
      display: (item) =>
        item.isActive ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span className="text-muted-foreground">{tCommon("no")}</span>
        ),
    },
  ];

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data) return <EmptyState icon={<Tag className="h-10 w-10" />} title={t("common.empty")} />;

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      addLabel={t("taskType.newType")}
      actionsLabel={t("common.actions")}
    />
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

type CustomFieldForm = {
  name: string;
  description: string;
  fieldType: string;
  isRequired: boolean;
  options: string;
  sortOrder: number;
};

function CustomFieldDefinitionsTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const { data, isLoading } = useCustomFieldDefinitions();
  const createMutation = useCreateCustomFieldDefinition();
  const updateMutation = useUpdateCustomFieldDefinition();
  const deleteMutation = useDeleteCustomFieldDefinition();

  const crud = useInlineCrudState<CustomFieldDefinition, CustomFieldForm>({
    data,
    emptyForm: (items) => ({
      name: "",
      description: "",
      fieldType: "Text",
      isRequired: false,
      options: "",
      sortOrder: (items.length + 1) * 10,
    }),
    toForm: (item) => ({
      name: item.name,
      description: item.description || "",
      fieldType: item.fieldType,
      isRequired: item.isRequired,
      options: item.options || "",
      sortOrder: item.sortOrder,
    }),
    create: (form) =>
      createMutation.mutateAsync({
        name: form.name,
        description: form.description || undefined,
        fieldType: form.fieldType,
        isRequired: form.isRequired,
        options: form.options || undefined,
        sortOrder: form.sortOrder,
      }),
    update: (item, form) =>
      updateMutation.mutateAsync({
        ...item,
        name: form.name,
        description: form.description || undefined,
        fieldType: form.fieldType,
        isRequired: form.isRequired,
        options: form.options || undefined,
        sortOrder: form.sortOrder,
      }),
    remove: (id) => deleteMutation.mutate(id),
    canSave: (form) => !!form.name,
  });

  const fieldTypeOptions = Object.values(CustomFieldType);

  const columns: CrudColumn<CustomFieldDefinition, CustomFieldForm>[] = [
    {
      header: t("common.name"),
      edit: ({ form, set, mode }) => (
        <input
          value={form.name}
          onChange={(e) => set({ name: e.target.value })}
          className={inputClass}
          placeholder={t("common.name")}
          autoFocus={mode === "add"}
        />
      ),
      display: (item) => <span className="font-medium">{item.name}</span>,
    },
    {
      header: t("common.description"),
      edit: ({ form, set }) => (
        <input
          value={form.description}
          onChange={(e) => set({ description: e.target.value })}
          className={inputClass}
          placeholder={t("common.description")}
        />
      ),
      display: (item) => <span className="text-muted-foreground">{item.description || "—"}</span>,
    },
    {
      header: t("customField.fieldType"),
      thClassName: "w-24",
      edit: ({ form, set }) => (
        <select
          value={form.fieldType}
          onChange={(e) => set({ fieldType: e.target.value })}
          className={inputClass}
        >
          {fieldTypeOptions.map((ft) => (
            <option key={ft} value={ft}>
              {t(`customField.types.${ft}` as "customField.types.Text")}
            </option>
          ))}
        </select>
      ),
      display: (item) => t(`customField.types.${item.fieldType}` as "customField.types.Text"),
    },
    {
      header: t("customField.required"),
      thClassName: "w-20",
      edit: ({ form, set }) => (
        <input
          type="checkbox"
          checked={form.isRequired}
          onChange={(e) => set({ isRequired: e.target.checked })}
        />
      ),
      display: (item) => (item.isRequired ? <Check className="h-4 w-4 text-green-600" /> : null),
    },
    {
      header: t("customField.options"),
      edit: ({ form, set }) => (
        <input
          value={form.options}
          onChange={(e) => set({ options: e.target.value })}
          className={inputClass}
          placeholder={t("customField.optionsPlaceholder")}
          disabled={form.fieldType !== "Select"}
        />
      ),
      display: (item) => (
        <span className="text-muted-foreground">
          {item.fieldType === "Select" ? item.options || "—" : "—"}
        </span>
      ),
    },
    {
      header: t("common.sortOrder"),
      thClassName: "w-20",
      edit: ({ form, set }) => (
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => set({ sortOrder: +e.target.value })}
          className={inputClass}
        />
      ),
      display: (item) => item.sortOrder,
    },
    {
      header: t("common.isActive"),
      thClassName: "w-20",
      edit: ({ mode, item }) =>
        mode === "add" ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span>{item?.isActive ? tCommon("yes") : tCommon("no")}</span>
        ),
      display: (item) =>
        item.isActive ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span className="text-muted-foreground">{tCommon("no")}</span>
        ),
    },
  ];

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return (
      <EmptyState icon={<SlidersHorizontal className="h-10 w-10" />} title={t("common.empty")} />
    );

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      addLabel={t("customField.newField")}
      actionsLabel={t("common.actions")}
    />
  );
}

// === Inline sub-table for task states / ticket priorities within a template ===

type TemplateTaskStateForm = {
  name: string;
  nameCs: string;
  nameEn: string;
  color: string;
  sortOrder: number;
  isDefault: boolean;
  isClosedState: boolean;
};

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

  const crud = useInlineCrudState<TaskState, TemplateTaskStateForm>({
    data: taskStates,
    emptyForm: (items) => ({
      name: "",
      nameCs: "",
      nameEn: "",
      color: "#3b82f6",
      sortOrder: (items.length + 1) * 10,
      isDefault: false,
      isClosedState: false,
    }),
    toForm: (item) => ({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isDefault: item.isDefault,
      isClosedState: item.isClosedState,
    }),
    create: (form) =>
      createTS.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        isClosedState: form.isClosedState,
        projectTemplateId: templateId,
      }),
    update: (item, form) =>
      updateTS.mutateAsync({
        ...item,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        isClosedState: form.isClosedState,
      }),
    remove: (id) => deleteTS.mutate(id),
    canSave: (form) => !!form.name,
  });

  const namePlaceholders = {
    name: t("common.name"),
    cs: t("common.nameCs"),
    en: t("common.nameEn"),
  };

  const columns: CrudColumn<TaskState, TemplateTaskStateForm>[] = [
    {
      header: t("common.color"),
      edit: ({ form, set }) => (
        <input
          type="color"
          value={form.color}
          onChange={(e) => set({ color: e.target.value })}
          className="w-7 h-7 rounded cursor-pointer"
        />
      ),
      display: (item) => (
        <div className="w-5 h-5 rounded-full border" style={{ backgroundColor: item.color }} />
      ),
    },
    {
      header: t("common.name"),
      edit: (ctx) => <NameTrioInputs {...ctx} placeholders={namePlaceholders} compact />,
      display: (item) => <span className="font-medium">{localizedName(item, locale)}</span>,
    },
    {
      header: t("common.sortOrder"),
      thClassName: "w-16",
      edit: ({ form, set }) => (
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => set({ sortOrder: +e.target.value })}
          className={inputClassXs}
        />
      ),
      display: (item) => item.sortOrder,
    },
    {
      header: t("common.isDefault"),
      thClassName: "w-16",
      edit: ({ form, set }) => (
        <input
          type="checkbox"
          checked={form.isDefault}
          onChange={(e) => set({ isDefault: e.target.checked })}
        />
      ),
      display: (item) => (item.isDefault ? <Check className="h-3.5 w-3.5 text-green-600" /> : null),
    },
    {
      header: t("common.isClosed"),
      thClassName: "w-16",
      edit: ({ form, set }) => (
        <input
          type="checkbox"
          checked={form.isClosedState}
          onChange={(e) => set({ isClosedState: e.target.checked })}
        />
      ),
      display: (item) =>
        item.isClosedState ? <Check className="h-3.5 w-3.5 text-red-500" /> : null,
    },
  ];

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      compact
      title={t("template.taskStates")}
      addLabel={t("template.addTaskState")}
      actionsLabel={t("common.actions")}
      emptyLabel={t("common.empty")}
    />
  );
}

type TemplatePriorityForm = {
  name: string;
  nameCs: string;
  nameEn: string;
  color: string;
  sortOrder: number;
  isDefault: boolean;
};

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

  const crud = useInlineCrudState<TicketPriorityLookup, TemplatePriorityForm>({
    data: ticketPriorities,
    emptyForm: (items) => ({
      name: "",
      nameCs: "",
      nameEn: "",
      color: "#f59e0b",
      sortOrder: (items.length + 1) * 10,
      isDefault: false,
    }),
    toForm: (item) => ({
      name: item.name,
      nameCs: item.nameCs || "",
      nameEn: item.nameEn || "",
      color: item.color,
      sortOrder: item.sortOrder,
      isDefault: item.isDefault,
    }),
    create: (form) =>
      createTP.mutateAsync({
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        projectTemplateId: templateId,
      }),
    update: (item, form) =>
      updateTP.mutateAsync({
        ...item,
        name: form.name,
        nameCs: form.nameCs || undefined,
        nameEn: form.nameEn || undefined,
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      }),
    remove: (id) => deleteTP.mutate(id),
    canSave: (form) => !!form.name,
  });

  const namePlaceholders = {
    name: t("common.name"),
    cs: t("common.nameCs"),
    en: t("common.nameEn"),
  };

  const columns: CrudColumn<TicketPriorityLookup, TemplatePriorityForm>[] = [
    {
      header: t("common.color"),
      edit: ({ form, set }) => (
        <input
          type="color"
          value={form.color}
          onChange={(e) => set({ color: e.target.value })}
          className="w-7 h-7 rounded cursor-pointer"
        />
      ),
      display: (item) => (
        <div className="w-5 h-5 rounded-full border" style={{ backgroundColor: item.color }} />
      ),
    },
    {
      header: t("common.name"),
      edit: (ctx) => <NameTrioInputs {...ctx} placeholders={namePlaceholders} compact />,
      display: (item) => <span className="font-medium">{localizedName(item, locale)}</span>,
    },
    {
      header: t("common.sortOrder"),
      thClassName: "w-16",
      edit: ({ form, set }) => (
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => set({ sortOrder: +e.target.value })}
          className={inputClassXs}
        />
      ),
      display: (item) => item.sortOrder,
    },
    {
      header: t("common.isDefault"),
      thClassName: "w-16",
      edit: ({ form, set }) => (
        <input
          type="checkbox"
          checked={form.isDefault}
          onChange={(e) => set({ isDefault: e.target.checked })}
        />
      ),
      display: (item) => (item.isDefault ? <Check className="h-3.5 w-3.5 text-green-600" /> : null),
    },
  ];

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      compact
      title={t("template.ticketPriorities")}
      addLabel={t("template.addPriority")}
      actionsLabel={t("common.actions")}
      emptyLabel={t("common.empty")}
    />
  );
}

// Read-only přehled stavů úkolů napříč všemi šablonami. Editace je
// per-šablona, aby uživatel viděl/měnil stavy v kontextu jedné šablony
// (a nedopustil se overshootu napříč jinými, které stejný stav sdílejí).
// Bez tohoto omezení by globální edit mlčky propsal změnu do všech projektů
// vázaných na danou šablonu — i těch, které admin v daný moment vůbec nemá
// v hlavě.
function TaskStatesGlobalTab() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const locale = useLocale() as Locale;
  const { data, isLoading } = useTaskStates();
  const { data: templates } = useProjectTemplates();
  const [templateFilter, setTemplateFilter] = useState<string>("");

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<ListTodo className="h-10 w-10" />} title={t("common.empty")} />;

  const templateNameById = (id: string) => templates?.find((tpl) => tpl.id === id)?.name || "—";

  const filtered = templateFilter
    ? data.filter((ts) => ts.projectTemplateId === templateFilter)
    : data;

  return (
    <div>
      <div className="mb-3">
        <p className="text-sm text-muted-foreground">{t("taskStatesGlobal.subtitle")}</p>
      </div>
      <div className="mb-3 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200">
        <AlertTriangle className="h-4 w-4 mt-0.5 shrink-0" />
        <span>{t("taskStatesGlobal.readOnlyBanner")}</span>
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
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {filtered.length === 0 && (
              <tr>
                <td colSpan={7} className="px-3 py-3 text-xs text-center text-muted-foreground">
                  {t("common.empty")}
                </td>
              </tr>
            )}
            {filtered.map((item) => (
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
              </tr>
            ))}
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
  const { data: taskTypes } = useTaskTypes();
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
    selectedTaskTypeIds: [] as string[],
  });
  const [duplicateName, setDuplicateName] = useState("");
  const [duplicatingId, setDuplicatingId] = useState<string | null>(null);

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data) return <EmptyState icon={<Copy className="h-10 w-10" />} title={t("common.empty")} />;

  const activeFields = fieldDefs?.filter((f) => f.isActive) || [];

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
      description: "",
      isActive: true,
      selectedFieldIds: [],
      selectedTaskTypeIds: [],
    });
  };
  const startEdit = (item: ProjectTemplate) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
      description: item.description || "",
      isActive: item.isActive,
      selectedFieldIds: item.fields.map((f) => f.customFieldDefinitionId),
      selectedTaskTypeIds: item.allowedTaskTypeIds,
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

  const toggleTaskType = (id: string) => {
    setForm((prev) => ({
      ...prev,
      selectedTaskTypeIds: prev.selectedTaskTypeIds.includes(id)
        ? prev.selectedTaskTypeIds.filter((tid) => tid !== id)
        : [...prev.selectedTaskTypeIds, id],
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
        allowedTaskTypeIds: form.selectedTaskTypeIds,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
        description: form.description || undefined,
        customFieldDefinitionIds: form.selectedFieldIds,
        allowedTaskTypeIds: form.selectedTaskTypeIds,
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

          {taskTypes && taskTypes.length > 0 && (
            <div>
              <p className="text-xs font-medium text-muted-foreground mb-1">
                {t("template.allowedTaskTypes")}
              </p>
              <p className="text-xs text-muted-foreground mb-2">
                {t("template.allowedTaskTypesHint")}
              </p>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                {taskTypes
                  .filter((tt) => tt.isActive || form.selectedTaskTypeIds.includes(tt.id))
                  .map((tt) => (
                    <label
                      key={tt.id}
                      className="flex items-center gap-2 text-sm p-2 rounded-lg border border-border hover:bg-muted/30 cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={form.selectedTaskTypeIds.includes(tt.id)}
                        onChange={() => toggleTaskType(tt.id)}
                        className="rounded"
                      />
                      <span className="font-medium">{tt.name}</span>
                      {!tt.isActive && (
                        <span className="text-xs text-muted-foreground">
                          ({t("common.inactive")})
                        </span>
                      )}
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
              {item.allowedTaskTypeIds.length > 0 && (
                <span className="text-xs text-muted-foreground px-2">
                  {item.allowedTaskTypeIds.length} {t("template.allowedTaskTypes")}
                </span>
              )}
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
