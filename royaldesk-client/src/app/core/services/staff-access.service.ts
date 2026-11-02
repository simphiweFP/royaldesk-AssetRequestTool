import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';

export interface StaffProfile {
  id: number;
  displayName: string;
  username?: string;
  isActive: boolean;
  lastAccessAtUtc?: string;
  pages: string[];
}

export interface StaffSession {
  staffAccessId: number;
  displayName: string;
  username?: string;
  pages: string[];
  expiresAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class StaffAccessService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://localhost:7080/api/staff-access';
  private readonly sessionSubject = new BehaviorSubject<StaffSession | null>(null);
  readonly session$ = this.sessionSubject.asObservable();

  get session(): StaffSession | null { return this.sessionSubject.value; }
  can(page: string): boolean { return !!this.session?.pages.includes(page); }

  bootstrapRequired(): Observable<{ required: boolean }> {
    return this.http.get<{ required: boolean }>(`${this.base}/bootstrap-required`, { withCredentials: true });
  }

  bootstrap(payload: { displayName: string; username?: string | null; password: string; bootstrapKey: string }): Observable<StaffProfile> {
    return this.http.post<StaffProfile>(`${this.base}/bootstrap`, payload, { withCredentials: true });
  }

  publicProfiles(): Observable<StaffProfile[]> {
    return this.http.get<StaffProfile[]>(`${this.base}/profiles/public`, { withCredentials: true });
  }

  unlock(staffAccessId: number, password: string): Observable<StaffSession> {
    return this.http.post<StaffSession>(`${this.base}/unlock`, { staffAccessId, password }, { withCredentials: true })
      .pipe(tap(session => this.sessionSubject.next(session)));
  }

  restore(): Observable<StaffSession> {
    return this.http.get<StaffSession>(`${this.base}/me`, { withCredentials: true })
      .pipe(tap(session => this.sessionSubject.next(session)));
  }

  lock(): Observable<void> {
    return this.http.post<void>(`${this.base}/lock`, {}, { withCredentials: true })
      .pipe(tap(() => this.sessionSubject.next(null)));
  }

  profiles(): Observable<StaffProfile[]> {
    return this.http.get<StaffProfile[]>(`${this.base}/profiles`, { withCredentials: true });
  }

  createProfile(payload: { displayName: string; username?: string | null; password: string; pages: string[] }): Observable<StaffProfile> {
    return this.http.post<StaffProfile>(`${this.base}/profiles`, payload, { withCredentials: true });
  }

  updateProfile(id: number, payload: { displayName: string; username?: string | null; isActive: boolean; pages: string[] }): Observable<void> {
    return this.http.put<void>(`${this.base}/profiles/${id}`, payload, { withCredentials: true });
  }

  resetPassword(id: number, password: string): Observable<void> {
    return this.http.post<void>(`${this.base}/profiles/${id}/reset-password`, { password }, { withCredentials: true });
  }

  clearLocalSession(): void { this.sessionSubject.next(null); }
}
