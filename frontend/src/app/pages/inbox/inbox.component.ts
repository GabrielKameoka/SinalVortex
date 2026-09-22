import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, EMPTY, catchError, switchMap, timer } from 'rxjs';
import { ThemeService } from '../../core/theme.service';
import { AuthService } from '../../core/auth.service';
import { NotificationPage, NotificationsService } from '../../services/notifications.service';

@Component({ selector: 'sv-inbox', standalone: true, imports: [CommonModule, RouterLink], templateUrl: './inbox.component.html', styleUrl: './inbox.component.scss' })
export class InboxComponent {
  readonly theme = inject(ThemeService);
  readonly auth = inject(AuthService);
  private readonly service = inject(NotificationsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly requestedPage = new BehaviorSubject(1);
  readonly page = signal<NotificationPage | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly selectedId = signal<string | null>(null);
  readonly selected = computed(() => this.page()?.items.find(item => item.id === this.selectedId()) ?? this.page()?.items[0] ?? null);

  constructor() {
    this.requestedPage.pipe(
      switchMap(page => timer(0, 5000).pipe(switchMap(() => this.service.list(page).pipe(
        catchError(() => {
          this.loading.set(false);
          this.error.set('Não foi possível atualizar as notificações. Os últimos dados recebidos podem estar desatualizados.');
          return EMPTY;
        })
      )))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => { this.page.set(result); this.loading.set(false); this.error.set(''); });
  }

  goTo(page: number): void {
    if (page < 1 || page > (this.page()?.totalPages ?? 1)) return;
    this.loading.set(true);
    this.requestedPage.next(page);
  }
  channelLabel(channel: number): string { return ({ 1: 'E-mail', 2: 'WhatsApp', 3: 'SMS', 4: 'Webhook', 5: 'Push' } as Record<number, string>)[channel] ?? 'Desconhecido'; }
  statusLabel(status: number): string { return ({ 1: 'Pendente', 2: 'Em processamento', 3: 'Enviado ao provedor', 4: 'Falhou', 5: 'DLQ' } as Record<number, string>)[status] ?? 'Desconhecido'; }
  logout(): void { this.auth.logout(); }
}
