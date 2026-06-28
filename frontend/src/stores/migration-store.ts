import { create } from "zustand";
import type {
  EpProjectPreview,
  EpTrackerMapping,
  EpStatusMapping,
  EpPriorityMapping,
  EpUserMapping,
} from "@/types";

interface MigrationState {
  step: number;
  baseUrl: string;
  apiKey: string;
  connectionTested: boolean;

  // Fetched EP data
  projects: EpProjectPreview[];
  trackerMappings: EpTrackerMapping[];
  statusMappings: EpStatusMapping[];
  priorityMappings: EpPriorityMapping[];
  userMappings: EpUserMapping[];

  // User selections
  selectedProjectIds: Set<number>;
  trackerSelections: Record<number, string | null>;
  statusSelections: Record<number, string>;
  prioritySelections: Record<number, string>;
  userSelections: Record<number, string | null>;

  // Options
  skipClosedIssues: boolean;
  skipAttachments: boolean;
  importComments: boolean;
  importWorklogs: boolean;
  importChecklists: boolean;
  createMissingUsers: boolean;

  // Target template — kam EP importované TaskStates/TicketPriorities patří
  // (a kam se napojí importované Project.ProjectTemplateId).
  targetProjectTemplateId: string | null;

  // Connection config (milník 3d)
  targetCompanyId: string | null;
  enableIncrementalSync: boolean;
  syncIntervalMinutes: number;

  // Active migration
  activeJobId: string | null;

  // Actions
  setStep: (step: number) => void;
  setBaseUrl: (url: string) => void;
  setApiKey: (key: string) => void;
  setConnectionTested: (tested: boolean) => void;
  setProjects: (projects: EpProjectPreview[]) => void;
  setLookups: (
    trackers: EpTrackerMapping[],
    statuses: EpStatusMapping[],
    priorities: EpPriorityMapping[]
  ) => void;
  setUserMappings: (users: EpUserMapping[]) => void;
  toggleProject: (id: number) => void;
  selectAllProjects: () => void;
  selectNoProjects: () => void;
  setTrackerSelection: (epId: number, value: string | null) => void;
  setStatusSelection: (epId: number, value: string) => void;
  setPrioritySelection: (epId: number, value: string) => void;
  setUserSelection: (epId: number, value: string | null) => void;
  setOption: (key: string, value: boolean) => void;
  setTargetProjectTemplateId: (id: string | null) => void;
  setTargetCompanyId: (id: string | null) => void;
  setEnableIncrementalSync: (value: boolean) => void;
  setSyncIntervalMinutes: (minutes: number) => void;
  updateProjectIssueCount: (epId: number, count: number) => void;
  setActiveJobId: (id: string | null) => void;
  reset: () => void;
}

const initialState = {
  step: 0,
  baseUrl: "",
  apiKey: "",
  connectionTested: false,
  projects: [] as EpProjectPreview[],
  trackerMappings: [] as EpTrackerMapping[],
  statusMappings: [] as EpStatusMapping[],
  priorityMappings: [] as EpPriorityMapping[],
  userMappings: [] as EpUserMapping[],
  selectedProjectIds: new Set<number>(),
  trackerSelections: {} as Record<number, string | null>,
  statusSelections: {} as Record<number, string>,
  prioritySelections: {} as Record<number, string>,
  userSelections: {} as Record<number, string | null>,
  skipClosedIssues: false,
  skipAttachments: false,
  importComments: true,
  importWorklogs: true,
  importChecklists: true,
  createMissingUsers: true,
  targetProjectTemplateId: null as string | null,
  targetCompanyId: null as string | null,
  enableIncrementalSync: false,
  syncIntervalMinutes: 1440,
  activeJobId: null as string | null,
};

export const useMigrationStore = create<MigrationState>()((set) => ({
  ...initialState,

  setStep: (step) => set({ step }),
  setBaseUrl: (baseUrl) => set({ baseUrl, connectionTested: false }),
  setApiKey: (apiKey) => set({ apiKey, connectionTested: false }),
  setConnectionTested: (connectionTested) => set({ connectionTested }),

  setProjects: (projects) => {
    const selected = new Set<number>();
    projects.forEach((p) => {
      if (!p.alreadyImported) selected.add(p.epId);
    });
    set({ projects, selectedProjectIds: selected });
  },

  setLookups: (trackers, statuses, priorities) => {
    const trackerSelections: Record<number, string | null> = {};
    trackers.forEach((t) => {
      trackerSelections[t.epId] = t.suggestedTaskTypeId ?? null;
    });

    const statusSelections: Record<number, string> = {};
    statuses.forEach((s) => {
      if (s.suggestedTaskStateId) statusSelections[s.epId] = s.suggestedTaskStateId;
    });

    const prioritySelections: Record<number, string> = {};
    priorities.forEach((p) => {
      if (p.suggestedTicketPriorityId) prioritySelections[p.epId] = p.suggestedTicketPriorityId;
    });

    set({
      trackerMappings: trackers,
      statusMappings: statuses,
      priorityMappings: priorities,
      trackerSelections,
      statusSelections,
      prioritySelections,
    });
  },

  setUserMappings: (users) => {
    const userSelections: Record<number, string | null> = {};
    users.forEach((u) => {
      userSelections[u.epId] = u.matchedUserId ?? null;
    });
    set({ userMappings: users, userSelections });
  },

  toggleProject: (id) =>
    set((state) => {
      const next = new Set(state.selectedProjectIds);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return { selectedProjectIds: next };
    }),

  selectAllProjects: () =>
    set((state) => ({
      selectedProjectIds: new Set(state.projects.map((p) => p.epId)),
    })),

  selectNoProjects: () => set({ selectedProjectIds: new Set() }),

  setTrackerSelection: (epId, value) =>
    set((state) => ({
      trackerSelections: { ...state.trackerSelections, [epId]: value },
    })),

  setStatusSelection: (epId, value) =>
    set((state) => ({
      statusSelections: { ...state.statusSelections, [epId]: value },
    })),

  setPrioritySelection: (epId, value) =>
    set((state) => ({
      prioritySelections: { ...state.prioritySelections, [epId]: value },
    })),

  setUserSelection: (epId, value) =>
    set((state) => ({
      userSelections: { ...state.userSelections, [epId]: value },
    })),

  setOption: (key, value) => set({ [key]: value } as Partial<MigrationState>),

  setTargetProjectTemplateId: (targetProjectTemplateId) => set({ targetProjectTemplateId }),

  setTargetCompanyId: (targetCompanyId) => set({ targetCompanyId }),
  setEnableIncrementalSync: (enableIncrementalSync) => set({ enableIncrementalSync }),
  setSyncIntervalMinutes: (syncIntervalMinutes) => set({ syncIntervalMinutes }),

  updateProjectIssueCount: (epId, count) =>
    set((state) => ({
      projects: state.projects.map((p) => (p.epId === epId ? { ...p, issueCount: count } : p)),
    })),

  setActiveJobId: (activeJobId) => set({ activeJobId }),

  reset: () => set(initialState),
}));
