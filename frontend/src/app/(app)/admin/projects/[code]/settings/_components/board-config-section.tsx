"use client";

import { useState } from "react";
import {
  useBoard,
  useCreateColumn,
  useUpdateColumn,
  useDeleteColumn,
  useReorderColumns,
} from "@/queries/kanban";
import { useTaskStates } from "@/queries/lookups";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { LayoutGrid, X, Trash2, Pencil, Check, GripVertical, Eye, EyeOff } from "lucide-react";
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";

const COLOR_PRESETS = [
  null,
  "#ef4444",
  "#f97316",
  "#eab308",
  "#22c55e",
  "#06b6d4",
  "#3b82f6",
  "#8b5cf6",
  "#ec4899",
  "#6b7280",
  "#000000",
] as const;

function ColorPicker({
  value,
  onChange,
}: {
  value: string | undefined;
  onChange: (v: string | undefined) => void;
}) {
  const t = useTranslations("ProjectSettings");
  return (
    <div className="flex items-center gap-1.5">
      {COLOR_PRESETS.map((color) => (
        <button
          key={color ?? "none"}
          type="button"
          onClick={() => onChange(color ?? undefined)}
          className="rounded-full w-5 h-5 border transition-all flex items-center justify-center"
          style={{
            backgroundColor: color ?? "transparent",
            borderColor:
              (value ?? null) === color ? "currentColor" : color ? color : "var(--border)",
            boxShadow:
              (value ?? null) === color
                ? `0 0 0 2px var(--background), 0 0 0 4px ${color ?? "var(--foreground)"}`
                : "none",
          }}
          title={color ?? t("colorNone")}
        >
          {color === null && <X className="h-3 w-3 text-muted-foreground" />}
        </button>
      ))}
    </div>
  );
}

function TaskStateMultiSelect({
  selected,
  onChange,
  taskStates,
  stateAssignments = {},
}: {
  selected: string[];
  onChange: (ids: string[]) => void;
  taskStates: { id: string; name: string; color: string }[];
  stateAssignments?: Record<string, string>;
}) {
  const t = useTranslations("ProjectSettings");
  const toggle = (id: string) => {
    onChange(selected.includes(id) ? selected.filter((s) => s !== id) : [...selected, id]);
  };

  return (
    <div className="space-y-1.5">
      <div className="flex flex-wrap gap-1 min-h-[24px]">
        {selected.map((id) => {
          const ts = taskStates.find((tk) => tk.id === id);
          if (!ts) return null;
          return (
            <span
              key={id}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium"
              style={{ backgroundColor: ts.color + "20", color: ts.color }}
            >
              {ts.name}
              <button type="button" onClick={() => toggle(id)} className="hover:opacity-70">
                <X className="h-3 w-3" />
              </button>
            </span>
          );
        })}
      </div>
      <div className="border border-input rounded-lg bg-background max-h-32 overflow-y-auto">
        {taskStates.map((ts) => {
          const owner = stateAssignments[ts.id];
          const isOwnedByOther = owner && !selected.includes(ts.id);
          return (
            <label
              key={ts.id}
              className="flex items-center gap-2 px-3 py-1.5 text-sm cursor-pointer hover:bg-muted transition-colors"
              title={isOwnedByOther ? t("stateInColumn", { column: owner }) : undefined}
            >
              <input
                type="checkbox"
                checked={selected.includes(ts.id)}
                onChange={() => toggle(ts.id)}
                className="rounded"
              />
              <span
                className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                style={{ backgroundColor: ts.color }}
              />
              <span className={cn(isOwnedByOther && "text-muted-foreground")}>{ts.name}</span>
              {isOwnedByOther && (
                <span className="ml-auto text-xs text-muted-foreground italic">
                  {t("stateInColumnShort", { column: owner })}
                </span>
              )}
            </label>
          );
        })}
      </div>
    </div>
  );
}

function SortableColumnRow({
  col,
  isEditing,
  onStartEdit,
  onDelete,
  onToggleVisibility,
  editState,
  onEditChange,
  onSaveEdit,
  onCancelEdit,
  updatePending,
  deletePending,
  activeTaskStates,
  stateAssignments,
}: {
  col: {
    id: string;
    name: string;
    wipLimit?: number;
    color?: string;
    isVisible: boolean;
    ticketCount: number;
    taskStates: { id: string; name: string; color: string }[];
  };
  isEditing: boolean;
  onStartEdit: () => void;
  onDelete: () => void;
  onToggleVisibility: () => void;
  editState: { name: string; taskStateIds: string[]; wipLimit: string; color: string | undefined };
  onEditChange: (patch: Partial<typeof editState>) => void;
  onSaveEdit: () => void;
  onCancelEdit: () => void;
  updatePending: boolean;
  deletePending: boolean;
  activeTaskStates: { id: string; name: string; color: string }[];
  stateAssignments: Record<string, string>;
}) {
  const t = useTranslations("ProjectSettings");
  const tCommon = useTranslations("Common");
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: col.id,
  });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };
  const btnOutline =
    "inline-flex items-center justify-center p-1.5 rounded-lg border border-border text-muted-foreground hover:bg-muted transition-colors disabled:opacity-50";
  const btnDestructive =
    "inline-flex items-center gap-1.5 p-1.5 rounded-lg text-destructive hover:bg-destructive/10 transition-colors disabled:opacity-50";
  const inputClass =
    "rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

  if (isEditing) {
    return (
      <div
        ref={setNodeRef}
        style={style}
        className="px-3 py-3 rounded-lg border border-primary/30 bg-background space-y-3"
      >
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={editState.name}
            onChange={(e) => onEditChange({ name: e.target.value })}
            className={`flex-1 ${inputClass}`}
            placeholder={t("columnName")}
          />
          <input
            type="number"
            value={editState.wipLimit}
            onChange={(e) => onEditChange({ wipLimit: e.target.value })}
            className={`w-20 ${inputClass}`}
            placeholder="WIP"
            min={1}
          />
          <button
            onClick={onSaveEdit}
            disabled={updatePending}
            className={btnOutline}
            title={t("save")}
          >
            <Check className="h-4 w-4 text-green-600" />
          </button>
          <button onClick={onCancelEdit} className={btnOutline} title={tCommon("cancel")}>
            <X className="h-4 w-4" />
          </button>
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("taskStates")}</label>
          <TaskStateMultiSelect
            selected={editState.taskStateIds}
            onChange={(ids) => onEditChange({ taskStateIds: ids })}
            taskStates={activeTaskStates}
            stateAssignments={stateAssignments}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("color")}</label>
          <ColorPicker value={editState.color} onChange={(c) => onEditChange({ color: c })} />
        </div>
      </div>
    );
  }

  const canHide = col.ticketCount === 0;
  const hideTitle = col.isVisible
    ? canHide
      ? t("hideColumn")
      : t("cannotHide", { count: col.ticketCount })
    : t("showColumn");

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "flex items-center gap-2 px-3 py-2 rounded-lg border border-border/50 bg-background",
        !col.isVisible && "opacity-60"
      )}
    >
      <button
        {...attributes}
        {...listeners}
        className="cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground p-0.5"
      >
        <GripVertical className="h-4 w-4" />
      </button>
      {col.color && (
        <span
          className="w-3 h-3 rounded-full flex-shrink-0"
          style={{ backgroundColor: col.color }}
        />
      )}
      <span className="flex-1 text-sm font-medium text-foreground">
        {col.name}
        {!col.isVisible && (
          <span className="ml-2 text-xs text-muted-foreground italic">{t("hidden")}</span>
        )}
      </span>
      <div className="flex flex-wrap gap-1">
        {col.taskStates.map((ts) => (
          <span
            key={ts.id}
            className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
            style={{ backgroundColor: ts.color + "20", color: ts.color }}
          >
            {ts.name}
          </span>
        ))}
      </div>
      <span className="text-xs text-muted-foreground w-16 text-center">
        WIP: {col.wipLimit ?? "–"}
      </span>
      <button
        onClick={onToggleVisibility}
        disabled={col.isVisible && !canHide}
        className={btnOutline}
        title={hideTitle}
      >
        {col.isVisible ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
      </button>
      <button onClick={onStartEdit} className={btnOutline} title={tCommon("edit")}>
        <Pencil className="h-4 w-4" />
      </button>
      <button
        onClick={onDelete}
        disabled={deletePending}
        className={btnDestructive}
        title={tCommon("delete")}
      >
        <Trash2 className="h-4 w-4" />
      </button>
    </div>
  );
}

export function BoardConfigSection({
  projectId,
  projectTemplateId,
}: {
  projectId: string;
  projectTemplateId?: string;
}) {
  const t = useTranslations("ProjectSettings");
  const { data: board, isLoading: boardLoading } = useBoard(projectId);
  const { data: taskStates } = useTaskStates(projectTemplateId);
  const createColumn = useCreateColumn();
  const updateColumn = useUpdateColumn();
  const deleteColumn = useDeleteColumn();
  const reorderColumns = useReorderColumns();

  const [newName, setNewName] = useState("");
  const [newTaskStateIds, setNewTaskStateIds] = useState<string[]>([]);
  const [newWipLimit, setNewWipLimit] = useState("");
  const [newColor, setNewColor] = useState<string | undefined>(undefined);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editState, setEditState] = useState({
    name: "",
    taskStateIds: [] as string[],
    wipLimit: "",
    color: undefined as string | undefined,
  });

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const inputClass =
    "rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";
  const btnPrimary =
    "px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50";

  const activeTaskStates = taskStates?.filter((ts) => ts.isActive) ?? [];
  const columns = board?.columns ?? [];

  if (boardLoading) {
    return (
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <Skeleton className="h-6 w-48" />
        <Skeleton className="h-32 w-full" />
      </section>
    );
  }

  if (!board) {
    return (
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <LayoutGrid className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">{t("boardConfig")}</h2>
        </div>
        <p className="text-sm text-muted-foreground">{t("noBoard")}</p>
      </section>
    );
  }

  const handleAddColumn = async () => {
    if (!newName.trim() || newTaskStateIds.length === 0) return;
    try {
      await createColumn.mutateAsync({
        projectId,
        boardId: board.id,
        name: newName.trim(),
        wipLimit: newWipLimit ? parseInt(newWipLimit, 10) : undefined,
        mapsToTaskStateIds: newTaskStateIds,
        color: newColor,
      });
      toast.success(t("columnAdded"));
      setNewName("");
      setNewTaskStateIds([]);
      setNewWipLimit("");
      setNewColor(undefined);
    } catch {
      toast.error(t("columnAddFailed"));
    }
  };

  const startEditing = (col: (typeof columns)[0]) => {
    setEditingId(col.id);
    setEditState({
      name: col.name,
      taskStateIds: col.taskStates.map((ts) => ts.id),
      wipLimit: col.wipLimit?.toString() ?? "",
      color: col.color,
    });
  };

  const handleSaveEdit = async () => {
    if (!editingId || !editState.name.trim() || editState.taskStateIds.length === 0) return;
    const originalColumn = columns.find((c) => c.id === editingId);
    try {
      await updateColumn.mutateAsync({
        projectId,
        boardId: board.id,
        columnId: editingId,
        name: editState.name.trim(),
        wipLimit: editState.wipLimit ? parseInt(editState.wipLimit, 10) : undefined,
        mapsToTaskStateIds: editState.taskStateIds,
        color: editState.color,
        isVisible: originalColumn?.isVisible ?? true,
      });
      toast.success(t("columnUpdated"));
      setEditingId(null);
    } catch {
      toast.error(t("columnUpdateFailed"));
    }
  };

  const handleDeleteColumn = async (columnId: string) => {
    if (!window.confirm(t("columnDeleteConfirm"))) return;
    try {
      await deleteColumn.mutateAsync({ projectId, boardId: board.id, columnId });
      toast.success(t("columnDeleted"));
    } catch {
      toast.error(t("columnDeleteFailed"));
    }
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIndex = columns.findIndex((c) => c.id === active.id);
    const newIndex = columns.findIndex((c) => c.id === over.id);
    if (oldIndex === -1 || newIndex === -1) return;
    const ids = columns.map((c) => c.id);
    ids.splice(oldIndex, 1);
    ids.splice(newIndex, 0, active.id as string);
    try {
      await reorderColumns.mutateAsync({ projectId, boardId: board.id, columnIds: ids });
    } catch {
      toast.error(t("reorderFailed"));
    }
  };

  const handleToggleVisibility = async (col: (typeof columns)[0]) => {
    try {
      await updateColumn.mutateAsync({
        projectId,
        boardId: board.id,
        columnId: col.id,
        name: col.name,
        wipLimit: col.wipLimit,
        mapsToTaskStateIds: col.taskStates.map((ts) => ts.id),
        color: col.color,
        isVisible: !col.isVisible,
      });
      toast.success(col.isVisible ? t("columnHidden") : t("columnShown"));
    } catch (err) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        t("columnHidden_failed");
      toast.error(message);
    }
  };

  const stateOwners = columns.reduce<Record<string, string>>((acc, c) => {
    c.taskStates.forEach((ts) => {
      acc[ts.id] = c.name;
    });
    return acc;
  }, {});

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <LayoutGrid className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("boardConfig")}</h2>
      </div>

      {columns.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("noColumns")}</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={columns.map((c) => c.id)} strategy={verticalListSortingStrategy}>
            <div className="space-y-1">
              {columns.map((col) => {
                const ticketCount = col.tickets?.length ?? 0;
                const assignmentsForThisRow = Object.fromEntries(
                  Object.entries(stateOwners).filter(([, owner]) => owner !== col.name)
                );
                return (
                  <SortableColumnRow
                    key={col.id}
                    col={{
                      id: col.id,
                      name: col.name,
                      wipLimit: col.wipLimit,
                      color: col.color,
                      isVisible: col.isVisible,
                      ticketCount,
                      taskStates: col.taskStates,
                    }}
                    isEditing={editingId === col.id}
                    onStartEdit={() => startEditing(col)}
                    onDelete={() => handleDeleteColumn(col.id)}
                    onToggleVisibility={() => handleToggleVisibility(col)}
                    editState={editState}
                    onEditChange={(patch) => setEditState((prev) => ({ ...prev, ...patch }))}
                    onSaveEdit={handleSaveEdit}
                    onCancelEdit={() => setEditingId(null)}
                    updatePending={updateColumn.isPending}
                    deletePending={deleteColumn.isPending}
                    activeTaskStates={activeTaskStates}
                    stateAssignments={assignmentsForThisRow}
                  />
                );
              })}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <div className="pt-2 border-t border-border space-y-3">
        <p className="text-sm font-medium text-card-foreground">{t("addColumn")}</p>
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs text-muted-foreground mb-1">{t("name")}</label>
            <input
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder={t("columnName")}
              className={`w-full ${inputClass}`}
            />
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">{t("wipLimit")}</label>
            <input
              type="number"
              value={newWipLimit}
              onChange={(e) => setNewWipLimit(e.target.value)}
              placeholder={t("wipLimitOptional")}
              min={1}
              className={`w-24 ${inputClass}`}
            />
          </div>
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("taskStates")}</label>
          <TaskStateMultiSelect
            selected={newTaskStateIds}
            onChange={setNewTaskStateIds}
            taskStates={activeTaskStates}
            stateAssignments={stateOwners}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("color")}</label>
          <ColorPicker value={newColor} onChange={setNewColor} />
        </div>
        <button
          onClick={handleAddColumn}
          disabled={!newName.trim() || newTaskStateIds.length === 0 || createColumn.isPending}
          className={btnPrimary}
        >
          {createColumn.isPending ? t("adding") : t("addColumn")}
        </button>
      </div>
    </section>
  );
}
