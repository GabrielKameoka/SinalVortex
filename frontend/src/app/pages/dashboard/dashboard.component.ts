import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, switchMap, timer } from 'rxjs';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { AuthService } from '../../core/auth.service';
import { ThemeService } from '../../core/theme.service';
import { DashboardMetrics, DashboardService } from '../../services/dashboard.service';

const channelNames: Record<number, string> = { 1: 'E-mail', 2: 'WhatsApp', 3: 'SMS', 4: 'Webhook', 5: 'Push' };
const channelColors = ['#b8f36b', '#7ed957', '#4fbf67', '#2d8f58', '#176b45'];

@Component({ selector: 'sv-dashboard', standalone: true, imports: [CommonModule, BaseChartDirective], templateUrl: './dashboard.component.html', styleUrl: './dashboard.component.scss' })
export class DashboardComponent {
  private readonly service = inject(DashboardService);
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  loading = true; refreshing = false; error = ''; lastUpdated = '';
  metrics: DashboardMetrics | null = null;
  doughnutData: ChartData<'doughnut'> = { labels: [], datasets: [{ data: [], backgroundColor: channelColors, borderWidth: 0, hoverOffset: 6 }] };
  readonly doughnutOptions: ChartConfiguration<'doughnut'>['options'] = { responsive: true, maintainAspectRatio: false, cutout: '70%', plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, padding: 18, color: '#8b978d' } } } };

  constructor() { this.startPolling(); }
  refresh(): void { this.refreshing = true; this.service.getMetrics().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (metrics) => this.applyMetrics(metrics), error: () => { this.error = 'Não foi possível atualizar as métricas.'; this.refreshing = false; } }); }
  logout(): void { this.auth.logout(); }
  channelName(channel: number): string { return channelNames[channel] ?? 'Outro'; }
  private startPolling(): void { timer(0, 30000).pipe(switchMap(() => this.service.getMetrics().pipe(catchError(() => { this.error = 'A API está indisponível. Exibindo o último valor recebido.'; this.loading = false; return EMPTY; }))), takeUntilDestroyed(this.destroyRef)).subscribe((metrics) => this.applyMetrics(metrics)); }
  private applyMetrics(metrics: DashboardMetrics): void { this.metrics = metrics; this.loading = false; this.refreshing = false; this.error = ''; this.lastUpdated = new Date(metrics.generatedAtUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }); this.doughnutData = { labels: metrics.byChannel.map((item) => this.channelName(item.channel)), datasets: [{ data: metrics.byChannel.map((item) => item.count), backgroundColor: channelColors, borderWidth: 0, hoverOffset: 6 }] }; }
}
