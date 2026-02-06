export enum GlobalRole {
  Admin = 'Admin',
  Manager = 'Manager',
  User = 'User',
}

export enum ProjectRole {
  ProjectManager = 'ProjectManager',
  Developer = 'Developer',
  Guest = 'Guest',
}

export enum ProjectStatus {
  Active = 'Active',
  OnHold = 'OnHold',
  Completed = 'Completed',
  Archived = 'Archived',
}

export enum TicketStatus {
  Backlog = 'Backlog',
  Todo = 'Todo',
  InProgress = 'InProgress',
  Review = 'Review',
  Done = 'Done',
  Closed = 'Closed',
}

export enum TicketPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical',
}

export enum CommentSource {
  Manual = 'Manual',
  Jira = 'Jira',
  Redmine = 'Redmine',
  Email = 'Email',
  AI = 'AI',
}

export enum WorklogSource {
  Manual = 'Manual',
  Timer = 'Timer',
  Import = 'Import',
  Sync = 'Sync',
}

export enum NotificationType {
  TicketAssigned = 'TicketAssigned',
  TicketUpdated = 'TicketUpdated',
  CommentAdded = 'CommentAdded',
  DeadlineApproaching = 'DeadlineApproaching',
  MentionedInComment = 'MentionedInComment',
  SyncFailed = 'SyncFailed',
}

// Interfaces for all entities matching backend DTOs

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  globalRole: GlobalRole;
  isActive: boolean;
}

export interface Project {
  id: string;
  name: string;
  code: string;
  description?: string;
  status: ProjectStatus;
  budgetHours?: number;
  spentHours: number;
  budgetAmount?: number;
  spentAmount: number;
  startDate?: string;
  endDate?: string;
  deadlineDate?: string;
  healthScore: number;
  isOverBudget: boolean;
  isOverDeadline: boolean;
  clientAccessEnabled: boolean;
}

export interface ProjectMember {
  id: string;
  projectId: string;
  userId: string;
  user: User;
  role: ProjectRole;
  hourlyRateOverride?: number;
}

export interface KanbanBoard {
  id: string;
  projectId: string;
  name: string;
  isDefault: boolean;
  columns: KanbanColumn[];
}

export interface KanbanColumn {
  id: string;
  boardId: string;
  name: string;
  position: number;
  wipLimit?: number;
  mapsToStatus: TicketStatus;
  tickets: Ticket[];
}

export interface Ticket {
  id: string;
  projectId: string;
  columnId?: string;
  title: string;
  description?: string;
  priority: TicketPriority;
  status: TicketStatus;
  position: number;
  assigneeId?: string;
  assignee?: User;
  reporterId: string;
  reporter: User;
  externalId?: string;
  externalUrl?: string;
  aiSummary?: string;
  dueDate?: string;
  estimatedHours?: number;
  checklistItems: ChecklistItem[];
  commentsCount: number;
  attachmentsCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface TicketAttachment {
  id: string;
  ticketId: string;
  fileName: string;
  blobUrl: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedBy: User;
  createdAt: string;
}

export interface ChecklistItem {
  id: string;
  ticketId: string;
  text: string;
  isCompleted: boolean;
  position: number;
}

export interface Comment {
  id: string;
  ticketId: string;
  author: User;
  content: string;
  isInternal: boolean;
  source: CommentSource;
  createdAt: string;
  updatedAt?: string;
}

export interface Worklog {
  id: string;
  projectId: string;
  ticketId?: string;
  userId: string;
  user: User;
  date: string;
  hours: number;
  description?: string;
  source: WorklogSource;
  isBillable: boolean;
}

export interface Notification {
  id: string;
  title: string;
  message?: string;
  type: NotificationType;
  referenceId?: string;
  isRead: boolean;
  createdAt: string;
}
