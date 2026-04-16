import { create } from "zustand";
import { persist } from "zustand/middleware";

interface TimerState {
  isRunning: boolean;
  startTime: number | null;
  elapsed: number;
  projectId: string | null;
  ticketId: string | null;
  description: string;
  start: (projectId: string, ticketId?: string, description?: string) => void;
  stop: () => {
    elapsed: number;
    projectId: string | null;
    ticketId: string | null;
    description: string;
  };
  tick: () => void;
  reset: () => void;
}

export const useTimerStore = create<TimerState>()(
  persist(
    (set, get) => ({
      isRunning: false,
      startTime: null,
      elapsed: 0,
      projectId: null,
      ticketId: null,
      description: "",
      start: (projectId, ticketId, description) =>
        set({
          isRunning: true,
          startTime: Date.now(),
          elapsed: 0,
          projectId,
          ticketId: ticketId || null,
          description: description || "",
        }),
      stop: () => {
        const state = get();
        const elapsed = state.startTime
          ? Math.floor((Date.now() - state.startTime) / 1000)
          : state.elapsed;
        set({ isRunning: false, startTime: null });
        return {
          elapsed,
          projectId: state.projectId,
          ticketId: state.ticketId,
          description: state.description,
        };
      },
      tick: () => {
        const state = get();
        if (state.isRunning && state.startTime) {
          set({ elapsed: Math.floor((Date.now() - state.startTime) / 1000) });
        }
      },
      reset: () =>
        set({
          isRunning: false,
          startTime: null,
          elapsed: 0,
          projectId: null,
          ticketId: null,
          description: "",
        }),
    }),
    { name: "softim-timer" }
  )
);
