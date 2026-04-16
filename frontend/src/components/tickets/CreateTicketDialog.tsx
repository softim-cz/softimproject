"use client";

import { useState } from "react";
import { useCreateTicket, type CreateTicketPayload } from "@/queries/tickets";
import { useTicketPriorities } from "@/queries/lookups";
import { toast } from "sonner";
import { X } from "lucide-react";

interface CreateTicketDialogProps {
  projectId: string;
  open: boolean;
  onClose: () => void;
  defaultColumnId?: string;
  projectTemplateId?: string;
}

export function CreateTicketDialog({
  projectId,
  open,
  onClose,
  defaultColumnId,
  projectTemplateId,
}: CreateTicketDialogProps) {
  const createTicket = useCreateTicket();
  const { data: priorities } = useTicketPriorities(projectTemplateId);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [ticketPriorityId, setTicketPriorityId] = useState<string>("");
  const [dueDate, setDueDate] = useState("");
  const [estimatedHours, setEstimatedHours] = useState("");

  if (!open) return null;

  const defaultPriority = priorities?.find((p) => p.isDefault) ?? priorities?.[0];
  const effectivePriorityId = ticketPriorityId || defaultPriority?.id || "";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    const payload: CreateTicketPayload = {
      projectId,
      title: title.trim(),
      description: description.trim() || undefined,
      ticketPriorityId: effectivePriorityId || undefined,
      columnId: defaultColumnId,
      dueDate: dueDate || undefined,
      estimatedHours: estimatedHours ? parseFloat(estimatedHours) : undefined,
    };

    try {
      await createTicket.mutateAsync(payload);
      toast.success("Task created");
      resetAndClose();
    } catch {
      toast.error("Failed to create task");
    }
  };

  const resetAndClose = () => {
    setTitle("");
    setDescription("");
    setTicketPriorityId("");
    setDueDate("");
    setEstimatedHours("");
    onClose();
  };

  const inputClass =
    "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={resetAndClose} />
      <div className="relative bg-card border border-border rounded-xl shadow-xl w-full max-w-lg mx-4 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-card-foreground">New Task</h2>
          <button onClick={resetAndClose} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Title <span className="text-destructive">*</span>
            </label>
            <input
              autoFocus
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className={inputClass}
              placeholder="Task title..."
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Description
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className={`${inputClass} min-h-[80px] resize-y`}
              placeholder="Optional description..."
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Priority
              </label>
              <select
                value={effectivePriorityId}
                onChange={(e) => setTicketPriorityId(e.target.value)}
                className={inputClass}
              >
                {priorities
                  ?.filter((p) => p.isActive)
                  .map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                Due Date
              </label>
              <input
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className={inputClass}
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Estimated Hours
            </label>
            <input
              type="number"
              step="0.5"
              min="0"
              value={estimatedHours}
              onChange={(e) => setEstimatedHours(e.target.value)}
              className={inputClass}
              placeholder="e.g. 4"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={resetAndClose}
              className="px-4 py-2 rounded-lg border border-border text-sm text-muted-foreground hover:text-foreground transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createTicket.isPending || !title.trim()}
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {createTicket.isPending ? "Creating..." : "Create Task"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
