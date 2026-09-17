import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChannelMetric { channel: number; count: number; }
export interface DashboardMetrics { total: number; delivered: number; failedOrDlq: number; successRate: number; byChannel: ChannelMetric[]; windowStartUtc: string; generatedAtUtc: string; }

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  getMetrics(): Observable<DashboardMetrics> { return this.http.get<DashboardMetrics>(`${environment.apiBaseUrl}/dashboard/metrics`); }
}
