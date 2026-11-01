export interface HelpDeskTicket {
  id: number;
  ticketNumber: string;
  requesterName: string;
  requesterEmail?: string;
  branch: string;
  department: string;
  itemType: string;
  quantity: number;
  reason: string;
  status: string;
  assignedToId?: number;
  assignedToName?: string;
  assignedAtUtc?: string;
  startedAtUtc?: string;
  resolvedAtUtc?: string;
  requestedAtUtc: string;
  lastUpdatedAtUtc?: string;
}

export interface TicketStatusHistory {
  id: number;
  ticketId: number;
  fromStatus?: string;
  toStatus: string;
  changedBy: string;
  changedByName?: string;
  comment?: string;
  publicVisible: boolean;
  changedAtUtc: string;
}

export interface ITTeamMember {
  id: number;
  displayName: string;
  email?: string;
  username: string;
  role: string;
  isActive: boolean;
}

export interface PublicTicketSummary {
  ticketNumber: string;
  requesterName: string;
  department: string;
  itemType: string;
  status: string;
  assignedToName?: string;
  requestedAtUtc: string;
  lastUpdatedAtUtc?: string;
  latestPublicUpdate?: string;
}

export interface PublicTicketHistory {
  status: string;
  technicianName?: string;
  update?: string;
  changedAtUtc: string;
}

export interface PublicTicketDetail extends PublicTicketSummary {
  branch: string;
  quantity: number;
  reason: string;
  assignedAtUtc?: string;
  startedAtUtc?: string;
  resolvedAtUtc?: string;
  history: PublicTicketHistory[];
}

export interface EscalateToJustIctRequest {
  teamViewerId: string;
  issueDescription: string;
  troubleshooting: string;
  additionalNotes?: string;
  emailBody: string;
}

export interface TicketEscalation {
  id: number;
  ticketId: number;
  provider: string;
  providerEmail: string;
  teamViewerId: string;
  issueDescription: string;
  troubleshooting: string;
  additionalNotes?: string;
  escalatedByName: string;
  escalatedAtUtc: string;
}
