import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthResponse { usuarioId: string; tenantId: string; accessToken: string; expiresAtUtc: string; }
export interface RegisterRequest { nome: string; email: string; senha: string; }
const SESSION_KEY = 'sinalvortex.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private session: AuthResponse | null = this.readSession();

  login(tenantId: string, email: string, senha: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/autenticacao/token`, { tenantId, email, senha }).pipe(tap((response) => this.saveSession(response)));
  }
  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/autenticacao/registrar`, request).pipe(tap((response) => this.saveSession(response)));
  }
  tenantId(): string | null { return this.session?.tenantId ?? null; }
  private saveSession(response: AuthResponse): void {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(response));
    this.session = response;
  }
  token(): string | null { return this.session?.accessToken ?? null; }
  isAuthenticated(): boolean { return !!this.token() && new Date(this.session?.expiresAtUtc ?? 0).getTime() > Date.now(); }
  logout(): void { this.session = null; sessionStorage.removeItem(SESSION_KEY); void this.router.navigateByUrl('/login'); }
  private readSession(): AuthResponse | null { try { const raw = sessionStorage.getItem(SESSION_KEY); return raw ? JSON.parse(raw) as AuthResponse : null; } catch { return null; } }
}
