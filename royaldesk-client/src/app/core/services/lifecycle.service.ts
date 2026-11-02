import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset, Assignment, Branch, Dashboard, Department, Employee, LifecycleTask, WriteOff } from '../models/lifecycle.model';
import { BusinessValueSummary } from '../models/business-value.model';

@Injectable({ providedIn: 'root' })
export class LifecycleService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://localhost:7080/api/lifecycle';
  private readonly businessValueBase = 'https://localhost:7080/api/business-value';
  private readonly options = { withCredentials: true };

  dashboard(): Observable<Dashboard> { return this.http.get<Dashboard>(`${this.base}/dashboard`, this.options); }
  businessValue(): Observable<BusinessValueSummary> { return this.http.get<BusinessValueSummary>(this.businessValueBase, this.options); }
  branches(): Observable<Branch[]> { return this.http.get<Branch[]>(`${this.base}/branches`, this.options); }
  departments(): Observable<Department[]> { return this.http.get<Department[]>(`${this.base}/departments`, this.options); }
  employees(): Observable<Employee[]> { return this.http.get<Employee[]>(`${this.base}/employees`, this.options); }
  assets(): Observable<Asset[]> { return this.http.get<Asset[]>(`${this.base}/assets`, this.options); }
  assignments(employeeId?: number): Observable<Assignment[]> { return this.http.get<Assignment[]>(`${this.base}/assignments${employeeId ? `?employeeId=${employeeId}` : ''}`, this.options); }
  writeOffs(): Observable<WriteOff[]> { return this.http.get<WriteOff[]>(`${this.base}/write-offs`, this.options); }
  tasks(employeeId: number, type: 'Onboarding' | 'Offboarding'): Observable<LifecycleTask[]> { return this.http.get<LifecycleTask[]>(`${this.base}/employees/${employeeId}/tasks/${type}`, this.options); }

  createEmployee(body: object): Observable<Employee> { return this.http.post<Employee>(`${this.base}/employees`, body, this.options); }
  createAsset(body: object): Observable<Asset> { return this.http.post<Asset>(`${this.base}/assets`, body, this.options); }
  assign(body: object): Observable<Assignment> { return this.http.post<Assignment>(`${this.base}/assignments`, body, this.options); }
  accept(id: number, signatureName: string): Observable<void> { return this.http.post<void>(`${this.base}/assignments/${id}/accept`, { signatureName }, this.options); }
  returnAsset(id: number, reason: string, condition: string, notes?: string): Observable<void> { return this.http.post<void>(`${this.base}/assignments/${id}/return`, { reason, condition, notes }, this.options); }
  createWriteOff(body: object): Observable<WriteOff> { return this.http.post<WriteOff>(`${this.base}/write-offs`, body, this.options); }
  approveWriteOff(id: number, approved: boolean, notes?: string): Observable<void> { return this.http.post<void>(`${this.base}/write-offs/${id}/approval`, { approved, notes }, this.options); }
  disposeWriteOff(id: number, body: object): Observable<void> { return this.http.post<void>(`${this.base}/write-offs/${id}/dispose`, body, this.options); }
  updateTask(id: number, isCompleted: boolean, notes?: string): Observable<void> { return this.http.patch<void>(`${this.base}/tasks/${id}`, { isCompleted, notes }, this.options); }
  acceptancePdf(id: number): Observable<Blob> { return this.http.get(`${this.base}/assignments/${id}/acceptance.pdf`, { withCredentials: true, responseType: 'blob' }); }
}
