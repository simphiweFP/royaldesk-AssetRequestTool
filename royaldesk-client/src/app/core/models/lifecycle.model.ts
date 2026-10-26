export interface Dashboard {
  totalAssets: number;
  assignedAssets: number;
  availableAssets: number;
  inRepairAssets: number;
  pendingWriteOffs: number;
  totalAssetValue: number;
  availableAssetValue: number;
  pendingRequests: number;
  activeEmployees: number;
  estimatedReuseValue: number;
}

export interface Branch { id: number; code: string; name: string; }
export interface Department { id: number; name: string; }

export interface Employee {
  id: number; employeeNumber: string; firstName: string; lastName: string;
  email?: string; username?: string; jobTitle?: string; branchId?: number;
  branchName?: string; departmentId?: number; departmentName?: string;
  employmentStatus: string; startDate?: string; endDate?: string;
}

export interface Asset {
  id: number; assetTag: string; serialNumber?: string; category: string; make?: string;
  model?: string; branchId?: number; branchName?: string; departmentId?: number;
  departmentName?: string; status: string; condition: string; purchaseDate?: string;
  purchaseValue?: number; warrantyExpiry?: string; fullyDepreciated: boolean;
  notes?: string; assignedEmployeeId?: number; assignedEmployeeName?: string;
}

export interface Assignment {
  id: number; assetId: number; assetTag: string; employeeId: number; employeeName: string;
  assignedAtUtc: string; assignedBy: string; conditionAtAssignment: string;
  returnedAtUtc?: string; returnReason?: string; conditionAtReturn?: string;
  status: string; employeeAcceptedAtUtc?: string; employeeSignatureName?: string;
}

export interface LifecycleTask {
  id: number; employeeId: number; lifecycleType: string; taskCode: string; taskName: string;
  isCompleted: boolean; completedBy?: string; completedAtUtc?: string; notes?: string;
}

export interface WriteOff {
  id: number; assetId: number; assetTag: string; serialNumber?: string; make?: string;
  model?: string; category: string; condition: string; currentValue?: number;
  fullyDepreciated: boolean; reason: string; requestedBy: string; requestedAtUtc: string;
  approvalStatus: string; approvedBy?: string; approvedAtUtc?: string;
  disposalMethod?: string; disposalCompany?: string; disposalDate?: string;
  certificateReference?: string; notes?: string;
}
