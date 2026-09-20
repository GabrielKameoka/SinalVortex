import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ThemeService } from '../../core/theme.service';
import { QueueMonitorService } from '../../services/queue-monitor.service';
import { QueueCountComponent } from './queue-count.component';

@Component({
  selector: 'sv-queue-monitor', standalone: true, imports: [CommonModule, RouterLink, DatePipe, QueueCountComponent],
  providers: [QueueMonitorService],
  templateUrl: './queue-monitor.component.html', styleUrl: './queue-monitor.component.scss'
})
export class QueueMonitorComponent implements OnInit, OnDestroy {
  readonly monitor = inject(QueueMonitorService);
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  readonly queueCards = [
    { key: 'high', label: 'Alta prioridade', detail: 'Preferência no consumo', tone: 'high' },
    { key: 'normal', label: 'Prioridade normal', detail: 'Fluxo padrão', tone: 'normal' },
    { key: 'low', label: 'Baixa prioridade', detail: 'Processamento assíncrono', tone: 'low' },
    { key: 'dlq', label: 'DLQ', detail: 'Exigem atenção', tone: 'dlq' }
  ] as const;

  ngOnInit(): void { void this.monitor.connect(); }
  ngOnDestroy(): void { void this.monitor.disconnect(); }
  count(key: keyof ReturnType<typeof this.monitor.queues>): number { return this.monitor.queues()[key]; }
  logout(): void { this.auth.logout(); }
}
