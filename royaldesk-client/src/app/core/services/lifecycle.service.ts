import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset, Assignment, Branch, Dashboard, Department, Employee, LifecycleTask, WriteOff } from '../models/lifecycle.model';

@Injectable({ providedIn: 'root' })
export class LifecycleService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://localhost:7080/api/lifecycle';
  private readonly headers = new HttpHeaders({ Authorization: 'Basic ' + btoa('demo.user:RoyalDesk123!') });

  dashboard(): Observable<Dashboard> { return this.http.get<Dashboard>(`${this.base}/dashboard`, { headers: this.headers }); }
  branches(): Observable<Branch[]> { return this.http.get<Branch[]>(`${this.base}/branches`, { headers: this.headers }); }
  departments(): Observable<Department[]> { return this.http.get<Department[]>(`${this.base}/departments`, { headers: this.headers }); }
  employees(): Observable<Employee[]> { return this.http.get<Employee[]>(`${this.base}/employees`, { headers: this.headers }); }
  assets(): Observable<Asset[]> { return this.http.get<Asset[]>(`${this.base}/assets`, { headers: this.headers }); }
  assignments(employeeId?: number): Observable<Assignment[]> { return this.http.get<Assignment[]>(`${this.base}/assignments${employeeId ? `?employeeId=${employeeId}` : ''}`, { headers: this.headers }); }
  writeOffs(): Observable<WriteOff[]> { return this.http.get<WriteOff[]>(`${this.base}/write-offs`, { headers: this.headers }); }
  tasks(employeeId: number, type: 'Onboarding' | 'Offboarding'): Observable<LifecycleTask[]> { return this.http.get<LifecycleTask[]>(`${this.base}/employees/${employeeId}/tasks/${type}`, { headers: this.headers }); }

  createEmployee(body: object): Observable<Employee> { return this.http.post<Employee>(`${this.base}/employees`, body, { headers: this.headers }); }
  createAsset(body: object): Observable<Asset> { return this.http.post<Asset>(`${this.base}/assets`, body, { headers: this.headers }); }
  assign(body: object): Observable<Assignment> { return this.http.post<Assignment>(`${this.base}/assignments`, body, { headers: this.headers }); }
  accept(id: number, signatureName: string): Observable<void> { return this.http.post<void>(`${this.base}/assignments/${id}/accept`, { signatureName }, { headers: this.headers }); }
  returnAsset(id: number, reason: string, condition: string, notes?: string): Observable<void> { return this.http.post<void>(`${this.base}/assignments/${id}/return`, { reason, condition, notes }, { headers: this.headers }); }
  createWriteOff(body: object): Observable<WriteOff> { return this.http.post<WriteOff>(`${this.base}/write-offs`, body, { headers: this.headers }); }
  approveWriteOff(id: number, approved: boolean, notes?: string): Observable<void> { return this.http.post<void>(`${this.base}/write-offs/${id}/approval`, { approved, notes }, { headers: this.headers }); }
  disposeWriteOff(id: number, body: object): Observable<void> { return this.http.post<void>(`${this.base}/write-offs/${id}/dispose`, body, { headers: this.headers }); }
  updateTask(id: number, isCompleted: boolean, notes?: string): Observable<void> { return this.http.patch<void>(`${this.base}/tasks/${id}`, { isCompleted, notes }, { headers: this.headers }); }
  acceptancePdf(id: number): Observable<Blob> { return this.http.get(`${this.base}/assignments/${id}/acceptance.pdf`, { headers: this.headers, responseType: 'blob' }); }
}
