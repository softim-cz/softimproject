export enum GlobalRole {
  Admin = "Admin",
  Manager = "Manager",
  User = "User",
}

export enum ProjectRole {
  ProjectManager = "ProjectManager",
  Developer = "Developer",
  Guest = "Guest",
}

export enum ProjectStatus {
  Active = "Active",
  OnHold = "OnHold",
  Completed = "Completed",
  Archived = "Archived",
}

export enum CommentSource {
  Manual = "Manual",
  Jira = "Jira",
  Redmine = "Redmine",
  Email = "Email",
  AI = "AI",
  GitHub = "GitHub",
  EasyProject = "EasyProject",
}

export enum WorklogSource {
  Manual = "Manual",
  Timer = "Timer",
  Import = "Import",
  Sync = "Sync",
}

export enum NotificationType {
  TicketAssigned = "TicketAssigned",
  TicketUpdated = "TicketUpdated",
  CommentAdded = "CommentAdded",
  DeadlineApproaching = "DeadlineApproaching",
  MentionedInComment = "MentionedInComment",
  SyncFailed = "SyncFailed",
}

// Interfaces for all entities matching backend DTOs

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  globalRole: GlobalRole;
  isActive: boolean;
  firstName?: string;
  lastName?: string;
  corporateRole?: string;
  companyName?: string;
}

export interface UserPermissions {
  projectsCreate: boolean;
  projectsRead: boolean;
  projectsUpdate: boolean;
  projectsDelete: boolean;
  timeTrackingCreate: boolean;
  timeTrackingRead: boolean;
  timeTrackingUpdate: boolean;
  timeTrackingDelete: boolean;
  reportsCreate: boolean;
  reportsRead: boolean;
  reportsUpdate: boolean;
  reportsDelete: boolean;
}

export interface CurrentUser extends User {
  projectRoles: { projectId: string; projectName: string; role: ProjectRole }[];
  permissions: UserPermissions;
}

export interface AdminUser extends User {
  applicationRoleIds: string[];
}

export interface Project {
  id: string;
  name: string;
  code: string;
  description?: string;
  status: ProjectStatus;
  companyId?: string;
  companyName?: string;
  projectTypeId?: string;
  projectTypeName?: string;
  projectStateId?: string;
  projectStateName?: string;
  projectStateColor?: string;
  parentProjectId?: string;
  parentProjectName?: string;
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
  clientAccessToken?: string;
  externalSystem?: string;
  externalProjectId?: string;
  gitHubConnectedByUserId?: string;
  gitHubWebhookActive?: boolean;
  projectTemplateId: string;
  projectTemplateName?: string;
  members?: ProjectMember[];
  memberCount?: number;
  ticketCount?: number;
  boards?: ProjectBoard[];
}

export interface ProjectBoard {
  id: string;
  name: string;
  isDefault: boolean;
}

export interface UserOption {
  id: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
}

export interface ProjectMember {
  id: string;
  userId: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  role: ProjectRole;
  hourlyRateOverride?: number;
  joinedAt: string;
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
  color?: string;
  isVisible: boolean;
  taskStates: { id: string; name: string; color: string }[];
  tickets: Ticket[];
}

export interface Ticket {
  id: string;
  number: number;
  key: string;
  projectId: string;
  columnId?: string;
  title: string;
  description?: string;
  ticketPriorityId: string;
  ticketPriorityName: string;
  ticketPriorityColor: string;
  taskStateId: string;
  taskStateName: string;
  taskStateColor: string;
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
  taskTypeId?: string;
  taskTypeName?: string;
  taskTypeIcon?: string;
  parentTicketId?: string;
  parentTicketNumber?: number;
  parentTicketKey?: string;
  parentTicketTitle?: string;
  subTickets?: TicketSubTicket[];
  cumulativeWorkedHours?: number;
  externalBudget?: number;
  externalUser?: string;
  externalProject?: string;
  implementationNotes?: string;
  lastComment?: string;
  isWatching?: boolean;
  checklistItems: ChecklistItem[];
  commentsCount: number;
  attachmentsCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface TicketSubTicket {
  id: string;
  number: number;
  key: string;
  title: string;
  taskStateId: string;
  taskStateName: string;
  taskStateColor: string;
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
  ticketId?: string;
  projectId?: string;
  author: User;
  content: string;
  isInternal: boolean;
  source: CommentSource;
  externalUser?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface Worklog {
  id: string;
  projectId: string;
  projectName: string;
  ticketId: string;
  ticketTitle: string;
  userId: string;
  user: User;
  date: string;
  hours: number;
  description: string;
  source: WorklogSource;
  isBillable: boolean;
  hourlyRateSnapshot?: number;
  aiSummary?: string;
  invoiced?: string;
  createdAt: string;
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

// Lookup types

export interface Company {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface IntegrationConnection {
  id: string;
  name: string;
  sourceSystem: string; // "EasyProject" | "Jira" | "Redmine"
  baseUrl: string;
  mode: string; // "Manual" | "FullThenIncremental" | "IncrementalOnly"
  isEnabled: boolean;
  intervalMinutes: number;
  conflictPolicy: string; // "SourceOwnedWins" | "StrictSourceWins" | "PreserveLocalEdits"
  targetCompanyId?: string | null;
  targetCompanyName?: string | null;
  hasToken: boolean;
  lastSyncStartedAt?: string | null;
  lastSyncWatermark?: string | null;
  projectsCount: number;
}

export interface ApplicationRoleEntity {
  id: string;
  name: string;
  nameCs?: string;
  nameEn?: string;
  description?: string;
  sortOrder: number;
  projectsCreate: boolean;
  projectsRead: boolean;
  projectsUpdate: boolean;
  projectsDelete: boolean;
  timeTrackingCreate: boolean;
  timeTrackingRead: boolean;
  timeTrackingUpdate: boolean;
  timeTrackingDelete: boolean;
  reportsCreate: boolean;
  reportsRead: boolean;
  reportsUpdate: boolean;
  reportsDelete: boolean;
}

export interface ProjectType {
  id: string;
  name: string;
  nameCs?: string;
  nameEn?: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface ProjectState {
  id: string;
  name: string;
  nameCs?: string;
  nameEn?: string;
  color: string;
  sortOrder: number;
  isActive: boolean;
  isDefault: boolean;
}

export interface TaskType {
  id: string;
  name: string;
  nameCs?: string;
  nameEn?: string;
  icon?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface TaskState {
  id: string;
  name: string;
  nameCs?: string;
  nameEn?: string;
  color: string;
  sortOrder: number;
  isActive: boolean;
  isDefault: boolean;
  isClosedState: boolean;
  projectTemplateId: string;
}

export interface TicketPriorityLookup {
  id: string;
  name: string;
  nameCs?: string;
  nameEn?: string;
  color: string;
  sortOrder: number;
  isActive: boolean;
  isDefault: boolean;
  projectTemplateId: string;
}

export enum CustomFieldType {
  Text = "Text",
  Number = "Number",
  Date = "Date",
  Select = "Select",
}

export interface CustomFieldDefinition {
  id: string;
  name: string;
  description?: string;
  fieldType: string;
  isRequired: boolean;
  options?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface ProjectCustomFieldValue {
  customFieldDefinitionId: string;
  fieldName: string;
  fieldType: string;
  description?: string;
  isRequired: boolean;
  options?: string;
  value?: string;
}

export interface ProjectTemplateField {
  customFieldDefinitionId: string;
  customFieldName: string;
  sortOrder: number;
}

export interface GitHubStatus {
  connected: boolean;
  login?: string;
}

export interface GitHubRepo {
  fullName: string;
  description?: string;
  isPrivate: boolean;
}

export interface ProjectTemplate {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  fields: ProjectTemplateField[];
  taskStates: TaskState[];
  ticketPriorities: TicketPriorityLookup[];
  allowedTaskTypeIds: string[];
}

export interface ApiKey {
  id: string;
  name: string;
  prefix: string;
  expiresAt?: string;
  lastUsedAt?: string;
  revokedAt?: string;
  createdAt: string;
}

export interface GenerateApiKeyResult {
  id: string;
  name: string;
  prefix: string;
  plaintextKey: string;
  expiresAt?: string;
  createdAt: string;
}

export interface ProjectAllowedTaskTypes {
  // True when ticket creation/edit is limited to `effectiveTaskTypes`.
  isRestricted: boolean;
  // Project's own override (empty = inherit template default).
  overrideTaskTypeIds: string[];
  // Template default (shown so the UI can explain what is inherited).
  templateTaskTypeIds: string[];
  // Resolved list actually offered when creating/editing tickets.
  effectiveTaskTypes: TaskType[];
}

// Migration types

export interface EpProjectPreview {
  epId: number;
  name: string;
  description?: string;
  status: number;
  parentName?: string;
  issueCount: number;
  alreadyImported: boolean;
}

export interface EpTrackerMapping {
  epId: number;
  epName: string;
  suggestedTaskTypeId?: string;
  suggestedTaskTypeName?: string;
}

export interface EpStatusMapping {
  epId: number;
  epName: string;
  isClosed: boolean;
  suggestedTaskStateId?: string;
  suggestedTaskStateName?: string;
}

export interface EpPriorityMapping {
  epId: number;
  epName: string;
  suggestedTicketPriorityId?: string;
  suggestedTicketPriorityName?: string;
}

export interface EpUserMapping {
  epId: number;
  epName: string;
  epEmail?: string;
  matchedUserId?: string;
  matchedUserName?: string;
}

export interface EpLookupsResult {
  trackers: EpTrackerMapping[];
  statuses: EpStatusMapping[];
  priorities: EpPriorityMapping[];
}

export interface MigrationProgress {
  jobId: string;
  status: string;
  currentPhase: string;
  projectsTotal: number;
  projectsMigrated: number;
  ticketsTotal: number;
  ticketsMigrated: number;
  commentsTotal: number;
  commentsMigrated: number;
  worklogsTotal: number;
  worklogsMigrated: number;
  attachmentsTotal: number;
  attachmentsMigrated: number;
  errorCount: number;
  itemsCreated: number;
  itemsUpdated: number;
  itemsSkipped: number;
  recentErrors: string[];
  recentLog: string[];
  overallPercent: number;
}

export interface MigrationJob {
  id: string;
  sourceSystem: string;
  sourceBaseUrl: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  projectsMigrated: number;
  ticketsMigrated: number;
  itemsFailed: number;
}
