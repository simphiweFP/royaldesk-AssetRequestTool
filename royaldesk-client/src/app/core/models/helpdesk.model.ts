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
