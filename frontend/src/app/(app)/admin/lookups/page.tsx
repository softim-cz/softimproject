"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
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

const tabs = [
  { key: "companies", label: "Companies", icon: Building2 },
  { key: "project-types", label: "Project Types", icon: FolderTree },
  { key: "project-states", label: "Project States", icon: CircleDot },
  { key: "task-types", label: "Task Types", icon: Tag },
  { key: "application-roles", label: "App Roles", icon: Shield },
  { key: "custom-fields", label: "Custom Fields", icon: SlidersHorizontal },
  { key: "templates", label: "Templates", icon: Copy },
] as const;

type TabKey = (typeof tabs)[number]["key"];

// === Generic inline-edit table for simple lookups ===

function CompaniesTab() {
  const { data, isLoading } = useCompanies();
  const createMutation = useCreateCompany();
  const updateMutation = useUpdateCompany();
  const deleteMutation = useDeleteCompany();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", description: "" });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data) return <EmptyState icon={<Building2 className="h-10 w-10" />} title="No companies" />;

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
          <Plus className="h-4 w-4" /> Add Company
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Description
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Active
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                Actions
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
                    placeholder="Name"
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder="Description"
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">Yes</td>
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
                  <td className="px-4 py-2 text-sm">{item.isActive ? "Yes" : "No"}</td>
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
                      <span className="text-green-600">Yes</span>
                    ) : (
                      <span className="text-muted-foreground">No</span>
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
  const { data, isLoading } = useProjectTypes();
  const createMutation = useCreateProjectType();
  const updateMutation = useUpdateProjectType();
  const deleteMutation = useDeleteProjectType();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", description: "", sortOrder: 0 });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<FolderTree className="h-10 w-10" />} title="No project types" />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", description: "", sortOrder: (data.length + 1) * 10 });
  };
  const startEdit = (item: ProjectType) => {
    setEditId(item.id);
    setAdding(false);
    setForm({ name: item.name, description: item.description || "", sortOrder: item.sortOrder });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updateMutation.mutateAsync({
        ...existing,
        name: form.name,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
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
          <Plus className="h-4 w-4" /> Add Project Type
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Description
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Order
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Active
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                Actions
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
                    placeholder="Name"
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder="Description"
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
                <td className="px-4 py-2 text-sm text-green-600">Yes</td>
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
                    <input
                      type="number"
                      value={form.sortOrder}
                      onChange={(e) => setForm({ ...form, sortOrder: +e.target.value })}
                      className="w-full px-2 py-1 text-sm border rounded"
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">{item.isActive ? "Yes" : "No"}</td>
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
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">Yes</span>
                    ) : (
                      <span className="text-muted-foreground">No</span>
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
  const { data, isLoading } = useProjectStates();
  const createPS = useCreateProjectState();
  const updatePS = useUpdateProjectState();
  const deletePS = useDeleteProjectState();

  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", color: "#3b82f6", sortOrder: 0, isDefault: false });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<CircleDot className="h-10 w-10" />} title="No project states" />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", color: "#3b82f6", sortOrder: (data.length + 1) * 10, isDefault: false });
  };
  const startEdit = (item: ProjectState) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
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
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      } as ProjectState);
      setEditId(null);
    } else {
      await createPS.mutateAsync({
        name: form.name,
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
          <Plus className="h-4 w-4" /> Add Project State
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Color
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Order
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Default
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Active
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                Actions
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
                  <input
                    value={form.name}
                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder="Name"
                    autoFocus
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
                <td className="px-4 py-2">
                  <input
                    type="checkbox"
                    checked={form.isDefault}
                    onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                  />
                </td>
                <td className="px-4 py-2 text-sm text-green-600">Yes</td>
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
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
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
                  <td className="px-4 py-2">
                    <input
                      type="checkbox"
                      checked={form.isDefault}
                      onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
                    />
                  </td>
                  <td className="px-4 py-2 text-sm">{item.isActive ? "Yes" : "No"}</td>
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
                  <td className="px-4 py-3 text-sm font-medium">{item.name}</td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isDefault ? <Check className="h-4 w-4 text-green-600" /> : null}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">Yes</span>
                    ) : (
                      <span className="text-muted-foreground">No</span>
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
  const { data, isLoading } = useTaskTypes();
  const createMutation = useCreateTaskType();
  const updateMutation = useUpdateTaskType();
  const deleteMutation = useDeleteTaskType();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", icon: "", sortOrder: 0 });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data) return <EmptyState icon={<Tag className="h-10 w-10" />} title="No task types" />;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", icon: "", sortOrder: (data.length + 1) * 10 });
  };
  const startEdit = (item: TaskType) => {
    setEditId(item.id);
    setAdding(false);
    setForm({ name: item.name, icon: item.icon || "", sortOrder: item.sortOrder });
  };

  const save = async () => {
    if (editId) {
      const existing = data.find((c) => c.id === editId)!;
      await updateMutation.mutateAsync({
        ...existing,
        name: form.name,
        icon: form.icon || undefined,
        sortOrder: form.sortOrder,
      });
      setEditId(null);
    } else {
      await createMutation.mutateAsync({
        name: form.name,
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
          <Plus className="h-4 w-4" /> Add Task Type
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Icon
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Order
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Active
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                Actions
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
                    placeholder="Name"
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.icon}
                    onChange={(e) => setForm({ ...form, icon: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder="e.g. Bug, Feature"
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
                <td className="px-4 py-2 text-sm text-green-600">Yes</td>
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
                  <td className="px-4 py-2 text-sm">{item.isActive ? "Yes" : "No"}</td>
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
                  <td className="px-4 py-3 text-sm text-muted-foreground">{item.icon || "—"}</td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">Yes</span>
                    ) : (
                      <span className="text-muted-foreground">No</span>
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
    { name: string; description: string; sortOrder: number } & typeof emptyPerms
  >({ name: "", description: "", sortOrder: 0, ...emptyPerms });

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<Shield className="h-10 w-10" />} title="No application roles" />;

  const areas = ["projects", "timeTracking", "reports"] as const;
  const ops = ["Create", "Read", "Update", "Delete"] as const;

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({ name: "", description: "", sortOrder: (data.length + 1) * 10, ...emptyPerms });
  };
  const startEdit = (item: ApplicationRoleEntity) => {
    setEditId(item.id);
    setAdding(false);
    setForm({
      name: item.name,
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
          <Plus className="h-4 w-4" /> Add Role
        </button>
      </div>

      {isEditing && (
        <div className="mb-4 p-4 rounded-lg border border-border bg-card space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Name</label>
              <input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
                autoFocus
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                Description
              </label>
              <input
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                Sort Order
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
            <p className="text-xs font-medium text-muted-foreground mb-2">Permissions</p>
            <div className="rounded-lg border border-border overflow-hidden">
              <table className="w-full">
                <thead>
                  <tr className="bg-muted/50">
                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                      Area
                    </th>
                    {ops.map((op) => (
                      <th
                        key={op}
                        className="px-3 py-2 text-center text-xs font-medium text-muted-foreground uppercase"
                      >
                        {op}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {areas.map((area) => (
                    <tr key={area}>
                      <td className="px-3 py-2 text-sm font-medium capitalize">
                        {area === "timeTracking" ? "Time Tracking" : area}
                      </td>
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
              Cancel
            </button>
            <button
              onClick={save}
              disabled={!form.name}
              className="px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90 disabled:opacity-30"
            >
              Save
            </button>
          </div>
        </div>
      )}

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Description
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">
                Projects
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">
                Time
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">
                Reports
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {data.map((item) => (
              <tr key={item.id} className="hover:bg-muted/30">
                <td className="px-4 py-3 text-sm font-medium">{item.name}</td>
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
      <EmptyState icon={<SlidersHorizontal className="h-10 w-10" />} title="No custom fields" />
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
          <Plus className="h-4 w-4" /> Add Custom Field
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Description
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-24">
                Type
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Req.
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                Options
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Order
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase w-20">
                Active
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase w-24">
                Actions
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
                    placeholder="Name"
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    className="w-full px-2 py-1 text-sm border rounded"
                    placeholder="Description"
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
                        {ft}
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
                    placeholder='["A","B"]'
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
                <td className="px-4 py-2 text-sm text-green-600">Yes</td>
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
                          {ft}
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
                  <td className="px-4 py-2 text-sm">{item.isActive ? "Yes" : "No"}</td>
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
                  <td className="px-4 py-3 text-sm">{item.fieldType}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isRequired ? <Check className="h-4 w-4 text-green-600" /> : null}
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {item.fieldType === "Select" ? item.options || "—" : "—"}
                  </td>
                  <td className="px-4 py-3 text-sm">{item.sortOrder}</td>
                  <td className="px-4 py-3 text-sm">
                    {item.isActive ? (
                      <span className="text-green-600">Yes</span>
                    ) : (
                      <span className="text-muted-foreground">No</span>
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
  const createTS = useCreateTaskState();
  const updateTS = useUpdateTaskState();
  const deleteTS = useDeleteTaskState();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
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
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
        isClosedState: form.isClosedState,
      });
      setEditId(null);
    } else {
      await createTS.mutateAsync({
        name: form.name,
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
        <p className="text-xs font-medium text-muted-foreground uppercase">Task States</p>
        <button
          onClick={startAdd}
          className="flex items-center gap-1 px-2 py-1 text-xs font-medium text-accent-orange hover:bg-accent-orange/10 rounded"
        >
          <Plus className="h-3 w-3" /> Add
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Color
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                Order
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                Def.
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                Closed
              </th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase w-20">
                Actions
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
                  <input
                    value={form.name}
                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                    className="w-full px-2 py-1 text-xs border rounded"
                    placeholder="Name"
                    autoFocus
                  />
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
                  No task states yet
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
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                    />
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
                  <td className="px-3 py-2 text-xs font-medium">{item.name}</td>
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
  const createTP = useCreateTicketPriority();
  const updateTP = useUpdateTicketPriority();
  const deleteTP = useDeleteTicketPriority();
  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", color: "#f59e0b", sortOrder: 0, isDefault: false });

  const startAdd = () => {
    setAdding(true);
    setEditId(null);
    setForm({
      name: "",
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
        color: form.color,
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      });
      setEditId(null);
    } else {
      await createTP.mutateAsync({
        name: form.name,
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
        <p className="text-xs font-medium text-muted-foreground uppercase">Ticket Priorities</p>
        <button
          onClick={startAdd}
          className="flex items-center gap-1 px-2 py-1 text-xs font-medium text-accent-orange hover:bg-accent-orange/10 rounded"
        >
          <Plus className="h-3 w-3" /> Add
        </button>
      </div>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Color
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Name
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                Order
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase w-16">
                Def.
              </th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase w-20">
                Actions
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
                  <input
                    value={form.name}
                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                    className="w-full px-2 py-1 text-xs border rounded"
                    placeholder="Name"
                    autoFocus
                  />
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
                  No ticket priorities yet
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
                    <input
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="w-full px-2 py-1 text-xs border rounded"
                    />
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
                  <td className="px-3 py-2 text-xs font-medium">{item.name}</td>
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

function ProjectTemplatesTab() {
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
  if (!data) return <EmptyState icon={<Copy className="h-10 w-10" />} title="No templates" />;

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
          <Plus className="h-4 w-4" /> Add Template
        </button>
      </div>

      {isEditing && (
        <div className="mb-4 p-4 rounded-lg border border-border bg-card space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Name</label>
              <input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="w-full px-3 py-1.5 text-sm border rounded-lg"
                autoFocus
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">
                Description
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
              <p className="text-xs font-medium text-muted-foreground mb-2">Custom Fields</p>
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
              Cancel
            </button>
            <button
              onClick={save}
              disabled={!form.name}
              className="px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90 disabled:opacity-30"
            >
              Save
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
                {item.taskStates.length} states
              </span>
              <span className="text-xs text-muted-foreground px-2">
                {item.ticketPriorities.length} priorities
              </span>
              <span className="text-xs text-muted-foreground px-2">
                {item.fields.length} fields
              </span>
              <span className="text-xs px-2">
                {item.isActive ? (
                  <span className="text-green-600">Active</span>
                ) : (
                  <span className="text-muted-foreground">Inactive</span>
                )}
              </span>
              <button
                onClick={() => startEdit(item)}
                className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                title="Edit"
              >
                <Pencil className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={() => {
                  setDuplicatingId(item.id);
                  setDuplicateName(`${item.name} (Copy)`);
                }}
                className="p-1 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded"
                title="Duplicate"
              >
                <Copy className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={() => deleteMutation.mutate(item.id)}
                className="p-1 text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded"
                title="Delete"
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
                  placeholder="New template name"
                  autoFocus
                />
                <button
                  onClick={() => handleDuplicate(item.id)}
                  disabled={!duplicateName.trim() || duplicateMutation.isPending}
                  className="px-3 py-1 text-sm font-medium bg-accent-orange text-white rounded hover:bg-accent-orange/90 disabled:opacity-30"
                >
                  {duplicateMutation.isPending ? "..." : "Duplicate"}
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
                      Custom Fields
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
  const [activeTab, setActiveTab] = useState<TabKey>("companies");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Lookups</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Manage configurable lookup tables used across the application
        </p>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border overflow-x-auto">
        {tabs.map((tab) => (
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
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === "companies" && <CompaniesTab />}
      {activeTab === "project-types" && <ProjectTypesTab />}
      {activeTab === "project-states" && <StateTable />}
      {activeTab === "task-types" && <TaskTypesTab />}
      {activeTab === "application-roles" && <ApplicationRolesTab />}
      {activeTab === "custom-fields" && <CustomFieldDefinitionsTab />}
      {activeTab === "templates" && <ProjectTemplatesTab />}
    </div>
  );
}
